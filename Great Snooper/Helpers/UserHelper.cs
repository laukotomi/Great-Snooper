namespace GreatSnooper.Helpers
{
    using GreatSnooper.Classes;
    using GreatSnooper.Model;

    public static class UserHelper
    {
        public static User CreateUser(AbstractCommunicator server, string name, string clan = "")
        {
            User user = new User(name, clan);
            user.IsBanned = GlobalManager.BanList.Contains(user.Name);
            if (server is WormNetCommunicator)
            {
                TusAccount tusAccount;
                if (GlobalManager.TusAccounts.TryGetValue(name, out tusAccount))
                {
                    user.TusAccount = tusAccount;
                    tusAccount.User = user;
                }
            }
            server.Users.Add(user.Name, user);
            return user;
        }

        public static void FinalizeUser(AbstractCommunicator server, User u)
        {
            if (u.TusAccount != null)
            {
                u.TusAccount.User = null;
                u.TusAccount = null;
            }
            // server.Users.Remove(u.Name);
            u.OnlineStatus = User.Status.Offline;
        }

        public static User GetUser(AbstractCommunicator server, string name, string clan = "")
        {
            User user;
            if (!server.Users.TryGetValue(name, out user))
            {
                return CreateUser(server, name, clan);
            }
            return user;
        }
    }
}