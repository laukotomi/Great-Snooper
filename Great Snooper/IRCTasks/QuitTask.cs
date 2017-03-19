namespace GreatSnooper.IRCTasks
{
    using System;
    using System.Collections.Generic;
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    class QuitTask : IRCTask
    {
        public QuitTask(IRCCommunicator server, string clientName, string message)
            : base(server)
        {
            this.ClientName = clientName;
            this.Message = message;
        }

        public string ClientName
        {
            get;
            private set;
        }

        public string Message
        {
            get;
            private set;
        }

        public override void DoTask(MainViewModel mwm)
        {
            User u;
            if (_server.Users.TryGetValue(this.ClientName, out u))
            {
                string msg;
                if (_server is WormNetCommunicator)
                {
                    if (this.Message.Length > 0)
                    {
                        msg = string.Format(Localizations.GSLocalization.Instance.WNQuitWMessage, this.Message);
                    }
                    else
                    {
                        msg = Localizations.GSLocalization.Instance.WNQuitWOMessage;
                    }
                }
                else
                {
                    if (this.Message.Length > 0)
                    {
                        msg = string.Format(Localizations.GSLocalization.Instance.GSQuitWMessage, this.Message);
                    }
                    else
                    {
                        msg = Localizations.GSLocalization.Instance.GSQuitWOMessage;
                    }
                }

                u.OnlineStatus = User.Status.Offline;

                // Send quit message to the channels where the user was active
                var temp = new HashSet<ChannelViewModel>(u.ChannelCollection.Channels);
                foreach (var chvm in temp)
                {
                    if (chvm.Joined)
                    {
                        chvm.AddMessage(u, msg, MessageSettings.QuitMessage);
                        chvm.RemoveUser(u);
                    }
                }

                // If we had a private chat with the user
                if (u.ChannelCollection.PmChannels.Count != 0)
                {
                    bool pingTimeout = this.Message == "Ping timeout: 180 seconds";
                    DateTime threeMinsBefore = DateTime.Now - new TimeSpan(0, 3, 0);

                    foreach (PMChannelViewModel chvm in u.ChannelCollection.PmChannels)
                    {
                        chvm.AddMessage(u, msg, MessageSettings.QuitMessage);

                        // Check if we wanted to send any messages that the user couldn't receive
                        if (pingTimeout && chvm.Messages.Count > 0)
                        {
                            var lastMsgNode = chvm.Messages.Last;
                            while (lastMsgNode != null)
                            {
                                if (lastMsgNode.Value.Time < threeMinsBefore)
                                {
                                    break;
                                }
                                else if (lastMsgNode.Value.Style.Type == Model.Message.MessageTypes.User)
                                {
                                    chvm.AddMessage(GlobalManager.SystemUser, Localizations.GSLocalization.Instance.PMPingTimeoutMessage, MessageSettings.SystemMessage);
                                    chvm.Highlight();
                                    break;
                                }
                                lastMsgNode = lastMsgNode.Previous;
                            }
                        }
                    }
                }
            }
        }
    }
}