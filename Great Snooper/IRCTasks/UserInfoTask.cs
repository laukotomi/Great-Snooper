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

            User u = UserHelper.GetUser(_server, ClientName, Clan, channelOK);
            u.SetUserInfo(Country, Ranks.GetRankByInt(Rank), ClientApp);
            u.OnlineStatus = User.Status.Online;
            u.CanShow = true;

            // This is needed, because when we join a channel we get information about the channel users using the WHO command
            if (channelOK && chvm is ChannelViewModel && !u.ChannelCollection.Channels.Contains(chvm))
            {
                ((ChannelViewModel)chvm).AddUser(u);
            }
        }
    }
}