using GreatSnooper.ViewModel;

namespace GreatSnooper.UserCommands
{
    public class PartCommand : UserCommand
    {
        public PartCommand()
            : base("part")
        {

        }

        public override void Run(AbstractChannelViewModel sender, string command, string text)
        {
            var chvm = sender as ChannelViewModel;
            if (chvm != null)
            {
                chvm.LeaveChannelCommand.Execute(null);
            }
            else
            {
                sender.MainViewModel.CloseChannelCommand.Execute(this);
            }
        }
    }
}
