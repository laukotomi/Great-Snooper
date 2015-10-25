using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.ViewModel;
using System;

namespace GreatSnooper.IRCTasks
{
    public class JoinedTask : IRCTask
    {
        public string ChannelHash { get; private set; }
        public string ClientName { get; private set; }
        public string Clan { get; private set; }

        public JoinedTask(AbstractCommunicator sender, string channelHash, string clientName, string clan)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.ClientName = clientName;
            this.Clan = clan;
        }

        public override void DoTask(MainViewModel mvm)
        {
            AbstractChannelViewModel temp;
            if (!Sender.Channels.TryGetValue(ChannelHash, out temp))
            {
                if (Sender.HandleJoinRequest)
                    temp = new ChannelViewModel(mvm, this.Sender, ChannelHash, "");
                else
                    return;
            }

            var chvm = (ChannelViewModel)temp;

            if (ClientName.Equals(Sender.User.Name, StringComparison.OrdinalIgnoreCase) == false)
            {
                if (chvm.Joined)
                {
                    User u;
                    if (!Sender.Users.TryGetValue(ClientName, out u))// Register the new client
                        u = Users.CreateUser(Sender, ClientName, Clan);

                    if (u.OnlineStatus != User.Status.Online)
                    {
                        this.Sender.GetInfoAboutClient(this, ClientName);
                        u.OnlineStatus = User.Status.Online;
                        u.AddToChannel.Add(chvm); // Client will be added to the channel if information is arrived to keep the client list sorted properly

                        foreach (var channel in u.PMChannels)
                            channel.AddMessage(u, Localizations.GSLocalization.Instance.PMOnlineMessage, MessageSettings.JoinMessage);
                    }
                    else if (u.Channels.Contains(chvm) == false)
                    {
                        chvm.AddUser(u);
                        chvm.AddMessage(u, Localizations.GSLocalization.Instance.JoinMessage, MessageSettings.JoinMessage);
                    }
                    else return;

                    if (Notificator.Instance.SearchInJoinMessagesEnabled && Notificator.Instance.JoinMessagesRegex.IsMatch(u.Name))
                        chvm.MainViewModel.NotificatorFound(string.Format(Localizations.GSLocalization.Instance.NotifOnlineMessage, u.Name, chvm.Name));
                    else if (u.Group.ID != UserGroups.SystemGroupID)
                    {
                        if (Properties.Settings.Default.TrayNotifications)
                            chvm.MainViewModel.TaskbarIconService.ShowMessage(string.Format(Localizations.GSLocalization.Instance.OnlineMessage, u.Name));
                        if (u.Group.SoundEnabled)
                            Sounds.PlaySound(u.Group.Sound);
                    }
                }
            }
            else if (chvm.Joined == false) // We joined a channel
            {
                if (Sender is WormNetCommunicator && chvm.Scheme == string.Empty)
                {
                    chvm.MainViewModel.GetChannelScheme(chvm, () =>
                    {
                        FinishJoin(chvm);
                    });
                }
                else
                    FinishJoin(chvm);
            }
        }

        private void FinishJoin(ChannelViewModel chvm)
        {
            chvm.FinishJoin();

            if (chvm.MainViewModel.SelectedChannel == chvm)
            {
                if (chvm.CanHost)
                    chvm.MainViewModel.GameListRefresh = true;
                chvm.MainViewModel.DialogService.GetView().UpdateLayout();
                chvm.IsTBFocused = true;
            }
        }
    }
}
