namespace GreatSnooper.IRCTasks
{

    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    class NamesTask : IRCTask
    {
        private string channelName;
        private string[] names;

        public NamesTask(IRCCommunicator server, string channelName, string[] names)
            : base(server)
        {
            this.channelName = channelName;
            this.names = names;
        }

        public override void DoTask(ViewModel.MainViewModel mw)
        {
            AbstractChannelViewModel temp;
            if (this._server.Channels.TryGetValue(this.channelName, out temp) && temp is ChannelViewModel)
            {
                var chvm = (ChannelViewModel)temp;
                foreach (string name in this.names)
                {
                    string userName = (name.StartsWith("@") || name.StartsWith("+")) ? name.Substring(1) : name;

                    User user = UserHelper.GetUser(_server, userName);

                    if (user.OnlineStatus != User.Status.Online)
                    {
                        user.OnlineStatus = User.Status.Online;
                        chvm.AddUser(user);
                    }
                    else if (user.ChannelCollection.Channels.Contains(chvm) == false)
                    {
                        chvm.AddUser(user);
                    }
                }
            }
        }
    }
}