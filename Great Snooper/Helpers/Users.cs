using GreatSnooper.Classes;
using GreatSnooper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GreatSnooper.Helpers
{
    public static class Users
    {
        public static User CreateUser(AbstractCommunicator server, string name, string clan = null)
        {
            var u = new User(name, clan);
            u.IsBanned = GlobalManager.BanList.Contains(u.Name);
            if (server is WormNetCommunicator)
            {
                TusAccount tusAccount;
                if (GlobalManager.TusAccounts.TryGetValue(name, out tusAccount))
                {
                    u.TusAccount = tusAccount;
                    tusAccount.User = u;
                }
            }
            server.Users.Add(u.Name, u);
            return u;
        }

        public static void FinalizeUser(AbstractCommunicator server, User u)
        {
            if (u.TusAccount != null)
            {
                u.TusAccount.User = null;
                u.TusAccount = null;
            }
            server.Users.Remove(u.Name);
        }
    }
}
