namespace GreatSnooper.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;
    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;
    using GreatSnooper.Services;
    using GreatSnooper.ViewModel;

    [DebuggerDisplay("{Sender.Name}: {Text}")]
    public class Message
    {
        public class MessageHighlight : IComparable
        {
            public int StartCharPos { get; private set; }
            public int LastCharPos { get; private set; }
            public int Length { get; private set; }
            public HightLightTypes Type { get; private set; }

            public MessageHighlight(int start, int length, HightLightTypes type)
            {
                StartCharPos = start;
                Length = length;
                Type = type;
                LastCharPos = StartCharPos + Length - 1;
            }

            public int CompareTo(object obj)
            {
                return StartCharPos.CompareTo(((MessageHighlight)obj).StartCharPos);
            }

            public MessageHighlight SubstractBefore(MessageHighlight substract)
            {
                if (StartCharPos < substract.StartCharPos)
                {
                    return new MessageHighlight(StartCharPos, substract.StartCharPos - StartCharPos, Type);
                }
                return null;
            }

            public MessageHighlight SubstractAfter(MessageHighlight substract)
            {
                if (LastCharPos > substract.LastCharPos)
                {
                    return new MessageHighlight(substract.LastCharPos + 1, LastCharPos - substract.LastCharPos, Type);
                }
                return null;
            }

            public bool IsOverlapping(MessageHighlight a)
            {
                // http://stackoverflow.com/questions/13513932/algorithm-to-detect-overlapping-periods
                return StartCharPos <= a.LastCharPos && a.StartCharPos <= LastCharPos;
            }
        }

        protected static Regex urlRegex = new Regex(@"\b(ht|f)tps?://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static Regex urlRegex2 = new Regex(@"\bwww\.\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private Run _nickRun;

        public enum HightLightTypes
        {
            Highlight, LeagueFound, NotificatorFound, URI
        }

        public enum MessageTypes
        {
            Channel, Join, Quit, Part, Offline, Action, User, Notice, Hyperlink, Time, League
        }

        public MySortedList<MessageHighlight> HighlightParts { get; private set; }

        public bool IsLogged { get; set; }

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

        public AbstractChannelViewModel Channel { get; private set; }
        public User Sender { get; private set; }

        public MessageSetting Style { get; private set; }

        public string Text { get; private set; }

        public DateTime Time { get; private set; }

        public Message(AbstractChannelViewModel channel, User sender, string text, MessageSetting setting, DateTime time, bool isLogged = false)
        {
            this.Channel = channel;
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
                HandleUrlMatches(matches);
                matches = urlRegex2.Matches(text);
                HandleUrlMatches(matches);
            }

            if (setting.Type == MessageTypes.Channel)
            {
                sender.ChannelCollection.CollectionChanged += UserStateChanged;
                sender.PropertyChanged += SenderPropertyChanged;
            }
        }

        private void HandleUrlMatches(MatchCollection matches)
        {
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

        public void AddHighlightWord(int idx, int length, HightLightTypes type)
        {
            if (this.HighlightParts == null)
            {
                this.HighlightParts = new MySortedList<MessageHighlight>();
            }

            // Handling overlapping.. eg. when notificator finds *, but the message contains url (#Help -> !port)
            // The logic is that newly added item can not conflict with already added item
            List<MessageHighlight> toAdd = new List<MessageHighlight>();
            MessageHighlight tempHightlight = new MessageHighlight(idx, length, type);

            foreach (MessageHighlight highlight in this.HighlightParts)
            {
                if (highlight.IsOverlapping(tempHightlight))
                {
                    // Since highlights are ordered the part before the overlap can be added to highlight parts..
                    MessageHighlight before = tempHightlight.SubstractBefore(highlight);
                    if (before != null)
                    {
                        toAdd.Add(before);
                    }
                    // .. and only the part after the overlap needs to be further processed (checking other highlight parts whether they overlap
                    tempHightlight = tempHightlight.SubstractAfter(highlight);
                    if (tempHightlight == null)
                    {
                        break;
                    }
                }
            }

            if (tempHightlight != null)
            {
                toAdd.Add(tempHightlight);
            }

            foreach (MessageHighlight highlight in toAdd)
            {
                this.HighlightParts.Add(highlight);
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
            if (e.PropertyName == "OnlineStatus")
            {
                this.UpdateNickStyle();
            }
        }

        private void UserStateChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AbstractChannelViewModel chvm = null; ;
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                chvm = e.NewItems[0] as AbstractChannelViewModel;
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                chvm = e.OldItems[0] as AbstractChannelViewModel;
            }

            if (chvm != null && chvm == Channel)
            {
                this.UpdateNickStyle();
            }
        }

        public void UpdateNickStyle()
        {
            if (this._nickRun == null)
            {
                return; // If message is not displayed yet
            }

            this.NickRun.FontStyle = FontStyles.Normal;
            this.NickRun.FontWeight = FontWeights.Bold;

            switch (this.Sender.OnlineStatus)
            {
                case User.Status.Online:
                    if (this.Style.Type != MessageTypes.Channel || this.Sender.ChannelCollection.AllChannels.Contains(this.Channel))
                    {
                        // Instant color
                        SolidColorBrush b;
                        if (InstantColors.Instance.TryGetValue(this.Sender, out b))
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
                    }
                    else
                    {
                        this.NickRun.Foreground = Brushes.Goldenrod;
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

        public MessageHighlight tempHighlight { get; set; }
    }
}