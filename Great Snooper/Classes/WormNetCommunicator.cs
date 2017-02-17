using System.Collections.Generic;
using GreatSnooper.Helpers;

namespace GreatSnooper.Classes
{
    public class WormNetCommunicator : AbstractCommunicator
    {
        #region Enums

        #endregion

        #region Members

        #endregion

        #region Properties

        #endregion

        public WormNetCommunicator(string serverAddress, int serverPort)
            : base(serverAddress, serverPort, true, false, false, false)
        {

        }

        protected override void SetUser()
        {
            this.User = GlobalManager.User;
        }

        protected override void SendPassword()
        {
            Send(this, "PASS ELSILRACLIHP");
        }

        protected override void EncodeMessage(int bytes)
        {
            for (int i = 0; i < bytes; i++)
                recvMessage.Append(WormNetCharTable.Decode[recvBuffer[i]]); //Decode the bytes into RecvMessage
        }

        protected override int DecodeMessage(string message)
        {
            if (message == "LIST")
                channelListHelper = new SortedDictionary<string, string>(GlobalManager.CIStringComparer);

            int i = 0;
            for (; i < message.Length; i++)
            {
                sendBuffer[i] = WormNetCharTable.Encode[message[i]];
            }
            sendBuffer[i++] = WormNetCharTable.Encode['\r'];
            sendBuffer[i++] = WormNetCharTable.Encode['\n'];
            return i;
        }

        public override string VerifyString(string str)
        {
            return WormNetCharTable.RemoveNonWormNetChars(str.TrimEnd());
        }
    }
}
