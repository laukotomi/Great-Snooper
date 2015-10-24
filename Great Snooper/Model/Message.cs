using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GreatSnooper.Model
{
    [DebuggerDisplay("{Sender.Name}: {Text}")]
    public class Message
    {
        #region Enums
        public enum MessageTypes { Channel, Join, Quit, Part, Offline, Action, User, Notice, BuddyJoined, Hyperlink, Time, League }
        public enum HightLightTypes { Highlight, LeagueFound, NotificatorFound, URI }
        #endregion

        #region Properties
        public User Sender { get; private set; }
        public string Text { get; private set; }
        public DateTime Time { get; private set; }
        public MessageSetting Style { get; private set; }
        public Dictionary<int, KeyValuePair<int, HightLightTypes>> HighlightWords { get; private set; }
        #endregion

        public Message(User sender, string text, MessageSetting setting)
        {
            this.Sender = sender;
            this.Text = text;
            this.Style = setting;
            this.Time = DateTime.Now;
        }

        public void AddHighlightWord(int idx, int length, HightLightTypes type)
        {
            if (HighlightWords == null)
                HighlightWords = new Dictionary<int, KeyValuePair<int, HightLightTypes>>();
            HighlightWords[idx] = new KeyValuePair<int,HightLightTypes>(length, type);
        }
    }
}
