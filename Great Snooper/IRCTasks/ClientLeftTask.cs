namespace GreatSnooper.IRCTasks
{
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    class ClientLeftTask : IRCTask
    {
        public ClientLeftTask(IRCCommunicator server, string channelHash, string clientName)
            : base(server)
        {
            this.ChannelHash = channelHash;
            this.ClientName = clientName;
        }

        public string ChannelHash
        {
            get;
            private set;
        }

        public string ClientName
        {
            get;
            private set;
        }

        public override void DoTask(MainViewModel mvm)
        {
            AbstractChannelViewModel temp = null;
            if (!_server.Channels.TryGetValue(this.ChannelHash, out temp) || temp.GetType() != typeof(PMChannelViewModel) || temp.Joined == false)
            {
                return;
            }

            var chvm = (PMChannelViewModel)temp;
            User u = null;
            if (!_server.Users.TryGetValue(this.ClientName, out u) || !chvm.IsUserInConversation(u))
            {
                return;
            }

            chvm.RemoveUserFromConversation(u, false);
            chvm.AddMessage(GlobalManager.SystemUser, string.Format(Localizations.GSLocalization.Instance.ConversationLeave, this.ClientName), MessageSettings.SystemMessage);
        }
    }
}