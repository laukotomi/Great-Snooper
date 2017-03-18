namespace GreatSnooper.IRCTasks
{
    using GreatSnooper.IRC;
    using GreatSnooper.ViewModel;

    public abstract class IRCTask
    {
        protected IRCCommunicator _server;

        public IRCTask(IRCCommunicator server)
        {
            _server = server;
        }

        public abstract void DoTask(MainViewModel mw);
    }
}