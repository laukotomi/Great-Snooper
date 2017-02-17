using GreatSnooper.Classes;
using GreatSnooper.ViewModel;

namespace GreatSnooper.IRCTasks
{
    class InviteTask : IRCTask
    {
        public string ChannelName { get; private set; }

        public InviteTask(AbstractCommunicator sender, string channelName)
        {
            this.Sender = sender;
            this.ChannelName = channelName;
        }

        public override void DoTask(MainViewModel mvm)
        {
            if (this.Sender.HandleJoinRequest)
            {
                AbstractChannelViewModel chvm;
                if (this.Sender.Channels.TryGetValue(this.ChannelName, out chvm) == false)
                {
                    new ChannelViewModel(mvm, this.Sender, this.ChannelName, "");
                }
            }
        }
    }
}
