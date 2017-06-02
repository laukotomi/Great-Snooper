
namespace GreatSnooper.UserCommands
{
    public class WormsCommand : UserCommand
    {
        public WormsCommand()
            : base("worms")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            Properties.Settings.Default.ShowWormsChannel = !Properties.Settings.Default.ShowWormsChannel;
            Properties.Settings.Default.Save();
        }
    }
}
