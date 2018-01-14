namespace GreatSnooper.IRC
{
    using System.Collections.Generic;

    using GreatSnooper.Helpers;

    public class WormNetCommunicator : IRCCommunicator
    {
        public WormNetCommunicator(string serverAddress, int serverPort)
            : base(serverAddress, serverPort, true, false, false, false)
        {
        }

        public override string VerifyString(string str)
        {
            return WormNetCharTable.Instance.RemoveNonWormNetChars(str.TrimEnd());
        }

        protected override int DecodeMessage(string message)
        {
            if (message == "LIST")
            {
                this._channelListHelper = new SortedDictionary<string, string>(GlobalManager.CIStringComparer);
            }

            int i = WormNetCharTable.Instance.GetBytes(message, 0, message.Length, _sendBuffer, 0);
            i += WormNetCharTable.Instance.GetBytes("\r\n", 0, 2, _sendBuffer, i);
            return i;
        }

        protected override string DecodeMessage(byte[] bytes, int length)
        {
            return WormNetCharTable.Instance.Decode(bytes, length);
        }

        protected override void SendPassword()
        {
            Send(this, "PASS ELSILRACLIHP");
        }

        protected override void SetUser()
        {
            this.User = GlobalManager.User;
        }
    }
}