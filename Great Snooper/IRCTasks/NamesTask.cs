namespace GreatSnooper.IRCTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    class NamesTask : IRCTask
    {
        private string channelName;
        private string[] names;

        public NamesTask(AbstractCommunicator sender, string channelName, string[] names)
        {
            this.Sender = sender;
            this.channelName = channelName;
            this.names = names;
        }

        public override void DoTask(ViewModel.MainViewModel mw)
        {
            AbstractChannelViewModel temp;
            if (this.Sender.Channels.TryGetValue(this.channelName, out temp) && temp is ChannelViewModel)
            {
                var chvm = (ChannelViewModel)temp;
                foreach (string name in this.names)
                {
                    string userName = (name.StartsWith("@") || name.StartsWith("+")) ? name.Substring(1) : name;

                    User user = UserHelper.GetUser(Sender, userName);

                    if (user.OnlineStatus != User.Status.Online)
                    {
                        user.OnlineStatus = User.Status.Online;
                        chvm.AddUser(user);
                    }
                    else if (user.Channels.Contains(chvm) == false)
                    {
                        chvm.AddUser(user);
                    }
                }
            }
        }
    }
}