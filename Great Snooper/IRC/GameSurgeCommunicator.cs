namespace GreatSnooper.IRC
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using GreatSnooper.Helpers;
    using GreatSnooper.Model;

    public class GameSurgeCommunicator : IRCCommunicator
    {
        public GameSurgeCommunicator(string serverAddress, int serverPort)
            : base(serverAddress, serverPort, false, true, true, true)
        {
            JoinChannelList = new List<string>();
        }

        public List<string> JoinChannelList
        {
            get;
            set;
        }

        public override string VerifyString(string str)
        {
            return str.TrimEnd();
        }

        protected override int DecodeMessage(string message)
        {
            try
            {
                int i = Encoding.UTF8.GetBytes(message, 0, message.Length, _sendBuffer, 0);
                i += Encoding.UTF8.GetBytes("\r\n", 0, 2, _sendBuffer, i);
                return i;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        protected override string DecodeMessage(byte[] bytes, int length)
        {
            return Encoding.UTF8.GetString(bytes, 0, length);
        }

        protected override void SetUser()
        {
            if (Properties.Settings.Default.WormsNick.Length > 0)
            {
                this.User = new User(this, Properties.Settings.Default.WormsNick, GlobalManager.User.Clan);
                this.User.SetUserInfo(GlobalManager.User.Country, GlobalManager.User.Rank, App.GetFullVersion());
            }
            else
            {
                this.User = GlobalManager.User;
            }
        }
    }
}