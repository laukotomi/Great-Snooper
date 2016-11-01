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
                    User u = UserHelper.GetUser(Sender, ClientName, Clan);
                    if (u.OnlineStatus != User.Status.Online)
                    {
                        u.OnlineStatus = User.Status.Online;
                        if (Properties.Settings.Default.UseWhoMessages)
                        {
                            this.Sender.GetInfoAboutClient(this, ClientName);
                            u.AddToChannel.Add(chvm); // Client will be added to the channel if information is arrived to keep the client list sorted properly
                        }
                        else
                        {
                            Message msg = new Message(u, Localizations.GSLocalization.Instance.JoinMessage, MessageSettings.JoinMessage, DateTime.Now);

                            if (Notificator.Instance.SearchInJoinMessagesEnabled && Notificator.Instance.JoinMessagesRegex.IsMatch(u.Name))
                            {
                                msg.AddHighlightWord(0, msg.Text.Length, Message.HightLightTypes.NotificatorFound);
                                chvm.MainViewModel.NotificatorFound(string.Format(Localizations.GSLocalization.Instance.NotifOnlineMessage, u.Name, chvm.Name));
                            }
                            else if (u.Group.ID != UserGroups.SystemGroupID)
                            {
                                if (Properties.Settings.Default.TrayNotifications)
                                    mvm.ShowTrayMessage(string.Format(Localizations.GSLocalization.Instance.OnlineMessage, u.Name));
                                if (u.Group.SoundEnabled)
                                    Sounds.PlaySound(u.Group.Sound);
                            }

                            chvm.AddUser(u);
                            chvm.AddMessage(msg);
                        }

                        foreach (var channel in u.PMChannels)
                            channel.AddMessage(u, Localizations.GSLocalization.Instance.PMOnlineMessage, MessageSettings.JoinMessage);
                    }
                    else if (u.Channels.Contains(chvm) == false)
                    {
                        chvm.AddUser(u);
                        chvm.AddMessage(u, Localizations.GSLocalization.Instance.JoinMessage, MessageSettings.JoinMessage);
                    }
                }
            }
            else if (chvm.Joined == false) // We joined a channel
            {
                if (Properties.Settings.Default.LoadChannelScheme && Sender is WormNetCommunicator && string.IsNullOrEmpty(chvm.Scheme) && (chvm.ChannelSchemeTask == null || chvm.ChannelSchemeTask.IsCompleted))
                {
                    chvm.TryGetChannelScheme(() =>
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
