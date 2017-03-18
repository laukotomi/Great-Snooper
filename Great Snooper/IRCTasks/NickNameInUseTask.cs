namespace GreatSnooper.IRCTasks
{
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.ViewModel;

    public class NickNameInUseTask : IRCTask
    {
        public NickNameInUseTask(IRCCommunicator server)
            : base(server)
        {
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