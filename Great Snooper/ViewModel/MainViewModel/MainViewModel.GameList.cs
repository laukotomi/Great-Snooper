namespace GreatSnooper.ViewModel
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using GalaSoft.MvvmLight;
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;

    public partial class MainViewModel : ViewModelBase, IDisposable
    {
        public void FreeGameProcess()
        {
            GameProcess.Dispose();
            GameProcess = null;
            lobbyWindow = IntPtr.Zero;
            gameWindow = IntPtr.Zero;
            ExitSnooperAfterGameStart = false;
            if (Properties.Settings.Default.EnergySaveModeGame && IsEnergySaveMode)
            {
                LeaveEnergySaveMode();
            }
        }

        private void HandleGameProcess()
        {
            // gameProcess = hoster.exe (HOST)
            // gameProcess = wa.exe (JOIN)
            if (GameProcess.HasExited)
            {
                SetBack();
                this.FreeGameProcess();
                return;
            }

            gameWindow = NativeMethods.FindWindow(null, "Worms2D");
            if (StartedGameType == StartedGameTypes.Join && ExitSnooperAfterGameStart && gameWindow != IntPtr.Zero)
            {
                this.CloseCommand.Execute(null);
                return;
            }

            lobbyWindow = NativeMethods.FindWindow(null, "Worms Armageddon");
            if (Properties.Settings.Default.EnergySaveModeGame && lobbyWindow != IntPtr.Zero)
            {
                if (NativeMethods.GetPlacement(lobbyWindow).showCmd == ShowWindowCommands.Normal)
                {
                    if (!IsEnergySaveMode)
                    {
                        this.EnterEnergySaveMode();
                    }
                }
                else if (IsEnergySaveMode)
                {
                    LeaveEnergySaveMode();
                }
            }
        }

        private void LoadGames(ChannelViewModel chvm)
        {
            loadGamesTask = Task.Factory.StartNew<bool>(() =>
            {
                try
                {
                    HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + this.WormNet.ServerAddress + ":80/wormageddonweb/GameList.asp?Channel=" + chvm.Name.Substring(1));
                    myHttpWebRequest.UserAgent = "T17Client/1.2";
                    myHttpWebRequest.Proxy = null;
                    myHttpWebRequest.AllowAutoRedirect = false;
                    myHttpWebRequest.Timeout = GlobalManager.WebRequestTimeout;
                    using (WebResponse myHttpWebResponse = myHttpWebRequest.GetResponse())
                    using (System.IO.Stream stream = myHttpWebResponse.GetResponseStream())
                    {
                        int bytes;
                        gameRecvSB.Clear();
                        while ((bytes = stream.Read(gameRecvBuffer, 0, gameRecvBuffer.Length)) > 0)
                        {
                            gameRecvSB.Append(WormNetCharTable.Instance.Decode(gameRecvBuffer, bytes));
                        }

                        gameRecvSB.Replace("\n", "");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                    return false;
                }
            })
            .ContinueWith((t) =>
            {
                if (this.closing)
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.CloseCommand.Execute(null);
                    }));
                    return;
                }

                if (t.Result == false || !chvm.Joined) // we already left the channel
                    return;

                try
                {
                    // <GAMELISTSTART><GAME GameName Hoster HosterAddress CountryID 1 PasswordNeeded GameID HEXCC><BR><GAME GameName Hoster HosterAddress CountryID 1 PasswordNeeded GameID HEXCC><BR><GAMELISTEND>
                    //string start = "<GAMELISTSTART>"; 15 chars
                    //string end = "<GAMELISTEND>"; 13 chars
                    if (gameRecvSB.Length > 28)
                    {
                        string[] games = gameRecvSB.ToString(15, gameRecvSB.Length - 15 - 13).Split(new string[] { "<BR>" }, StringSplitOptions.RemoveEmptyEntries);

                        // Set all the games we have in !isAlive state (we will know if the game is not active anymore)
                        foreach (var game in chvm.Games)
                        {
                            game.IsAlive = false;
                        }

                        for (int i = 0; i < games.Length; i++)
                        {
                            // <GAME GameName Hoster HosterAddress CountryID 1 PasswordNeeded GameID HEXCC><BR>
                            Match m = GameRegex.Match(games[i].Trim());
                            if (m.Success)
                            {
                                string name = m.Groups[1].Value.Replace('\b', ' ').Replace("#039", "\x12");

                                // Encode the name to decode it with GameDecode
                                int bytes = WormNetCharTable.Instance.GetBytes(name, 0, name.Length, gameRecvBuffer, 0);
                                name = WormNetCharTable.Instance.DecodeGame(gameRecvBuffer, bytes);

                                string hoster = m.Groups[2].Value;
                                string address = m.Groups[3].Value;

                                int countryID;
                                if (!int.TryParse(m.Groups[4].Value, out countryID))
                                {
                                    continue;
                                }

                                bool password = m.Groups[5].Value == "1";

                                uint gameID;
                                if (!uint.TryParse(m.Groups[6].Value, out gameID))
                                {
                                    continue;
                                }

                                string hexCC = m.Groups[7].Value;

                                // Get the country of the hoster
                                Country country;
                                if (hexCC.Length < 9)
                                {
                                    country = Countries.GetCountryByID(countryID);
                                }
                                else
                                {
                                    string hexstr = uint.Parse(hexCC).ToString("X");
                                    if (hexstr.Length == 8 && hexstr.Substring(0, 4) == "6487")
                                    {
                                        char c1 = WormNetCharTable.Instance.DecodeByte(byte.Parse(hexstr.Substring(6), System.Globalization.NumberStyles.HexNumber));
                                        char c2 = WormNetCharTable.Instance.DecodeByte(byte.Parse(hexstr.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
                                        country = Countries.GetCountryByCC(c1.ToString() + c2.ToString());
                                    }
                                    else
                                    {
                                        country = Countries.DefaultCountry;
                                    }
                                }

                                // Add the game to the list or set its isAlive state true if it is already in the list
                                Game game = chvm.Games.Where(x => x.ID == gameID).FirstOrDefault();
                                if (game != null)
                                {
                                    game.IsAlive = true;
                                }
                                else
                                {
                                    chvm.Games.Add(new Game(gameID, name, address, country, hoster, password));
                                    if (this.notificator.SearchInGameNamesEnabled &&
                                        this.notificator.GameNames.Any(r => r.IsMatch(name, hoster, chvm.Name)) ||
                                        this.notificator.SearchInHosterNamesEnabled &&
                                        this.notificator.HosterNames.Any(r => r.IsMatch(hoster, hoster, chvm.Name)))
                                    {
                                        NotificatorFound(string.Format(Localizations.GSLocalization.Instance.NotificatorGameText, hoster, name), chvm);
                                    }
                                }
                            }
                        }

                        // Delete inactive games from the list
                        for (int i = 0; i < chvm.Games.Count; i++)
                        {
                            if (!chvm.Games[i].IsAlive)
                            {
                                chvm.Games.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }

                chvm.GameListUpdatedTime = DateTime.Now;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}