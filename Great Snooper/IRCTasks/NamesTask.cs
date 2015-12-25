using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GreatSnooper.IRCTasks
{
    class NamesTask : IRCTask
    {
        private string[] names;
        private string channelName;

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

                    User u;
                    if (!Sender.Users.TryGetValue(userName, out u))// Register the new client
                        u = Users.CreateUser(Sender, userName);

                    if (u.OnlineStatus != User.Status.Online)
                    {
                        u.OnlineStatus = User.Status.Online;
                        chvm.AddUser(u);
                    }
                    else if (u.Channels.Contains(chvm) == false)
                        chvm.AddUser(u);
                }
            }
        }
    }
}
