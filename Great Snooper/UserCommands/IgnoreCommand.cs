
namespace GreatSnooper.UserCommands
{
    public class IgnoreCommand : UserCommand
    {
        public IgnoreCommand()
            : base("ignore")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            sender.MainViewModel.AddOrRemoveBanCommand.Execute(text);
        }
    }
}
