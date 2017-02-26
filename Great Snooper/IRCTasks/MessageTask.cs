namespace GreatSnooper.IRCTasks
{
    using System.Text.RegularExpressions;

    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    public class MessageTask : IRCTask
    {
        private static Regex nickRegex = new Regex(@"[a-z0-9`\-]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public MessageTask(AbstractCommunicator sender, string clientName, string channelHash, string message, MessageSetting setting)
        {
            this.Sender = sender;
            this.ClientName = clientName;
            this.ChannelHash = channelHash;
            this.Message = message;
            this.Setting = setting;
        }

        public string ChannelHash
        {
            get;
            private set;
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

        public MessageSetting Setting
        {
            get;
            private set;
        }

        public User User
        {
            get;
            private set;
        }

        public override void DoTask(MainViewModel mvm)
        {
            AbstractChannelViewModel chvm = null;

            // If the message arrived in a closed channel
            if (Sender.Channels.TryGetValue(this.ChannelHash, out chvm) && !chvm.Joined)
            {
                return;
            }

            // If the user doesn't exists we create one
            this.User = UserHelper.GetUser(Sender, this.ClientName);

            if (chvm == null) // New private message arrived for us
            {
                chvm = new PMChannelViewModel(mvm, Sender, this.ChannelHash);
            }

            chvm.ProcessMessage(this);
        }
    }
}