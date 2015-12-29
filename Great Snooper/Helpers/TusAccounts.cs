using GreatSnooper.Classes;
using GreatSnooper.Model;
using System;
using System.Collections.Generic;

namespace GreatSnooper.Helpers
{
    public static class TusAccounts
    {
        public static DateTime tusAccountsLoaded = new DateTime(1999, 5, 31);

        public static void SetTusAccounts(string[] rows, AbstractCommunicator server = null)
        {
            foreach (var account in GlobalManager.TusAccounts)
                account.Value.Active = false;

            foreach (var row in rows)
            {
                string[] data = row.Split(new char[] { ' ' });
                if (data.Length == 6 && Uri.IsWellFormedUriString(data[4], UriKind.Absolute))
                {
                    TusAccount tusAccount;
                    if (!GlobalManager.TusAccounts.TryGetValue(data[0], out tusAccount))
                    {
                        tusAccount = new TusAccount(data);
                        GlobalManager.TusAccounts.Add(data[0], tusAccount);
                    }
                    else
                        tusAccount.Active = true;

                    User u;
                    if (server != null && server.State == AbstractCommunicator.ConnectionStates.Connected && server.Users.TryGetValue(data[0], out u) && u.TusAccount == null)
                    {
                        u.TusAccount = tusAccount;
                        tusAccount.User = u;
                    }
                }
            }

            var toRemove = new List<string>();
            foreach (var account in GlobalManager.TusAccounts)
            {
                if (account.Value.Active == false)
                {
                    if (account.Value.User != null)
                    {
                        account.Value.User.TusAccount = null;
                        account.Value.User = null;
                    }
                    toRemove.Add(account.Key);
                }
            }

            foreach (var key in toRemove)
                GlobalManager.TusAccounts.Remove(key);

            tusAccountsLoaded = DateTime.Now;
        }
    }
}
