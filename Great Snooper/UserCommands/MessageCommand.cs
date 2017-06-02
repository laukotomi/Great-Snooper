using GreatSnooper.ViewModel;

namespace GreatSnooper.UserCommands
{
    public class MessageCommand : UserCommand
    {
        public MessageCommand()
            : base("pm", "msg", "chat")
        {

        }

        public override void Run(AbstractChannelViewModel sender, string command, string text)
        {
            if (text.Length > 0)
            {
                string username = text;
                string msg = string.Empty;

                int spacePos = text.IndexOf(' ');
                if (spacePos != -1)
                {
                    username = text.Substring(0, spacePos).Trim();
                    msg = text.Substring(spacePos + 1).Trim();
                }

                AbstractChannelViewModel chvm;
                if (sender.Server.Channels.TryGetValue(username, out chvm) == false)
                {
                    chvm = new PMChannelViewModel(sender.MainViewModel, sender.Server, username);
                }
                sender.MainViewModel.SelectChannel(chvm);

                if (msg.Length > 0)
                {
                    chvm.SendMessage(msg);
                }
            }
        }
    }
}
