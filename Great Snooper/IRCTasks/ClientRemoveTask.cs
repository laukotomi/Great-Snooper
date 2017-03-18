namespace GreatSnooper.IRCTasks
{
    using System;
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    class ClientRemoveTask : IRCTask
    {
        public ClientRemoveTask(IRCCommunicator server, string channelHash, string senderName, string clientName)
            : base(server)
        {
            this.ChannelHash = channelHash;
            this.SenderName = senderName;
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

        public string SenderName
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

            User u2 = null;
            if (!_server.Users.TryGetValue(this.SenderName, out u2) || !chvm.IsUserInConversation(u2))
            {
                return;
            }

            if (this.ClientName.Equals(_server.User.Name, StringComparison.OrdinalIgnoreCase))
            {
                chvm.AddMessage(GlobalManager.SystemUser, Localizations.GSLocalization.Instance.ConversationKick, MessageSettings.SystemMessage);
                chvm.Disabled = true;
            }
            else
            {
                User u1 = null;
                if (!_server.Users.TryGetValue(this.ClientName, out u1) || !chvm.IsUserInConversation(u1))
                {
                    return;
                }

                chvm.RemoveUserFromConversation(u1, false);
                chvm.AddMessage(GlobalManager.SystemUser, string.Format(Localizations.GSLocalization.Instance.ConversationRemoved, this.SenderName, this.ClientName), MessageSettings.SystemMessage);
            }
        }
    }
}