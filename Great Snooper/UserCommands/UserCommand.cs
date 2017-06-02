
using GreatSnooper.ViewModel;
namespace GreatSnooper.UserCommands
{
    public abstract class UserCommand
    {
        public string[] Commands { get; private set; }
        public abstract void Run(AbstractChannelViewModel sender, string command, string text);

        public UserCommand(params string[] commands)
        {
            this.Commands = commands;
        }
    }
}
