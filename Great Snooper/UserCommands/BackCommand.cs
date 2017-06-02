
namespace GreatSnooper.UserCommands
{
    public class BackCommand : UserCommand
    {
        public BackCommand()
            : base("back")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            sender.MainViewModel.SetBack();
        }
    }
}
