using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.ViewModel;
using System.Linq;

namespace GreatSnooper.IRCTasks
{
    public class UserInfoTask : IRCTask
    {
        public string ChannelHash { get; private set; }
        public string ClientName { get; private set; }
        public string Clan { get; private set; }
        public Country Country { get; private set; }
        public int Rank { get; private set; }
        public string ClientApp { get; private set; }

        public UserInfoTask(AbstractCommunicator sender, string channelHash, string clientName, Country country, string clan, int rank, string clientApp)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.ClientName = clientName;
            this.Country = country;
            this.Clan = clan;
            this.Rank = rank;
            this.ClientApp = clientApp;
        }

        public override void DoTask(MainViewModel mvm)
        {
            AbstractChannelViewModel chvm;
            bool channelOK = Sender.Channels.TryGetValue(ChannelHash, out chvm) && chvm.Joined; // GameSurge may send info about client with channel name: *.. so we try to process all these messages

            User u = null;
            if (!Sender.Users.TryGetValue(ClientName, out u))
            {
                if (channelOK)
                    u = Users.CreateUser(Sender, ClientName, Clan);
                else // we don't have any common channel with this client
                    return;
            }

            u.OnlineStatus = User.Status.Online;
            u.Country = Country;
            u.Rank = Ranks.GetRankByInt(Rank);
            u.ClientName = ClientApp;

            if (u.AddToChannel.Count > 0)
            {
                foreach (var channel in u.AddToChannel)
                {
                    if (!u.Channels.Contains(chvm))
                    {
                        channel.AddUser(u);
                        channel.AddMessage(u, Localizations.GSLocalization.Instance.JoinMessage, MessageSettings.JoinMessage);
                    }
                }
                u.AddToChannel.Clear();
            }

            // This is needed, because when we join a channel we get information about the channel users using the WHO command
            if (channelOK && !u.Channels.Contains(chvm))
                ((ChannelViewModel)chvm).AddUser(u);
        }
    }
}
