
namespace GreatSnooper.UserCommands
{
    public class NewsCommand : UserCommand
    {
        public NewsCommand()
            : base("news")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            sender.MainViewModel.OpenNewsCommand.Execute(null);
        }
    }
}
