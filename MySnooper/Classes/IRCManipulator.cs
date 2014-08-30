using System;
using System.Collections.Generic;

namespace MySnooper
{
    public delegate void HightlightDelegate(int channelIndex);

    // IRC manipulator class
    public class IRCManipulator
    {
        // Private server variables
        private string ServerAddress;

        // Communicators
        private IRCCommunicator WormNetC;
        private WormageddonWebComm WormWebC;

        // Buddy and ban lists
        public SortedDictionary<string, string> BanList { get; private set; }
        public SortedDictionary<string, string> BuddyList { get; private set; }

        // Lists
        public SortedDictionary<string, Channel> ChannelList;
        public SortedDictionary<string, Client> Clients;



        // Constructor
        public IRCManipulator(string ServerAddress, IRCCommunicator WormNetC, WormageddonWebComm WormWebC)
        {
            this.ServerAddress = ServerAddress;
            this.WormNetC = WormNetC;
            this.WormWebC = WormWebC;
            this.BanList = new SortedDictionary<string, string>();
            this.BuddyList = new SortedDictionary<string, string>();
            ChannelList = new SortedDictionary<string, Channel>();
            Clients = new SortedDictionary<string, Client>(StringComparer.OrdinalIgnoreCase);

            string[] list;
            // Unserialize buddy and ban list
            list = Properties.Settings.Default.BuddyList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < list.Length; i++)
                BuddyList.Add(list[i].ToLower(), list[i]);

