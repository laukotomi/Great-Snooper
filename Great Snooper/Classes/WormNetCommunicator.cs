namespace GreatSnooper.Classes
{
    using System.Collections.Generic;

    using GreatSnooper.Helpers;

    public class WormNetCommunicator : AbstractCommunicator
    {
        public WormNetCommunicator(string serverAddress, int serverPort)
            : base(serverAddress, serverPort, true, false, false, false)
        {
        }

        public override string VerifyString(string str)
        {
            return WormNetCharTable.RemoveNonWormNetChars(str.TrimEnd());
        }

        protected override int DecodeMessage(string message)
        {
            if (message == "LIST")
            {
                this._channelListHelper = new SortedDictionary<string, string>(GlobalManager.CIStringComparer);
            }

            int i = 0;
            for (; i < message.Length; i++)
            {
                this._sendBuffer[i] = WormNetCharTable.Encode[message[i]];
            }
            this._sendBuffer[i++] = WormNetCharTable.Encode['\r'];
            this._sendBuffer[i++] = WormNetCharTable.Encode['\n'];
            return i;
        }

        protected override void EncodeMessage(int bytes)
        {
            for (int i = 0; i < bytes; i++)
            {
                _recvMessage.Append(WormNetCharTable.Decode[_recvBuffer[i]]);    // Decode the bytes into RecvMessage
            }
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