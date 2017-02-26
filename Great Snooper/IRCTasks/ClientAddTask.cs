﻿namespace GreatSnooper.IRCTasks
{
    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    class ClientAddTask : IRCTask
    {
        public ClientAddTask(AbstractCommunicator sender, string channelHash, string senderName, string clientName)
        {
            this.Sender = sender;
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
            if (!Sender.Channels.TryGetValue(this.ChannelHash, out temp) || temp.GetType() != typeof(PMChannelViewModel) || temp.Joined == false)
            {
                return;
            }

            var chvm = (PMChannelViewModel)temp;
            User u1 = null;
            if (!Sender.Users.TryGetValue(this.ClientName, out u1) || chvm.IsUserInConversation(u1))
            {
                return;
            }

            User u2 = null;
            if (!Sender.Users.TryGetValue(this.SenderName, out u2) || !chvm.IsUserInConversation(u2))
            {
                return;
            }

            chvm.AddUserToConversation(u1, false);
            chvm.AddMessage(GlobalManager.SystemUser, string.Format(Localizations.GSLocalization.Instance.ConversationAdded, this.SenderName, this.ClientName), MessageSettings.SystemMessage);
        }
    }
}