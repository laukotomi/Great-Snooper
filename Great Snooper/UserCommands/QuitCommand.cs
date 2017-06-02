
namespace GreatSnooper.UserCommands
{
    public class QuitCommand : UserCommand
    {
        public QuitCommand()
            : base("quit", "exit", "close")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            sender.MainViewModel.CloseCommand.Execute(null);
        }
    }
}
