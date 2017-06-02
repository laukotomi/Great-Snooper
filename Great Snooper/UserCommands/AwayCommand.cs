
namespace GreatSnooper.UserCommands
{
    public class AwayCommand : UserCommand
    {
        public AwayCommand()
            : base("away")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            sender.MainViewModel.SetAway(text);
        }
    }
}
