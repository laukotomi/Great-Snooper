namespace GreatSnooper.IRCTasks
{
    using GreatSnooper.Classes;
    using GreatSnooper.ViewModel;

    public abstract class IRCTask
    {
        public AbstractCommunicator Sender
        {
            get;
            protected set;
        }

        public abstract void DoTask(MainViewModel mw);
    }
}