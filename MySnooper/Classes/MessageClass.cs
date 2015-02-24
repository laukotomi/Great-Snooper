using System;

namespace MySnooper
{
    public enum MessageTypes { Channel, Join, Quit, Part, Offline, Action, User, Notice, BuddyJoined, Hyperlink, Time, League }

    public class MessageClass
    {
        public Client Sender { get; private set; }
        public string Message { get; private set; }
        public DateTime Time { get; private set; }
        public MessageSetting Style { get; private set; }

        public MessageClass(Client Sender, string Message, MessageSetting setting)
        {
            this.Sender = Sender;
            this.Message = Message;
            this.Style = setting;
            this.Time = DateTime.Now;
        }
    }
}
