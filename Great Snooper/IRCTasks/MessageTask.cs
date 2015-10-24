using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.ViewModel;
using System.Text.RegularExpressions;

namespace GreatSnooper.IRCTasks
{
    public class MessageTask : IRCTask
    {
        private static Regex nickRegex = new Regex(@"[a-z0-9`\-]", RegexOptions.IgnoreCase);

        public string ClientName { get; private set; }
        public string ChannelHash { get; private set; }
        public string Message { get; private set; }
        public MessageSetting Setting { get; private set; }
        public User User { get; private set; }

        public MessageTask(AbstractCommunicator sender, string clientName, string channelHash, string message, MessageSetting setting)
        {
            this.Sender = sender;
            this.ClientName = clientName;
            this.ChannelHash = channelHash;
            this.Message = message;
            this.Setting = setting;
        }

        public override void DoTask(MainViewModel mvm)
        {
            User u = null;
            AbstractChannelViewModel chvm = null;

            // If the message arrived in a closed channel
            if (Sender.Channels.TryGetValue(ChannelHash, out chvm) && !chvm.Joined)
                return;

            // If the user doesn't exists we create one
            if (!Sender.Users.TryGetValue(ClientName, out u))
                u = Users.CreateUser(Sender, ClientName);
            this.User = u;

            if (chvm == null) // New private message arrived for us
                chvm = new PMChannelViewModel(mvm, Sender, ChannelHash);

            chvm.ProcessMessage(this);
        }
    }
}
