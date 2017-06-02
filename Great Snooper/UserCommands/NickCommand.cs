
namespace GreatSnooper.UserCommands
{
    public class NickCommand : UserCommand
    {
        public NickCommand()
            : base("nick")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            if (text.Length > 0 && sender.Server.HandleNickChange)
            {
                sender.Server.NickChange(this, text);
            }
        }
    }
}
