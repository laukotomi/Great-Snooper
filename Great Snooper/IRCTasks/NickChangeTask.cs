using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.ViewModel;

namespace GreatSnooper.IRCTasks
{
    public class NickChangeTask : IRCTask
    {
        public string OldClientName { get; private set; }
        public string NewClientName { get; private set; }

        public NickChangeTask(AbstractCommunicator sender, string oldClientName, string newClientName)
        {
            this.Sender = sender;
            this.OldClientName = oldClientName;
            this.NewClientName = newClientName;
        }

        public override void DoTask(MainViewModel mvm)
        {
            User u;
            if (Sender.Users.TryGetValue(OldClientName, out u))
            {
                if (u == Sender.User)
                    Sender.User.Name = NewClientName;

                // To keep SortedDictionary sorted, first client will be removed..
                foreach (var chvm in u.Channels)
                {
                    if (chvm.Joined)
                        chvm.Users.Remove(u);
                }
                foreach (var chvm in u.PMChannels)
                    chvm.RemoveUserFromConversation(u, false, false);

                u.Name = NewClientName;
                Sender.Users.Remove(OldClientName);
                Sender.Users.Add(u.Name, u);

                // then later it will be readded with new Name
                foreach (var chvm in u.Channels)
                {
                    if (chvm.Joined)
                    {
                        chvm.Users.Add(u);
                        chvm.AddMessage(GlobalManager.SystemUser, string.Format(Localizations.GSLocalization.Instance.GSNicknameChange, OldClientName, NewClientName), MessageSettings.SystemMessage);
                    }
                }
                foreach (var chvm in u.PMChannels)
                {
                    chvm.AddUserToConversation(u, false, false);
                    chvm.AddMessage(GlobalManager.SystemUser, string.Format(Localizations.GSLocalization.Instance.GSNicknameChange, OldClientName, NewClientName), MessageSettings.SystemMessage);
                }
            }
        }
    }
}
