using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GreatSnooper.Model
{
    [DebuggerDisplay("{Sender.Name}: {Text}")]
    public class Message
    {
        #region Static
        protected static Regex urlRegex = new Regex(@"(ht|f)tps?://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        #endregion

        #region Enums
        public enum MessageTypes { Channel, Join, Quit, Part, Offline, Action, User, Notice, Hyperlink, Time, League }
        public enum HightLightTypes { Highlight, LeagueFound, NotificatorFound, URI }
        #endregion

        #region Properties
        public User Sender { get; private set; }
        public string Text { get; private set; }
        public DateTime Time { get; private set; }
        public MessageSetting Style { get; private set; }
        public SortedDictionary<int, KeyValuePair<int, HightLightTypes>> HighlightWords { get; private set; }
        public bool IsLogged { get; private set; }
        #endregion

        public Message(User sender, string text, MessageSetting setting, DateTime time, bool isLogged = false)
        {
            this.Sender = sender;
            this.Text = text;
            this.Style = setting;
            this.Time = time;
            this.IsLogged = isLogged;

            if (setting.Type == MessageTypes.Action ||
                setting.Type == MessageTypes.Channel ||
                setting.Type == MessageTypes.Notice ||
                setting.Type == MessageTypes.User ||
                setting.Type == MessageTypes.Quit)
            {
                MatchCollection matches = urlRegex.Matches(text);
                for (int i = 0; i < matches.Count; i++)
                {
                    Group group = matches[i].Groups[0];
                    Uri uri;
                    if (Uri.TryCreate(group.Value, UriKind.RelativeOrAbsolute, out uri))
                        this.AddHighlightWord(group.Index, group.Length, Message.HightLightTypes.URI);
                }
            }
        }

        public void AddHighlightWord(int idx, int length, HightLightTypes type)
        {
            if (this.HighlightWords == null)
                this.HighlightWords = new SortedDictionary<int, KeyValuePair<int, HightLightTypes>>();

            // Handling overlapping.. eg. when notificator finds *, but the message contains url (#Help -> !port)
            // The logic is that newly added item can not conflict with already added item
            Dictionary<int, int> addRanges = new Dictionary<int, int>();
            KeyValuePair<int, int> tempRange = new KeyValuePair<int, int>(idx, length);
            foreach (var item in this.HighlightWords)
            {
                if (item.Key > tempRange.Key && tempRange.Key + tempRange.Value > item.Key)
                {
                    int newLength = item.Key - tempRange.Key;
                    if (tempRange.Value < newLength)
                        newLength = tempRange.Value;
                    addRanges.Add(tempRange.Key, newLength);
                    tempRange = new KeyValuePair<int, int>(item.Key + item.Value.Key, this.Text.Length - item.Key - item.Value.Key);
                }
            }
            if (tempRange.Value > 0)
                addRanges.Add(tempRange.Key, tempRange.Value);

            foreach (var item in addRanges)
                this.HighlightWords[item.Key] = new KeyValuePair<int, HightLightTypes>(item.Value, type);
        }
    }
}
