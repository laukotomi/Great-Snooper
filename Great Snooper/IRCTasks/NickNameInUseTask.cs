using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.ViewModel;

namespace GreatSnooper.IRCTasks
{
    public class NickNameInUseTask : IRCTask
    {
        public NickNameInUseTask(AbstractCommunicator sender)
        {
            this.Sender = sender;
        }

        public override void DoTask(MainViewModel mvm)
        {
            AbstractChannelViewModel chvm;
            if (this.Sender.Channels.TryGetValue("#worms", out chvm) && chvm.Joined)
                chvm.AddMessage(GlobalManager.SystemUser, Localizations.GSLocalization.Instance.GSNicknameInUse, MessageSettings.SystemMessage);
        }
    }
}
