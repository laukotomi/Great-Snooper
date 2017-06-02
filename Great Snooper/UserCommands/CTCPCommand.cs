
namespace GreatSnooper.UserCommands
{
    public class CTCPCommand : UserCommand
    {
        public CTCPCommand()
            : base("ctcp")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            if (text.Length > 0)
            {
                string ctcpCommand = text;
                string ctcpText = string.Empty;

                int spacePos = text.IndexOf(' ');
                if (spacePos != -1)
                {
                    ctcpCommand = text.Substring(0, spacePos).Trim();
                    ctcpText = text.Substring(spacePos + 1).Trim();
                }

                sender.SendCTCPMessage(ctcpCommand, ctcpText);
            }
        }
    }
}
