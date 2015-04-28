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
        public Task<string[]> StartTusCommunication()
        {
            return Task.Factory.StartNew<string[]>(() =>
            {
                using (var tusRequest = new System.Net.WebClient() { Proxy = null })
                {
                    string userlist;
                    if (GlobalManager.User.TusNick != string.Empty)
                        userlist = tusRequest.DownloadString("http://www.tus-wa.com/userlist.php?league=classic&update=" + System.Web.HttpUtility.UrlEncode(GlobalManager.User.TusNick));
                    else
                        userlist = tusRequest.DownloadString("http://www.tus-wa.com/userlist.php?league=classic");

                    TusCTS.Token.ThrowIfCancellationRequested();

                    return userlist.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }, TusCTS.Token);
        }

        private void TUSLoaded(string[] result)
        {
            if (tusTask.IsCanceled || TusCTS.Token.IsCancellationRequested)
            {
                this.Close();
                return;
            }

            if (tusTask.IsFaulted)
                return;

            for (int i = 0; i < result.Length; i++)
            {
                string[] data = result[i].Split(new char[] { ' ' });

                string lowerName = data[0].ToLower();
                Client c;
                for (int j = 0; j < Servers.Count; j++)
                {
                    if (Servers[j].IsRunning && Servers[j].Clients.TryGetValue(lowerName, out c))
                    {
                        if (c.TusActive == false)
                        {
                            c.TusActive = true;
                            c.TusNick = data[1];
                            int rank;
                            if (int.TryParse(data[2].Substring(1), out rank))
                                c.Rank = RanksClass.GetRankByInt(rank - 1);
                            c.Country = CountriesClass.GetCountryByCC(data[3].ToUpper());
                            c.TusLink = data[4];
                        }
                    }
                }
            }
        }
    }
}
