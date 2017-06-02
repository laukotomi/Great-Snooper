
namespace GreatSnooper.UserCommands
{
    public class BatmanCommand : UserCommand
    {
        public BatmanCommand()
            : base("batman")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            sender.MainViewModel.BatLogo = !sender.MainViewModel.BatLogo;
        }
    }
}
