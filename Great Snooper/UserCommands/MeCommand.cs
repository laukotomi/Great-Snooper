
namespace GreatSnooper.UserCommands
{
    public class MeCommand : UserCommand
    {
        public MeCommand()
            : base("me")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            if (text.Length > 0)
            {
                sender.SendActionMessage(text);
            }
        }
    }
}
