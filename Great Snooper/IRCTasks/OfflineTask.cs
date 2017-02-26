namespace GreatSnooper.IRCTasks
{
    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    public class OfflineTask : IRCTask
    {
        public OfflineTask(AbstractCommunicator sender, string clientName)
        {
            this.Sender = sender;
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
            if (Sender.Users.TryGetValue(this.ClientName, out u))
            {
                u.OnlineStatus = User.Status.Offline;
                if (u.PMChannels.Count > 0)
                {
                    foreach (var chvm in u.PMChannels)
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