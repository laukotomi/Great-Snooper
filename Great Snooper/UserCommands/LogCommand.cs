using GreatSnooper.Helpers;

namespace GreatSnooper.UserCommands
{
    public class LogCommand : UserCommand
    {
        public LogCommand()
            : base("log", "logs")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            System.Diagnostics.Process.Start(GlobalManager.SettingsPath);
        }
    }
}
