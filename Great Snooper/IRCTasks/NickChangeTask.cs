namespace GreatSnooper.IRCTasks
{
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    public class NickChangeTask : IRCTask
    {
        public NickChangeTask(IRCCommunicator server, string oldClientName, string newClientName)
            : base(server)
        {
            this.OldClientName = oldClientName;
            this.NewClientName = newClientName;
        }

        public string NewClientName
        {
            get;
            private set;
        }

        public string OldClientName
        {
            get;
            private set;
        }

        public override void DoTask(MainViewModel mvm)
        {
            User u;
            if (_server.Users.TryGetValue(this.OldClientName, out u))
            {
                if (u == _server.User)
                {
                    _server.User.Name = this.NewClientName;
                }

                // To keep SortedDictionary sorted, first client will be removed..
                foreach (ChannelViewModel chvm in u.ChannelCollection.Channels)
                {
                    if (chvm.Joined)
                    {
                        chvm.Users.Remove(u);
                    }
                }
                foreach (PMChannelViewModel chvm in u.ChannelCollection.PmChannels)
                {
                    chvm.RemoveUserFromConversation(u, false, false);
                }

                u.Name = this.NewClientName;
                _server.Users.Remove(this.OldClientName);
                _server.Users.Add(u.Name, u);

                // then later it will be readded with new Name
                foreach (var chvm in u.ChannelCollection.Channels)
                {
                    if (chvm.Joined)
                    {
                        chvm.Users.Add(u);
                        chvm.AddMessage(GlobalManager.SystemUser, string.Format(Localizations.GSLocalization.Instance.GSNicknameChange, this.OldClientName, this.NewClientName), MessageSettings.SystemMessage);
                    }
                }
                foreach (var chvm in u.ChannelCollection.PmChannels)
                {
                    chvm.AddUserToConversation(u, false, false);
                    chvm.AddMessage(GlobalManager.SystemUser, string.Format(Localizations.GSLocalization.Instance.GSNicknameChange, this.OldClientName, this.NewClientName), MessageSettings.SystemMessage);
                }
            }
        }
    }
}