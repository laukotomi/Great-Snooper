namespace GreatSnooper.IRCTasks
{
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    public class OfflineTask : IRCTask
    {
        public OfflineTask(IRCCommunicator server, string clientName)
            : base(server)
        {
            this.ClientName = clientName;
        }

        public string ClientName
        {
            get;
            private set;
        }

        public override void DoTask(MainViewModel mvm)
        {
            User u;
            // Send a message to the private message channel that the user is offline
            if (_server.Users.TryGetValue(this.ClientName, out u))
            {
                u.OnlineStatus = User.Status.Offline;
                if (u.ChannelCollection.PmChannels.Count > 0)
                {
                    foreach (var chvm in u.ChannelCollection.PmChannels)
                    {
                        chvm.AddMessage(GlobalManager.SystemUser, string.Format(Localizations.GSLocalization.Instance.OfflineMessage, this.ClientName), MessageSettings.SystemMessage);
                    }
                }
                else
                {
                    mvm.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, string.Format(Localizations.GSLocalization.Instance.OfflineMessage, this.ClientName));
                }
            }
            else
            {
                mvm.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, string.Format(Localizations.GSLocalization.Instance.OfflineMessage, this.ClientName));
            }
        }
    }
}