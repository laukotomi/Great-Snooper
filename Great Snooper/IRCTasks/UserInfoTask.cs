namespace GreatSnooper.IRCTasks
{
    using System.Linq;
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    public class UserInfoTask : IRCTask
    {
        public UserInfoTask(IRCCommunicator server, string channelHash, string clientName, Country country, string clan, int rank, string clientApp)
            : base(server)
        {
            this.ChannelHash = channelHash;
            this.ClientName = clientName;
            this.Country = country;
            this.Clan = clan;
            this.Rank = rank;
            this.ClientApp = clientApp;
        }

        public string ChannelHash
        {
            get;
            private set;
        }

        public string Clan
        {
            get;
            private set;
        }

        public string ClientApp
        {
            get;
            private set;
        }

        public string ClientName
        {
            get;
            private set;
        }

        public Country Country
        {
            get;
            private set;
        }

        public int Rank
        {
            get;
            private set;
        }

        public override void DoTask(MainViewModel mvm)
        {
            AbstractChannelViewModel chvm;
            bool channelOK = _server.Channels.TryGetValue(this.ChannelHash, out chvm) && chvm.Joined; // GameSurge may send info about client with channel name: *.. so we try to process all these messages

            User u = null;
            if (!_server.Users.TryGetValue(this.ClientName, out u))
            {
                if (channelOK)
                {
                    u = UserHelper.CreateUser(_server, this.ClientName, this.Clan);
                }
                else // we don't have any common channel with this client
                {
                    return;
                }
            }

            u.OnlineStatus = User.Status.Online;
            u.Country = Country;
            u.Rank = Ranks.GetRankByInt(this.Rank);
            u.ClientName = this.ClientApp;

            if (u.AddToChannel.Count > 0)
            {
                foreach (var channel in u.AddToChannel)
                {
                    if (channel.Joined && !u.ChannelCollection.Channels.Contains(chvm))
                    {
                        channel.AddUser(u);
                    }
                }
                u.AddToChannel.Clear();
            }

            // This is needed, because when we join a channel we get information about the channel users using the WHO command
            if (channelOK && chvm is ChannelViewModel && !u.ChannelCollection.Channels.Contains(chvm))
            {
                ((ChannelViewModel)chvm).AddUser(u);
            }
        }
    }
}