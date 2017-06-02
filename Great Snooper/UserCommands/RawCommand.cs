
namespace GreatSnooper.UserCommands
{
    public class RawCommand : UserCommand
    {
        public RawCommand()
            : base("raw", "irc")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            if (text.Length > 0)
            {
                sender.Server.Send(this, text);
            }
        }
    }
}
