using System.Collections.Generic;
using System.Linq;

namespace GreatSnooper.UserCommands
{
    public class GSCommand : UserCommand
    {
        public GSCommand()
            : base("gs")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            List<string> gsUsers = sender.Server.Users
                .Where(x => x.Value.UsingGreatSnooper)
                .Select(x => x.Value.Name)
                .ToList();

            sender.MainViewModel.DialogService.ShowDialog(Localizations.GSLocalization.Instance.GSCheckTitle, string.Format(Localizations.GSLocalization.Instance.GSCheckText, gsUsers.Count, string.Join(", ", gsUsers)));
        }
    }
}
