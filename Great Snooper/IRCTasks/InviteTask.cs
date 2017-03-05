namespace GreatSnooper.IRCTasks
{
    using GreatSnooper.Classes;
    using GreatSnooper.ViewModel;

    class InviteTask : IRCTask
    {
        public InviteTask(AbstractCommunicator sender, string channelName)
        {
            this.Sender = sender;
            this.ChannelName = channelName;
        }

        public string ChannelName
        {
            get;
            private set;
        }

        public override void DoTask(MainViewModel mvm)
        {
            if (this.Sender.HandleJoinRequest)
            {
                AbstractChannelViewModel chvm;
                if (this.Sender.Channels.TryGetValue(this.ChannelName, out chvm) == false)
                {
                    new ChannelViewModel(mvm, this.Sender, this.ChannelName, string.Empty);
                }
            }
        }
    }
}