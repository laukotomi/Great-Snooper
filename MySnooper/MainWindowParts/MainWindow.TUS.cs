using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;


namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        // Joining a game
        private BackgroundWorker TUSCommunicator;
        private SortedDictionary<string, Client> TusUsers;


        // "Constructor"
        public void TUS()
        {
            TUSCommunicator = new BackgroundWorker();
            TUSCommunicator.WorkerSupportsCancellation = true;
            TUSCommunicator.DoWork += TUSCommunication;
            TUSCommunicator.RunWorkerCompleted += TUSLoaded;

            TusUsers = new SortedDictionary<string, Client>();
        }

        private void TUSCommunication(object sender, System.ComponentModel.DoWorkEventArgs e)
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

                    e.Result = userlist.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.log(ex);
            }

            if (TUSCommunicator.CancellationPending)
                e.Cancel = true;
        }

        private void TUSLoaded(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                this.Close();
                return;
            }

            if (e.Error != null)
            {
                ErrorLog.log(e.Error);
                return;
            }

            if (e.Result == null) // case of net cut this may happen!
                return;

            foreach (var item in TusUsers)
                item.Value.TusActiveCheck = false;

            string[] TusRows = (string[])e.Result;
            for (int i = 0; i < TusRows.Length; i++)
            {
                string[] data = TusRows[i].Split(new char[] { ' ' });

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
