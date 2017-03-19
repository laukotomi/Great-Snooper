namespace GreatSnooper.Helpers
{
    using GreatSnooper.IRC;
    using GreatSnooper.Model;

    public static class UserHelper
    {
        private static User CreateUser(IRCCommunicator server, string name, string clan = "")
        {
            User user = new User(server, name, clan);
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

        public static User GetUser(IRCCommunicator server, string name, string clan = "", bool createIfNotExists = true)
        {
            User user;
            if (server.Users.TryGetValue(name, out user))
            {
                return user;
            }
            else if (createIfNotExists)
            {
                return CreateUser(server, name, clan);
            }
            return null;
        }

        public static void UpdateMessageStyle(User user)
        {
            foreach (Message message in user.Messages)
            {
                message.UpdateNickStyle();
            }
        }
    }
}