namespace GreatSnooper.IRCTasks
{
    using GreatSnooper.IRC;
    using GreatSnooper.ViewModel;

    class InviteTask : IRCTask
    {
        public InviteTask(IRCCommunicator server, string channelName)
            : base(server)
        {
            this.ChannelName = channelName;
        }

        public string ChannelName
        {
            get;
            private set;
        }

        public override void DoTask(MainViewModel mvm)
        {
            if (this._server.HandleJoinRequest)
            {
                AbstractChannelViewModel chvm;
                if (this._server.Channels.TryGetValue(this.ChannelName, out chvm) == false)
                {
                    new ChannelViewModel(mvm, this._server, this.ChannelName, string.Empty);
                }
            }
        }
    }
}