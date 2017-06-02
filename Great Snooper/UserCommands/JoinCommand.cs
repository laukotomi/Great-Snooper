using System;
using GreatSnooper.ViewModel;

namespace GreatSnooper.UserCommands
{
    public class JoinCommand : UserCommand
    {
        public JoinCommand()
            : base("join")
        {

        }

        public override void Run(AbstractChannelViewModel sender, string command, string text)
        {
            if (sender.Server.HandleJoinRequest && text.Length > 0 && (text.StartsWith("#") || text.StartsWith("&")))
            {
                string[] parts = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length <= 2)
                {
                    AbstractChannelViewModel chvm;
                    if (sender.Server.Channels.TryGetValue(parts[0], out chvm) == false)
                    {
                        if (parts.Length == 1)
                        {
                            chvm = new ChannelViewModel(sender.MainViewModel, sender.Server, parts[0], string.Empty);
                        }
                        else
                        {
                            chvm = new ChannelViewModel(sender.MainViewModel, sender.Server, parts[0], string.Empty, parts[1]);
                        }
                    }
                    sender.MainViewModel.SelectChannel(chvm);
                }
            }
        }
    }
}
