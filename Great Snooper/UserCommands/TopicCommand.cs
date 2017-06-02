using GreatSnooper.ViewModel;

namespace GreatSnooper.UserCommands
{
    public class TopicCommand : UserCommand
    {
        public TopicCommand()
            : base("topic")
        {

        }

        public override void Run(AbstractChannelViewModel sender, string command, string text)
        {
            if (sender is ChannelViewModel)
            {
                sender.Server.Send(this, "TOPIC " + sender.Name);
            }
        }
    }
}
