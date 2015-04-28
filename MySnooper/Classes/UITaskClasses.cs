using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
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
                for (int i = 0; i < c.Channels.Count; i++)
                {
                    c.Channels[i].AddMessage(c, msg, MessageSettings.QuitMessage);
                    c.Channels[i].Clients.Remove(c);
                }
                c.Channels.Clear();

                for (int i = 0; i < c.PMChannels.Count; i++)
                    c.PMChannels[i].AddMessage(c, msg, MessageSettings.QuitMessage);

                if (c.PMChannels.Count == 0)
                    Sender.Clients.Remove(ClientNameL);
                // If we had a private chat with the user
                else
                    c.OnlineStatus = 0;
            }
        }
    }

    public class MessageUITask : UITask
    {
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
            {
                c = new Client(ClientName);
                c.IsBanned = mw.IsBanned(fromLow);
                c.IsBuddy = mw.IsBuddy(fromLow);
                c.OnlineStatus = 2;
                Sender.Clients.Add(c.LowerName, c);
            }

            if (ch == null) // New private message arrived for us
                ch = new Channel(mw, Sender, ChannelHash, "Chat with " + c.Name, c);

            // Search for league or hightlight our name
            if (!ch.IsPrivMsgChannel)
            {
                string highlightWord = string.Empty;
                bool LookForLeague = Setting.Type == MessageTypes.Channel && mw.SearchHere == ch;
                bool notificationSearch = false;
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
                            mw.NotificatorFound(c.Name + ": " + Message, ch); break;
                        }
                    }
                }

                if (Setting.Type == MessageTypes.Channel || LookForLeague)
                {
                    string[] words = Message.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < words.Length; i++)
                    {
                        if (words[i] == Sender.User.Name)
                        {
                            mw.Highlight(ch);
                        }
                        else if (LookForLeague)
                        {
                            string lower = words[i].ToLower();
                            // foundUsers.ContainsKey(lower) == league name we are looking for
                            // foundUsers[lower].Contains(c.LowerName) == the user we found for league lower
                            foreach (var item in mw.FoundUsers)
                            {
                                if (lower.Contains(item.Key) && !mw.FoundUsers[item.Key].Contains(c.LowerName))
                                {
                                    mw.FoundUsers[item.Key].Add(c.LowerName);
                                    highlightWord = words[i];
                                    if (Properties.Settings.Default.TrayFlashing && !mw.IsWindowFocused)
                                        mw.FlashWindow();
                                    if (Properties.Settings.Default.TrayNotifications)
                                        mw.NotifyIcon.ShowBalloonTip(null, c.Name + ": " + Message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

                                    if (Properties.Settings.Default.LeagueFoundBeepEnabled)
                                        mw.PlaySound("LeagueFoundBeep");
                                    break;
                                }
                            }

                        }
                        else if (notif && notificationSearch)
                        {
                            foreach (NotificatorClass nc in mw.Notifications)
                            {
                                if (nc.InMessages && nc.TryMatch(words[i].ToLower()))
                                {
                                    highlightWord = words[i];
                                    mw.NotificatorFound(c.Name + ": " + Message, ch);
                                    break;
                                }
                            }
                        }
                    }
                }
                ch.AddMessage(c, Message, Setting, highlightWord);
            }
            // Beep user that new private message arrived
            else
            {
                // If user was removed from conversation and then added to it again but the channel tab remained open
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
                    if (ch.BeepSoundPlay && (ch != selectedCH || !mw.IsWindowFocused))
                    {
                        ch.NewMessages = true;
                        ch.BeepSoundPlay = false;
                        if (Properties.Settings.Default.TrayFlashing && mw.IsWindowFocused)
                            mw.FlashWindow();
                        if (Properties.Settings.Default.TrayNotifications)
                            mw.NotifyIcon.ShowBalloonTip(null, c.Name + ": " + Message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

                        if (Properties.Settings.Default.PMBeepEnabled)
                            mw.PlaySound("PMBeep");
                    }

                    // Send back away message if needed
                    if (mw.AwayText != string.Empty && ch.SendAway && ch.Messages.Count > 0 && (selectedCH != ch || !mw.IsWindowFocused))
                    {
                        mw.SendMessageToChannel(mw.AwayText, ch);
                        ch.SendAway = false;
                        ch.SendBack = true;
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
                    ch.Clients.Remove(c);
                    c.Channels.Remove(ch);
                    if (c.Channels.Count == 0)
                    {
                        if (c.PMChannels.Count > 0)
                            c.OnlineStatus = 2;
                        else
                            Sender.Clients.Remove(ClientNameL);
                    }
                    ch.AddMessage(c, "has left the channel.", MessageSettings.PartMessage);
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
            bool buddyJoined = false;
            bool userJoined = false;

            if (lowerName != Sender.User.LowerName)
            {
                if (ch.Joined)
                {
                    Client c = null;
                    if (!Sender.Clients.TryGetValue(lowerName, out c))// Register the new client
                    {
                        c = new Client(ClientName, Clan);
                        c.IsBanned = mw.IsBanned(lowerName);
                        c.IsBuddy = mw.IsBuddy(lowerName);
                        Sender.Clients.Add(lowerName, c);
                    }

                    if (c.OnlineStatus != 1)
                    {
                        ch.Server.GetInfoAboutClient(ClientName);
                        // Reset client info
                        c.TusActive = false;
                        c.ClientGreatSnooper = false;
                        c.OnlineStatus = 1;
                        c.AddToChannel.Add(ch); // Client will be added to the channel if information is arrived to keep the client list sorted properly

                        foreach (Channel channel in c.PMChannels)
                            channel.AddMessage(c, "is online.", MessageSettings.JoinMessage);
                    }
                    else
                    {
                        c.Channels.Add(ch);
                        ch.Clients.Add(c);
                    }

                    if (c.IsBuddy)
                    {
                        buddyJoined = true;
                        ch.AddMessage(c, "joined the channel.", MessageSettings.BuddyJoinedMessage);
                        if (Properties.Settings.Default.TrayNotifications)
                            mw.NotifyIcon.ShowBalloonTip(null, c.Name + " is online.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    }
                    else
                        ch.AddMessage(c, "joined the channel.", MessageSettings.JoinMessage);

                    if (mw.Notifications.Count > 0)
                    {
                        foreach (NotificatorClass nc in mw.Notifications)
                        {
                            if (nc.InJoinMessages && nc.TryMatch(c.LowerName))
                            {
                                mw.NotificatorFound(c.Name + " joined " + ch.Name + "!", ch);
                                break;
                            }
                        }
                    }
                }
                else
                    return;
            }
            else if (!ch.Joined) // We joined a channel
            {
                ch.Join(mw.WormWebC);
                ch.Server.GetChannelClients(ch.Name); // get the users in the channel

                userJoined = true;
                ch.AddMessage(Sender.User, "joined the channel.", MessageSettings.JoinMessage);

                if (mw.Channels.SelectedItem != null)
                {
                    Channel selectedCH = (Channel)((TabItem)mw.Channels.SelectedItem).DataContext;
                    if (ch == selectedCH)
                    {
                        if (ch.CanHost)
                            mw.GameListForce = true;
                        mw.TusForce = true;
                    }
                }
            }

            if (buddyJoined && Properties.Settings.Default.BJBeepEnabled)
                mw.PlaySound("BJBeep");

            if (userJoined && mw.Channels.SelectedItem != null)
            {
                Channel selectedCH = (Channel)((TabItem)mw.Channels.SelectedItem).DataContext;
                if (ch == selectedCH)
                {
                    ch.ChannelTabItem.UpdateLayout();
                    ch.TheTextBox.Focus();
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

                if (mw.AutoJoinList.ContainsKey(ch.HashName))
                {
                    ch.Loading(true);
                    ch.Server.JoinChannel(ch.Name);
                }
            }

            Channel worms = new Channel(mw, mw.Servers[1], "#worms", "Place for hardcore wormers");
            if (mw.AutoJoinList.ContainsKey(worms.HashName))
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
                {
                    c = new Client(ClientName, Clan);
                    c.IsBanned = mw.IsBanned(lowerName);
                    c.IsBuddy = mw.IsBuddy(lowerName);
                    Sender.Clients.Add(lowerName, c);
                }
                else // we don't have any common channel with this client
                    return;
            }

            c.OnlineStatus = 1;
            if (!c.TusActive)
            {
                c.Country = Country;
                c.Rank = RanksClass.GetRankByInt(Rank);
            }
            c.ClientGreatSnooper = ClientGreatSnooper;
            c.ClientApp = ClientApp;

            // This is needed, because when we join a channel we get information about the channel users using the WHO command
            if (!channelBad && !c.Channels.Contains(ch))
            {
                c.Channels.Add(ch);
                ch.Clients.Add(c);
            }

            if (c.AddToChannel.Count > 0)
            {
                foreach (Channel channel in c.AddToChannel)
                {
                    if (!c.Channels.Contains(ch))
                    {
                        c.Channels.Add(channel);
                        channel.Clients.Add(c);
                    }
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
                c.OnlineStatus = 0;
                foreach (Channel ch in c.PMChannels)
                {
                    ch.AddMessage(GlobalManager.SystemClient, ClientName + " is currently offline.", MessageSettings.OfflineMessage);
                }
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
                foreach (Channel ch in c.Channels)
                    ch.Clients.Remove(c);
                foreach (Channel ch in c.PMChannels)
                    ch.RemoveClientFromConversation(c, false, false);

                c.Name = NewClientName;
                Sender.Clients.Remove(oldLowerName);
                Sender.Clients.Add(c.LowerName, c);

                // then later it will be readded with new Name
                foreach (Channel ch in c.Channels)
                {
                    ch.Clients.Add(c);
                    ch.AddMessage(GlobalManager.SystemClient, OldClientName + " is now known as " + NewClientName + ".", MessageSettings.OfflineMessage);
                }
                foreach (Channel ch in c.PMChannels)
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
