using GreatSnooper.Helpers;

namespace GreatSnooper.UserCommands
{
    public class DebugCommand : UserCommand
    {
        public DebugCommand()
            : base("debug")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            GlobalManager.DebugMode = !GlobalManager.DebugMode;
        }
    }
}