            list = Properties.Settings.Default.BanList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < list.Length; i++)
                BanList.Add(list[i].ToLower(), list[i]);
        }


        // Add a new client or modify its properties
        public void AddClient(string channelName, string clientName, CountryClass country, string clan, int rank, bool ClientGreatSnooper)
        {
            Channel ch;

            if (!ChannelList.TryGetValue(channelName.ToLower(), out ch) || !ch.Joined)
                return;

            string lowerName = clientName.ToLower();

            Client c = null;
            // If we have a private chat with the user
            foreach (var item in ChannelList)
            {
                if (item.Value.IsPrivMsgChannel && item.Value.LowerName == lowerName)
                {
                    c = item.Value.TheClient;
                    break;
                }
            }


            if (c == null && !Clients.TryGetValue(lowerName, out c)) // Register the new client (this will be after WHO #ChannelName request)
            {
                c = new Client(clientName, country, clan, rank, ClientGreatSnooper);
                Clients.Add(lowerName, c);
            }
            
            c.OnlineStatus = 1;
            c.IsBanned = IsBanned(lowerName);
            c.IsBuddy = IsBuddy(lowerName);
            c.Country = country;
            c.Rank = RanksClass.GetRankByInt(rank);
            c.ClientGreatSnooper = ClientGreatSnooper;
            if (!c.Channels.Contains(ch))
                c.Channels.Add(ch);
            if (!ch.Clients.Contains(c))
                ch.Clients.Add(c);
        }


        // A user joins a channel
        public bool JoinedChannel(string channelName, string clientName, string clan, ref bool buddyjoined)
        {
            Channel ch;
            if (!ChannelList.TryGetValue(channelName.ToLower(), out ch))
                return false;

            bool ToReturn = false; // We will return true if we joined the channel

            string lowerName = clientName.ToLower();
            MessageTypes messageType = MessageTypes.Join;
            if (lowerName != GlobalManager.User.LowerName)
            {
                if (ch.Joined)
                {
                    Client c;
                    if (!Clients.TryGetValue(lowerName, out c))
                    {
                        c = new Client(clientName, null, clan, 0, false);
                        c.OnlineStatus = 1;
                        c.IsBanned = IsBanned(lowerName);
                        c.IsBuddy = IsBuddy(lowerName);
                        c.Channels.Add(ch);
                        ch.Clients.Add(c);
                        Clients.Add(lowerName, c);

                        if (c.IsBuddy)
                        {
                            messageType = MessageTypes.BuddyJoined;
                            buddyjoined = true;
                        }

                        WormNetC.GetInfoAboutClient(clientName);
                    }
                    else
                    {
                        if (!c.Channels.Contains(ch))
                            c.Channels.Add(ch);
                        if (!ch.Clients.Contains(c))
                            ch.Clients.Add(c);

                        if (c.Country == null)
                            WormNetC.GetInfoAboutClient(clientName);
                    }

                    ch.AddMessage(c, "joined the channel", messageType);

                    // If we have a private chat with the user
                    foreach (var item in ChannelList)
                    {
                        if (item.Value.IsPrivMsgChannel && item.Value.LowerName == lowerName)
                        {
                            item.Value.TheClient.OnlineStatus = 1;
                            break;
                        }
                    }
                }
            }
            else if (!ch.Joined) // We joined a channel
            {
                if (ch.Scheme == string.Empty)
                {
                    WormWebC.SetChannelScheme(ch);
                }
                ch.Join();
                WormNetC.GetChannelClients(ch.Name); // get the users in the channel

                ToReturn = true;
                ch.AddMessage(GlobalManager.User, "joined the channel", messageType);
            }

            return ToReturn;
        }


        // A user leaves a channel
        public Client PartedChannel(string channelName, string clientName)
        {
            Channel ch;
            if (!ChannelList.TryGetValue(channelName.ToLower(), out ch))
                return null;

            if (ch.Joined)
            {
                string lowerName = clientName.ToLower();
                // This can reagate for force PART (if that exists :D) - this was the old way to PART a channel (was waiting for a PART message from the server as an answer for the PART command sent by the client)
                if (lowerName == GlobalManager.User.LowerName)
                {
                    ch.Part();
                }
                else
                {
                    Client c;
                    if (Clients.TryGetValue(lowerName, out c))
                    {
                        ch.Clients.Remove(c);
                        c.Channels.Remove(ch);
                        if (c.Channels.Count == 0)
                        {
                            Clients.Remove(lowerName);
                            // If we had a private message channel with the user we sign that we don't know if the user is online or not
                            foreach (var item in ChannelList)
                            {
                                if (item.Value.IsPrivMsgChannel && item.Value.LowerName == lowerName)
                                {
                                    item.Value.TheClient.OnlineStatus = 2;
                                    break;
                                }
                            }
                            return c;
                        }
                        ch.AddMessage(c, "left the channel", MessageTypes.Part);
                    }
                }
            }
            return null;
        }


        // A user quits
        public void QuittedChannel(string clientName, string message)
        {
            string lowerName = clientName.ToLower();
            Client c;
            if (Clients.TryGetValue(lowerName, out c))
            {                
                // Send quit message to the channels where the user was active
                for (int i = 0; i < c.Channels.Count; i++)
                {
                    if (c.Channels[i].Joined)
                    {
                        c.Channels[i].AddMessage(c, "quitted (" + message + ")", MessageTypes.Quit);
                        c.Channels[i].Clients.Remove(c);
                    }
                }
                Clients.Remove(lowerName);

                // If we had a private chat with the user
                foreach (var item in ChannelList)
                {
                    if (item.Value.IsPrivMsgChannel && item.Value.LowerName == lowerName)
                    {
                        item.Value.TheClient.OnlineStatus = 0;
                        //item.Value.AddMessage(c, "quitted (" + message + ")", MessageTypes.Quit);
                        break;
                    }
                }
            }
        }

        // The user we sent a private message to is offline
        public void OfflineUserPrivChat(string clientName)
        {
            string lowerName = clientName.ToLower();
            // Send a message to the private message channel that the user is offline
            foreach (var item in ChannelList)
            {
                if (item.Value.IsPrivMsgChannel && item.Value.LowerName == lowerName)
                {
                    item.Value.TheClient.OnlineStatus = 0;
                    item.Value.AddMessage(GlobalManager.SystemClient, "The user is currently offline!", MessageTypes.Offline);
                    break;
                }
            }
        }


        // Buddy list things
        #region Buddy list
        public void AddBuddy(string name)
        {
            string lowerName = name.ToLower();
            BuddyList.Add(lowerName, name);

            Client c;
            if (Clients.TryGetValue(lowerName, out c))
                c.IsBuddy = true;
        }

        public void RemoveBuddy(string name)
        {
            string lowerName = name.ToLower();
            BuddyList.Remove(lowerName);

            Client c;
            if (Clients.TryGetValue(lowerName, out c))
                c.IsBuddy = false;
        }

        public bool IsBuddy(string name)
        {
            return BuddyList.ContainsKey(name);
        }
        #endregion



        // Ban list things
        #region Ban list
        public void AddBan(string name)
        {
            string lowerName = name.ToLower();
            BanList.Add(lowerName, name);

            Client c;
            if (Clients.TryGetValue(lowerName, out c))
                c.IsBanned = true;
        }

        public void RemoveBan(string name)
        {
            string lowerName = name.ToLower();
            BanList.Remove(lowerName);

            Client c;
            if (Clients.TryGetValue(lowerName, out c))
                c.IsBanned = false;
        }

        public bool IsBanned(string name)
        {
            return BanList.ContainsKey(name);
        }
        #endregion
    }
}
