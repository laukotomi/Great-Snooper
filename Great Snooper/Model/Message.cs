namespace GreatSnooper.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;

    using GreatSnooper.Helpers;
    using GreatSnooper.ViewModel;

    [DebuggerDisplay("{Sender.Name}: {Text}")]
    public class Message
    {
        protected static Regex urlRegex = new Regex(@"(ht|f)tps?://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private Run _nickRun;

        public enum HightLightTypes
        {
            Highlight, LeagueFound, NotificatorFound, URI
        }

        public enum MessageTypes
        {
            Channel, Join, Quit, Part, Offline, Action, User, Notice, Hyperlink, Time, League
        }

        public SortedDictionary<int, KeyValuePair<int, HightLightTypes>> HighlightWords
        {
            get;
            private set;
        }

        public bool IsLogged
        {
            get;
            set;
        }

        public Run NickRun
        {
            get
            {
                return this._nickRun;
            }
            set
            {
                if (this._nickRun != value)
                {
                    this._nickRun = value;
                    this._nickRun.MouseLeftButtonDown += this.MouseClick;
                    this.UpdateNickStyle();
                }
            }
        }

        public User Sender
        {
            get;
            private set;
        }

        public MessageSetting Style
        {
            get;
            private set;
        }

        public string Text
        {
            get;
            private set;
        }

        public DateTime Time
        {
            get;
            private set;
        }

        public Message(User sender, string text, MessageSetting setting, DateTime time, bool isLogged = false)
        {
            this.Sender = sender;
            this.Text = text;
            this.Style = setting;
            this.Time = time;
            this.IsLogged = isLogged;

            sender.Messages.Add(this);

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
                    {
                        this.AddHighlightWord(group.Index, group.Length, Message.HightLightTypes.URI);
                    }
                }
            }

            if (setting.Type == MessageTypes.Channel)
            {
                sender.PropertyChanged += this.SenderPropertyChanged;
            }
        }

        public void AddHighlightWord(int idx, int length, HightLightTypes type)
        {
            if (this.HighlightWords == null)
            {
                this.HighlightWords = new SortedDictionary<int, KeyValuePair<int, HightLightTypes>>();
            }

            // Handling overlapping.. eg. when notificator finds *, but the message contains url (#Help -> !port)
            // The logic is that newly added item can not conflict with already added item
            Dictionary<int, int> addRanges = new Dictionary<int, int>();
            KeyValuePair<int, int> tempRange = new KeyValuePair<int, int>(idx, length);
            foreach (var item in this.HighlightWords)
            {
                if (item.Key >= tempRange.Key && tempRange.Key + tempRange.Value >= item.Key)
                {
                    int newLength = item.Key - tempRange.Key;
                    if (newLength > 0)
                    {
                        if (tempRange.Value < newLength)
                        {
                            newLength = tempRange.Value;
                        }
                        addRanges.Add(tempRange.Key, newLength);
                    }
                    tempRange = new KeyValuePair<int, int>(item.Key + item.Value.Key, length - item.Key - item.Value.Key);
                }
            }
            if (tempRange.Value > 0)
            {
                addRanges.Add(tempRange.Key, tempRange.Value);
            }

            foreach (var item in addRanges)
            {
                this.HighlightWords[item.Key] = new KeyValuePair<int, HightLightTypes>(item.Value, type);
            }
        }

        private void MouseClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MainViewModel.Instance.SelectedGLChannel.OpenChatCommand.Execute(this.Sender);
            }
        }

        private void SenderPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this.NickRun != null && e.PropertyName == "OnlineStatus")
            {
                this.UpdateNickStyle();
            }
        }

        public void UpdateNickStyle()
        {
            if (this._nickRun == null)
            {
                return; // If message is not displayed
            }

            this.NickRun.FontStyle = FontStyles.Normal;
            this.NickRun.FontWeight = FontWeights.Bold;

            switch (this.Sender.OnlineStatus)
            {
                case User.Status.Online:
                    // Instant color
                    SolidColorBrush b;
                    if (MainViewModel.Instance.InstantColors.TryGetValue(this.Sender, out b))
                    {
                        this.NickRun.Foreground = b;
                    }
                    // Group color
                    else if (this.Sender.Group.ID != UserGroups.SystemGroupID)
                    {
                        this.NickRun.Foreground = this.Sender.Group.TextColor;
                        this.NickRun.FontStyle = FontStyles.Italic;
                    }
                    else
                    {
                        this.NickRun.Foreground = this.Style.NickColor;
                    }
                    break;

                case User.Status.Offline:
                    this.NickRun.Foreground = Brushes.Red;
                    break;

                case User.Status.Unknown:
                    this.NickRun.Foreground = Brushes.Goldenrod;
                    break;
            }
        }

        public void Dispose()
        {
            this._nickRun = null;
            this.Sender.Messages.Remove(this);
        }
    }
}