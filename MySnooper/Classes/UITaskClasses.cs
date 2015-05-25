using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MySnooper
{
    public abstract class UITask
    {
        public IRCCommunicator Sender { get; protected set; }

        public abstract void DoTask(MainWindow mw);
    }

    public class QuitUITask : UITask
    {
        public string ClientNameL { get; private set; }
        public string Message { get; private set; }

        public QuitUITask(IRCCommunicator sender, string clientNameL, string message)
        {
            this.Sender = sender;
            this.ClientNameL = clientNameL;
            this.Message = message;
        }

        public override void DoTask(MainWindow mw)
        {
            Client c;
            if (Sender.Clients.TryGetValue(ClientNameL, out c))
            {
                string msg;
                if (Sender.IsWormNet)
                {
                    if (Message.Length > 0)
                        msg = "has left WormNet (" + Message + ").";
                    else
                        msg = "has left WormNet.";
                }
                else
                {
                    if (Message.Length > 0)
                        msg = "has left the server (" + Message + ").";
                    else
                        msg = "has left the server.";
                }

                // Send quit message to the channels where the user was active
                var temp = new List<Channel>(c.Channels);
                foreach (Channel ch in temp)
                {
                    if (ch.Joined)
                    {
                        ch.AddMessage(c, msg, MessageSettings.QuitMessage);
                        ch.Clients.Remove(c);
                    }
                }
                c.Channels.Clear();

                if (c.PMChannels.Count == 0)
                    Sender.Clients.Remove(ClientNameL);
                // If we had a private chat with the user
                else
                {
                    c.OnlineStatus = Client.Status.Offline;

                    bool pingTimeout = Message == "Ping timeout: 180 seconds";
                    DateTime threeMinsBefore = DateTime.Now - new TimeSpan(0, 3, 0);

                    for (int i = 0; i < c.PMChannels.Count; i++)
                    {
                        c.PMChannels[i].AddMessage(c, msg, MessageSettings.QuitMessage);
                        
                        // Check if we wanted to send any that the user couldn't receive
                        if (pingTimeout)
                        {
                            Channel ch = c.PMChannels[i];
                            for (int j = ch.Messages.Count - 1; j >= 0; j--)
                            {
                                if (ch.Messages[j].Time < threeMinsBefore)
                                    break;
                                else if (ch.Messages[j].Sender == Sender.User)
                                {
                                    ch.AddMessage(GlobalManager.SystemClient, "There may be messages that this user could not receive!", MessageSettings.OfflineMessage);
                                    if (ch.NewMessages == false && mw.Channels.SelectedItem != null)
                                    {
                                        Channel msgch = (Channel)((TabItem)mw.Channels.SelectedItem).DataContext;
                                        if (msgch != ch || !mw.IsActive)
                                            ch.NewMessages = true;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class MessageUITask : UITask
    {
        private static Regex nickRegex = new Regex(@"[a-z0-9`\-]", RegexOptions.IgnoreCase);

        public string ClientName { get; private set; }
        public string ChannelHash { get; private set; }
        public string Message { get; private set; }
        public MessageSetting Setting { get; private set; }

        public MessageUITask(IRCCommunicator sender, string clientName, string channelHash, string message, MessageSetting setting)
        {
            this.Sender = sender;
            this.ClientName = clientName;
            this.ChannelHash = channelHash;
            this.Message = message;
            this.Setting = setting;
        }

        public override void DoTask(MainWindow mw)
        {
            Client c = null;
            Channel ch = null;
            string fromLow = ClientName.ToLower();

            // If the message arrived in a closed channel
            if (Sender.ChannelList.TryGetValue(ChannelHash, out ch) && !ch.Joined)
                return;

            // If the user doesn't exists we create one
            if (!Sender.Clients.TryGetValue(fromLow, out c))
                c = new Client(ClientName, Sender);

            if (ch == null) // New private message arrived for us
                ch = new Channel(mw, Sender, ChannelHash, "Chat with " + c.Name, c);

            if (!ch.IsPrivMsgChannel)
            {
                MessageClass msg = new MessageClass(c, Message, Setting);
                bool LookForLeague = Setting.Type == MessageTypes.Channel && mw.SearchHere == ch;

                // Search for league or hightlight or notification
                if (LookForLeague || Setting.Type == MessageTypes.Channel)
                {
                    bool notificationSearch = false; // Is there any notificator for messages
                    bool notif = true; // to ensure that only one notification will be sent
                    if (mw.Notifications.Count > 0)
                    {
                        foreach (NotificatorClass nc in mw.Notifications)
                        {
                            if (notif && nc.InMessages)
                            {
                                notificationSearch = true;
                            }
                            if (nc.InMessageSenders && nc.TryMatch(c.LowerName))
                            {
                                notif = false;
                                mw.NotificatorFound(c.Name + ": " + Message, ch);
                                break;
                            }
                        }
                    }

                    string[] words = Message.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    msg.Words = words;
                    for (int i = 0; i < words.Length; i++)
                    {
                        // Highlight
                        if (words[i].StartsWith(Sender.User.Name) && !nickRegex.IsMatch(words[i].Substring(Sender.User.Name.Length)))
                        {
                            msg.AddHighlightWord(i, HightLightTypes.Highlight);

                            Channel selectedCH = null;
                            if (mw.Channels.SelectedItem != null)
                                selectedCH = (Channel)((TabItem)mw.Channels.SelectedItem).DataContext;

                            if (!mw.IsActive || ch != selectedCH)
                            {
                                ch.NewMessages = true;

                                if (Properties.Settings.Default.TrayFlashing && !mw.IsActive)
                                    mw.FlashWindow();
                                if (Properties.Settings.Default.TrayNotifications)
                                    mw.NotifyIcon.ShowBalloonTip(null, "You have been highlighted!", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                                if (ch.BeepSoundPlay && Properties.Settings.Default.HBeepEnabled)
                                {
                                    Sounds.PlaySound("HBeep");
                                    ch.BeepSoundPlay = false;
                                }
                            }
                        }
                        else if (LookForLeague)
                        {
                            // foundUsers.ContainsKey(lower) == league name we are looking for
                            // foundUsers[lower].Contains(c.LowerName) == the user we found for league lower
                            foreach (var item in mw.FoundUsers)
                            {
                                if (words[i].IndexOf(item.Key, StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    msg.AddHighlightWord(i, HightLightTypes.LeagueFound);

                                    if (!mw.FoundUsers[item.Key].Contains(c.LowerName))
                                    {
                                        mw.FoundUsers[item.Key].Add(c.LowerName);

                                        if (Properties.Settings.Default.TrayFlashing && !mw.IsActive)
                                            mw.FlashWindow();
                                        if (Properties.Settings.Default.TrayNotifications)
                                            mw.NotifyIcon.ShowBalloonTip(null, c.Name + ": " + Message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                                        if (Properties.Settings.Default.LeagueFoundBeepEnabled)
                                            Sounds.PlaySound("LeagueFoundBeep");
                                    }
                                    break;
                                }
                            }

                        }
                        else if (notif && notificationSearch)
                        {
                            foreach (NotificatorClass nc in mw.Notifications)
                            {
                                if (nc.InMessages && nc.TryMatch(words[i]))
                                {
                                    msg.AddHighlightWord(i, HightLightTypes.NotificatorFound);
                                    mw.NotificatorFound(c.Name + ": " + Message, ch);
                                    break;
                                }
                            }
                        }
                    }
                }

                ch.AddMessage(msg);
            }
            // Beep user that new private message arrived
            else
            {
                // If user was removed from conversation and then added to it again but the channel tab remaint open
                if (ch.Disabled)
                    ch.Disabled = false;

                // This way away message will be added to the channel later than the arrived message
                ch.AddMessage(c, Message, Setting);

                if (!c.IsBanned)
                {
                    Channel selectedCH = null;
                    if (mw.Channels.SelectedItem != null)
                        selectedCH = (Channel)((TabItem)mw.Channels.SelectedItem).DataContext;

                    // Private message arrived notification
                    if (!mw.IsActive || ch != selectedCH)
                    {
                        ch.NewMessages = true;

                        if (Properties.Settings.Default.TrayFlashing && !mw.IsActive)
                            mw.FlashWindow();
                        if (Properties.Settings.Default.TrayNotifications)
                            mw.NotifyIcon.ShowBalloonTip(null, c.Name + ": " + Message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                        if (ch.BeepSoundPlay && Properties.Settings.Default.PMBeepEnabled)
                        {
                            Sounds.PlaySound("PMBeep");
                            ch.BeepSoundPlay = false;
                        }

                        // Send away message if needed
                        if (mw.AwayText != string.Empty && ch.SendAway && ch.Messages.Count > 0)
                        {
                            mw.SendMessageToChannel(mw.AwayText, ch);
                            ch.SendAway = false;
                            ch.SendBack = true;
                        }
                    }
                }
            }
        }
    }

    public class JoinedUITask : UITask
    {
        public string ChannelHash { get; private set; }
        public string ClientName { get; private set; }
        public string Clan { get; private set; }

        public JoinedUITask(IRCCommunicator sender, string channelHash, string clientName, string clan)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.ClientName = clientName;
            this.Clan = clan;
        }

        public override void DoTask(MainWindow mw)
        {
            Channel ch;
            if (!Sender.ChannelList.TryGetValue(ChannelHash, out ch))
                return;

            string lowerName = ClientName.ToLower();

            if (lowerName != Sender.User.LowerName)
            {
                if (ch.Joined)
                {
                    Client c = null;
                    if (!Sender.Clients.TryGetValue(lowerName, out c))// Register the new client
                        c = new Client(ClientName, Sender, Clan);

                    if (c.OnlineStatus != Client.Status.Online)
                    {
                        ch.Server.GetInfoAboutClient(ClientName);
                        // Reset client info
                        c.TusActive = false;
                        c.GreatSnooper = false;
                        c.OnlineStatus = Client.Status.Online;
                        c.AddToChannel.Add(ch); // Client will be added to the channel if information is arrived to keep the client list sorted properly

                        foreach (Channel channel in c.PMChannels)
                            channel.AddMessage(c, "is online.", MessageSettings.JoinMessage);
                    }
                    else
                        ch.Clients.Add(c);

                    ch.AddMessage(c, "joined the channel.", MessageSettings.JoinMessage);

                    bool notif = true;
                    if (mw.Notifications.Count > 0)
                    {
                        foreach (NotificatorClass nc in mw.Notifications)
                        {
                            if (nc.InJoinMessages && nc.TryMatch(c.LowerName))
                            {
                                mw.NotificatorFound(c.Name + " joined " + ch.Name + "!", ch);
                                notif = false;
                                break;
                            }
                        }
                    }

                    if (notif && c.Group.ID != UserGroups.SystemGroupID)
                    {
                        if (Properties.Settings.Default.TrayNotifications)
                            mw.NotifyIcon.ShowBalloonTip(null, c.Name + " is online.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                        if (c.Group.SoundEnabled)
                            c.Group.PlaySound();
                    }
                }
            }
            else if (!ch.Joined) // We joined a channel
            {
                ch.Join();
                ch.Server.GetChannelClients(ch.Name); // get the users in the channel

                ch.AddMessage(Sender.User, "joined the channel.", MessageSettings.JoinMessage);

                if (mw.Channels.SelectedItem != null)
                {
                    Channel selectedCH = (Channel)((TabItem)mw.Channels.SelectedItem).DataContext;
                    if (ch == selectedCH)
                    {
                        if (ch.CanHost)
                            mw.GameListForce = true;
                        mw.TusForce = true;
                        ch.ChannelTabItem.UpdateLayout();
                        ch.TheTextBox.Focus();
                    }
                }
            }
        }
    }

    public class PartedUITask : UITask
    {
        public string ChannelHash { get; private set; }
        public string ClientNameL { get; private set; }

        public PartedUITask(IRCCommunicator sender, string channelHash, string clientNameL)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.ClientNameL = clientNameL;
        }

        public override void DoTask(MainWindow mw)
        {
            Channel ch;
            if (!Sender.ChannelList.TryGetValue(ChannelHash, out ch) || !ch.Joined)
                return;

            // This can reagate for force PART (if that exists :D) - this was the old way to PART a channel (was waiting for a PART message from the server as an answer for the PART command sent by the client)
            if (ClientNameL == Sender.User.LowerName)
            {
                ch.Part();
            }
            else
            {
                Client c;
                if (Sender.Clients.TryGetValue(ClientNameL, out c))
                {
                    if (ch.Joined)
                    {
                        ch.AddMessage(c, "has left the channel.", MessageSettings.PartMessage);
                        ch.Clients.Remove(c);
                    }
                    else // remove it for any chance
                        c.Channels.Remove(ch);

                    if (c.Channels.Count == 0)
                    {
                        if (c.PMChannels.Count > 0)
                            c.OnlineStatus = Client.Status.Unknown;
                        else
                            Sender.Clients.Remove(ClientNameL);
                    }
                }
            }
        }
    }

    public class ChannelListUITask : UITask
    {
        public SortedDictionary<string, string> ChannelList { get; private set; }

        public ChannelListUITask(IRCCommunicator sender, SortedDictionary<string, string> channelList)
        {
            this.Sender = sender;
            this.ChannelList = channelList;
        }

        public override void DoTask(MainWindow mw)
        {
            foreach (var item in ChannelList)
            {
                Channel ch = new Channel(mw, Sender, item.Key, item.Value);

                if (GlobalManager.AutoJoinList.ContainsKey(ch.HashName))
                {
                    ch.Loading(true);
                    ch.Server.JoinChannel(ch.Name);
                }
            }

            Channel worms = new Channel(mw, mw.Servers[1], "#worms", "Place for hardcore wormers");
            if (GlobalManager.AutoJoinList.ContainsKey(worms.HashName))
            {
                worms.Loading(true);
                mw.GameSurgeIsConnected = true;
                worms.Server.Connect();
            }
        }
    }

    public class ClientUITask : UITask
    {
        public string ChannelHash { get; private set; }
        public string ClientName { get; private set; }
        public string Clan { get; private set; }
        public CountryClass Country { get; private set; }
        public int Rank { get; private set; }
        public bool ClientGreatSnooper { get; private set; }
        public string ClientApp { get; private set; }

        public ClientUITask(IRCCommunicator sender, string channelHash, string clientName, CountryClass country, string clan, int rank, bool clientGreatSnooper, string clientApp)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.ClientName = clientName;
            this.Country = country;
            this.Clan = clan;
            this.Rank = rank;
            this.ClientGreatSnooper = clientGreatSnooper;
            this.ClientApp = clientApp;
        }

        public override void DoTask(MainWindow mw)
        {
            Channel ch;
            bool channelBad = !Sender.ChannelList.TryGetValue(ChannelHash, out ch) || !ch.Joined; // GameSurge may send info about client with channel name: *.. so we try to process all these messages

            Client c = null;
            string lowerName = ClientName.ToLower();

            if (!Sender.Clients.TryGetValue(lowerName, out c))
            {
                if (!channelBad)
                    c = new Client(ClientName, Sender, Clan);
                else // we don't have any common channel with this client
                    return;
            }

            c.OnlineStatus = Client.Status.Online;
            if (!c.TusActive)
            {
                c.Country = Country;
                c.Rank = RanksClass.GetRankByInt(Rank);
            }
            c.GreatSnooper = ClientGreatSnooper;
            c.ClientApp = ClientApp;

            // This is needed, because when we join a channel we get information about the channel users using the WHO command
            if (!channelBad && !c.Channels.Contains(ch))
                ch.Clients.Add(c);

            if (c.AddToChannel.Count > 0)
            {
                foreach (Channel channel in c.AddToChannel)
                {
                    if (!c.Channels.Contains(ch))
                        channel.Clients.Add(c);
                }
                c.AddToChannel.Clear();
            }
        }
    }

    public class OfflineUITask : UITask
    {
        public string ClientName { get; private set; }

        public OfflineUITask(IRCCommunicator sender, string clientName)
        {
            this.Sender = sender;
            this.ClientName = clientName;
        }

        public override void DoTask(MainWindow mw)
        {
            Client c;
            // Send a message to the private message channel that the user is offline
            if (Sender.Clients.TryGetValue(ClientName.ToLower(), out c))
            {
                c.OnlineStatus = Client.Status.Offline;
                foreach (Channel ch in c.PMChannels)
                    ch.AddMessage(GlobalManager.SystemClient, ClientName + " is currently offline.", MessageSettings.OfflineMessage);
            }
        }
    }

    public class NickNameInUseTask : UITask
    {
        public NickNameInUseTask(IRCCommunicator sender)
        {
            this.Sender = sender;
        }

        public override void DoTask(MainWindow mw)
        {
            Channel ch = mw.Servers[1].ChannelList["#worms"];
            if (ch.Joined)
                ch.AddMessage(GlobalManager.SystemClient, "The selected nickname is already in use!", MessageSettings.OfflineMessage);
        }
    }

    public class NickUITask : UITask
    {
        public string OldClientName { get; private set; }
        public string NewClientName { get; private set; }

        public NickUITask(IRCCommunicator sender, string oldClientName, string newClientName)
        {
            this.Sender = sender;
            this.OldClientName = oldClientName;
            this.NewClientName = newClientName;
        }

        public override void DoTask(MainWindow mw)
        {
            Client c;
            string oldLowerName = OldClientName.ToLower();
            if (Sender.Clients.TryGetValue(oldLowerName, out c))
            {
                if (c == Sender.User)
                    Sender.User.Name = NewClientName;

                // To keep SortedDictionary sorted, first client will be removed..
                var temp1 = new List<Channel>(c.Channels);
                foreach (Channel ch in temp1)
                {
                    if (ch.Joined)
                        ch.Clients.Remove(c);
                }
                var temp2 = new List<Channel>(c.PMChannels);
                foreach (Channel ch in temp2)
                    ch.RemoveClientFromConversation(c, false, false);

                c.Name = NewClientName;
                Sender.Clients.Remove(oldLowerName);
                Sender.Clients.Add(c.LowerName, c);

                // then later it will be readded with new Name
                foreach (Channel ch in temp1)
                {
                    if (ch.Joined)
                    {
                        ch.Clients.Add(c);
                        ch.AddMessage(GlobalManager.SystemClient, OldClientName + " is now known as " + NewClientName + ".", MessageSettings.OfflineMessage);
                    }
                }
                foreach (Channel ch in temp2)
                {
                    ch.AddClientToConversation(c, false, false);
                    ch.AddMessage(GlobalManager.SystemClient, OldClientName + " is now known as " + NewClientName + ".", MessageSettings.OfflineMessage);
                }
            }
        }
    }

    public class ClientAddOrRemoveTask : UITask
    {
        public enum TaskType { Add, Remove };
        public string ChannelHash { get; private set; }
        public string SenderName { get; private set; }
        public string ClientName { get; private set; }
        public TaskType Type { get; private set; }

        public ClientAddOrRemoveTask(IRCCommunicator sender, string channelHash, string senderName, string clientName, TaskType type)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.SenderName = senderName;
            this.ClientName = clientName;
            this.Type = type;
        }

        public override void DoTask(MainWindow mw)
        {
            Channel ch = null;
            if (!Sender.ChannelList.TryGetValue(ChannelHash, out ch))
                return;

            Client c = null;
            if (!Sender.Clients.TryGetValue(ClientName.ToLower(), out c))
                return;

            Client c2 = null;
            if (!Sender.Clients.TryGetValue(SenderName.ToLower(), out c2) || !ch.IsInConversation(c2))
                return;

            if (Type == ClientAddOrRemoveTask.TaskType.Add)
            {
                ch.AddClientToConversation(c, false);
                ch.AddMessage(GlobalManager.SystemClient, SenderName + " has added " + ClientName + " to the conversation.", MessageSettings.OfflineMessage);
            }
            else
            {
                if (ClientName.ToLower() == Sender.User.LowerName)
                {
                    ch.AddMessage(GlobalManager.SystemClient, "You have been removed from this conversation.", MessageSettings.OfflineMessage);
                    ch.Disabled = true;
                }
                else
                {
                    ch.RemoveClientFromConversation(c, false);
                    ch.AddMessage(GlobalManager.SystemClient, SenderName + " has removed " + ClientName + " from the conversation.", MessageSettings.OfflineMessage);
                }
            }
        }
    }

    public class ClientLeaveConvTask : UITask
    {
        public string ChannelHash { get; private set; }
        public string ClientName { get; private set; }

        public ClientLeaveConvTask(IRCCommunicator sender, string channelHash, string clientName)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.ClientName = clientName;
        }

        public override void DoTask(MainWindow mw)
        {
            Channel ch = null;
            if (!Sender.ChannelList.TryGetValue(ChannelHash, out ch))
                return;

            Client c = null;
            if (!Sender.Clients.TryGetValue(ClientName.ToLower(), out c))
                return;

            ch.RemoveClientFromConversation(c, false);
            ch.AddMessage(GlobalManager.SystemClient, ClientName + " has left the conversation.", MessageSettings.OfflineMessage);
        }
    }
}
