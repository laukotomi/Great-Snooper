
namespace GreatSnooper.UserCommands
{
    public class NoticeCommand : UserCommand
    {
        public NoticeCommand()
            : base("notice")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            if (text.Length > 0)
            {
                sender.SendNotice(text);
            }
        }
    }
}
