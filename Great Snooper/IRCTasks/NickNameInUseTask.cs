namespace GreatSnooper.IRCTasks
{
    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;
    using GreatSnooper.ViewModel;

    public class NickNameInUseTask : IRCTask
    {
        public NickNameInUseTask(AbstractCommunicator sender)
        {
            this.Sender = sender;
        }

        public override void DoTask(MainViewModel mvm)
        {
            if (mvm.SelectedChannel.Server is GameSurgeCommunicator && mvm.SelectedChannel.Joined)
            {
                mvm.SelectedChannel.AddMessage(GlobalManager.SystemUser, Localizations.GSLocalization.Instance.GSNicknameInUse, MessageSettings.SystemMessage);
            }
        }
    }
}