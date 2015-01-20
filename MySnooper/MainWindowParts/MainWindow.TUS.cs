using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;


namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        // Joining a game
        private SortedDictionary<string, Client> TusUsers = new SortedDictionary<string,Client>();
        private CancellationTokenSource TusCTS = new CancellationTokenSource();


        public Task<string[]> StartTusCommunication()
        {
            return Task.Factory.StartNew<string[]>(() =>
            {
                try
                {
                    using (var tusRequest = new System.Net.WebClient() { Proxy = null })
                    {
                        string userlist;
                        if (GlobalManager.User.TusNick != string.Empty)
                            userlist = tusRequest.DownloadString("http://www.tus-wa.com/userlist.php?league=classic&update=" + System.Web.HttpUtility.UrlEncode(GlobalManager.User.TusNick));
                        else
                            userlist = tusRequest.DownloadString("http://www.tus-wa.com/userlist.php?league=classic");

                        if (TusCTS.IsCancellationRequested)
                        {
                            return null;
                        }
                        return userlist.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.log(ex);
                }

                return null;
            }, TusCTS.Token);
        }

        private void TUSLoaded(string[] result)
        {
            if (TusCTS.IsCancellationRequested)
            {
                this.Close();
                return;
            }

            if (result == null)
                return;

            foreach (var item in TusUsers)
                item.Value.TusActiveCheck = false;

            for (int i = 0; i < result.Length; i++)
            {
                string[] data = result[i].Split(new char[] { ' ' });

                string lowerName = data[0].ToLower();
                Client c;
                if (!TusUsers.TryGetValue(lowerName, out c))
                {
                    if (WormNetM.Clients.TryGetValue(lowerName, out c))
                    {
                        c.TusActive = true;
                        c.TusActiveCheck = true;
                        c.TusNick = data[1];
                        int rank;
                        if (int.TryParse(data[2].Substring(1), out rank))
                            c.Rank = RanksClass.GetRankByInt(rank - 1);
                        c.Country = CountriesClass.GetCountryByCC(data[3].ToUpper());
                        c.TusLink = data[4];
                        TusUsers.Add(lowerName, c);
                    }
                }
                else
                {
                    c.TusActiveCheck = true;
                }
            }

            // Remove tus users who doesn't active on tus anymore
            List<string> toDelete = new List<string>();
            foreach (var item in TusUsers)
            {
                if (!item.Value.TusActiveCheck)
                {
                    item.Value.TusActive = false;
                    toDelete.Add(item.Key);
                }
            }

            for (int i = 0; i < toDelete.Count; i++)
            {
                TusUsers.Remove(toDelete[i]);
            }
        }
    }
}
