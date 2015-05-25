using System;
using System.Collections.Generic;

namespace MySnooper
{
    public enum MessageTypes { Channel, Join, Quit, Part, Offline, Action, User, Notice, BuddyJoined, Hyperlink, Time, League }
    public enum HightLightTypes { Highlight, LeagueFound, NotificatorFound }

    public class MessageClass
    {
        public Client Sender { get; private set; }
        public string Message { get; private set; }
        public DateTime Time { get; private set; }
        public MessageSetting Style { get; private set; }
        public string[] Words { get; set; }
        public Dictionary<int, HightLightTypes> HighlightWords { get; set; }

        public MessageClass(Client Sender, string Message, MessageSetting setting)
        {
            this.Sender = Sender;
            this.Message = Message;
            this.Style = setting;
            this.Time = DateTime.Now;
        }

        public void AddHighlightWord(int index, HightLightTypes type)
        {
            if (HighlightWords == null)
                HighlightWords = new Dictionary<int, HightLightTypes>();
            HighlightWords.Add(index, type);
        }
    }
}
