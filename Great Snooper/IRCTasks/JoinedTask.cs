namespace GreatSnooper.IRCTasks
{
    using System;
    using System.Linq;
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;
    using GreatSnooper.Services;
    using GreatSnooper.ViewModel;

    public class JoinedTask : IRCTask
    {
        private string _channelHash;
        private string _clientName;
        private string _clan;

        public JoinedTask(IRCCommunicator server, string channelHash, string clientName, string clan)
            : base(server)
        {
            _channelHash = channelHash;
            _clientName = clientName;
            _clan = clan;
        }

        public override void DoTask(MainViewModel mvm)
        {
            AbstractChannelViewModel temp;
            if (!_server.Channels.TryGetValue(_channelHash, out temp))
            {
                if (_server.HandleJoinRequest)
                {
                    temp = new ChannelViewModel(mvm, _server, _channelHash, string.Empty);
                }
                else
                {
                    return;
                }
            }

            ChannelViewModel chvm = temp as ChannelViewModel;
            if (chvm == null)
            {
                return;
            }

            if (_clientName.Equals(_server.User.Name, StringComparison.OrdinalIgnoreCase) == false)
            {
                if (chvm.Joined)
                {
                    User user = UserHelper.GetUser(_server, _clientName, _clan);
                    if (user.OnlineStatus != User.Status.Online)
                    {
                        user.OnlineStatus = User.Status.Online;
                        if (Properties.Settings.Default.UseWhoMessages)
                        {
                            _server.GetInfoAboutClient(this, _clientName);
                            user.AddToChannel.Add(chvm); // Client will be added to the channel if information is arrived to keep the client list sorted properly
                        }
                        else
                        {
                            chvm.AddUser(user);
                        }

                        Message msg = new Message(chvm, user, Localizations.GSLocalization.Instance.JoinMessage, MessageSettings.JoinMessage, DateTime.Now);

                        if (Notificator.Instance.SearchInJoinMessagesEnabled &&
                            Notificator.Instance.JoinMessages.Any(r => r.IsMatch(user.Name, user.Name, chvm.Name)))
                        {
                            msg.AddHighlightWord(0, msg.Text.Length, Message.HightLightTypes.NotificatorFound);
                            chvm.MainViewModel.NotificatorFound(string.Format(Localizations.GSLocalization.Instance.NotifOnlineMessage, user.Name, chvm.Name), chvm);
                        }
                        else if (user.Group.ID != UserGroups.SystemGroupID)
                        {
                            if (Properties.Settings.Default.TrayNotifications)
                            {
                                mvm.ShowTrayMessage(string.Format(Localizations.GSLocalization.Instance.OnlineMessage, user.Name), chvm);
                            }
                            if (user.Group.SoundEnabled)
                            {
                                Sounds.PlaySound(user.Group.Sound);
                            }
                        }
                        user.Messages.Add(msg);
                        chvm.AddMessage(msg);

                        foreach (PMChannelViewModel channel in user.ChannelCollection.PmChannels)
                        {
                            channel.AddMessage(user, Localizations.GSLocalization.Instance.PMOnlineMessage, MessageSettings.JoinMessage);
                        }
                    }
                    else
                    {
                        if (user.AddToChannel.Count > 0)
                        {
                            user.AddToChannel.Add(chvm); // Client will be added to the channel if information is arrived to keep the client list sorted properly
                        }
                        else if (user.ChannelCollection.Channels.Contains(chvm) == false)
                        {
                            chvm.AddUser(user);
                        }
                        chvm.AddMessage(user, Localizations.GSLocalization.Instance.JoinMessage, MessageSettings.JoinMessage);
                    }
                }
            }
            else if (chvm.Joined == false) // We joined a channel
            {
                if (Properties.Settings.Default.LoadChannelScheme && _server is WormNetCommunicator &&
                    string.IsNullOrEmpty(chvm.Scheme) && (chvm.ChannelSchemeTask == null || chvm.ChannelSchemeTask.IsCompleted))
                {
                    chvm.TryGetChannelScheme(() =>
                    {
                        FinishJoin(chvm);
                    });
                }
                else
                {
                    this.FinishJoin(chvm);
                }
            }
        }

        private void FinishJoin(ChannelViewModel chvm)
        {
            chvm.FinishJoin();

            if (chvm.MainViewModel.SelectedChannel == chvm)
            {
                if (chvm.CanHost)
                {
                    chvm.MainViewModel.GameListRefresh = true;
                }
                chvm.MainViewModel.DialogService.GetView().UpdateLayout();
                chvm.IsTBFocused = true;
            }
        }
    }
}