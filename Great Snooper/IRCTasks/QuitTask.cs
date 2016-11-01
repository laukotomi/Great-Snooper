using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.ViewModel;
using System;
using System.Collections.Generic;

namespace GreatSnooper.IRCTasks
{
    class QuitTask : IRCTask
    {
        public string ClientName { get; private set; }
        public string Message { get; private set; }

        public QuitTask(AbstractCommunicator sender, string clientName, string message)
        {
            this.Sender = sender;
            this.ClientName = clientName;
            this.Message = message;
        }

        public override void DoTask(MainViewModel mwm)
        {
            User u;
            if (Sender.Users.TryGetValue(ClientName, out u))
            {
                string msg;
                if (Sender is WormNetCommunicator)
                {
                    if (Message.Length > 0)
                        msg = string.Format(Localizations.GSLocalization.Instance.WNQuitWMessage, Message);
                    else
                        msg = Localizations.GSLocalization.Instance.WNQuitWOMessage;
                }
                else
                {
                    if (Message.Length > 0)
                        msg = string.Format(Localizations.GSLocalization.Instance.GSQuitWMessage, Message);
                    else
                        msg = Localizations.GSLocalization.Instance.GSQuitWOMessage;
                }

                // Send quit message to the channels where the user was active
                var temp = new HashSet<ChannelViewModel>(u.Channels);
                foreach (var chvm in temp)
                {
                    if (chvm.Joined)
                    {
                        chvm.AddMessage(u, msg, MessageSettings.QuitMessage);
                        chvm.RemoveUser(u);
                    }
                }
                u.Channels.Clear();

                if (u.PMChannels.Count == 0)
                    UserHelper.FinalizeUser(Sender, u);
                // If we had a private chat with the user
                else
                {
                    u.OnlineStatus = User.Status.Offline;

                    bool pingTimeout = Message == "Ping timeout: 180 seconds";
                    DateTime threeMinsBefore = DateTime.Now - new TimeSpan(0, 3, 0);

                    foreach (var chvm in u.PMChannels)
                    {
                        chvm.AddMessage(u, msg, MessageSettings.QuitMessage);

                        // Check if we wanted to send any messages that the user couldn't receive
                        if (pingTimeout && chvm.Messages.Count > 0)
                        {
                            var lastMsgNode = chvm.Messages.Last;
                            while (lastMsgNode != null)
                            {
                                if (lastMsgNode.Value.Time < threeMinsBefore)
                                    break;
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
