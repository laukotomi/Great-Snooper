using System;
using System.Collections.Generic;
using System.Text;
using GreatSnooper.Helpers;
using GreatSnooper.Model;

namespace GreatSnooper.Classes
{
    public class GameSurgeCommunicator : AbstractCommunicator
    {
        #region Enums

        #endregion

        #region Members

        #endregion

        #region Properties
        public List<string> JoinChannelList { get; set; }
        #endregion

        public GameSurgeCommunicator(string serverAddress, int serverPort)
            : base(serverAddress, serverPort, false, true, true, true)
        {
            JoinChannelList = new List<string>();
        }

        protected override void SetUser()
        {
            if (Properties.Settings.Default.WormsNick.Length > 0)
            {
                this.User = new User(Properties.Settings.Default.WormsNick, GlobalManager.User.Clan);
                this.User.Country = GlobalManager.User.Country;
                this.User.Rank = GlobalManager.User.Rank;
            }
            else
                this.User = GlobalManager.User;
        }

        protected override void EncodeMessage(int bytes)
        {
            recvMessage.Append(Encoding.UTF8.GetString(recvBuffer, 0, bytes));
        }

        protected override int DecodeMessage(string message)
        {
            int i = 0;
            try
            {
                i = Encoding.UTF8.GetBytes(message, 0, message.Length, sendBuffer, 0);
                i += Encoding.UTF8.GetBytes("\r\n", 0, 2, sendBuffer, i);
                return i;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public override string VerifyString(string str)
        {
            return str.TrimEnd();
        }
    }
}
