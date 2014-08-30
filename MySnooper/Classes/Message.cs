using System;

namespace MySnooper
{
    public enum MessageTypes { Channel, Join, Quit, Part, Offline, Action, User, Notice, BuddyJoined }

    public class MessageClass
    {
        public Client Sender { get; private set; }
        public string Message { get; private set; }
        public MessageTypes MessageType { get; private set; }
        public DateTime Time { get; private set; }

        public MessageClass(Client Sender, string Message, MessageTypes MessageType)
        {
            this.Sender = Sender;
            this.Message = Message;
            this.MessageType = MessageType;
            this.Time = DateTime.Now;
        }
    }
}
