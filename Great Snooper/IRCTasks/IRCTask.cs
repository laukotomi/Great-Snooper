using GreatSnooper.Classes;
using GreatSnooper.ViewModel;

namespace GreatSnooper.IRCTasks
{
    public abstract class IRCTask
    {
        public AbstractCommunicator Sender { get; protected set; }

        public abstract void DoTask(MainViewModel mw);
    }
}
