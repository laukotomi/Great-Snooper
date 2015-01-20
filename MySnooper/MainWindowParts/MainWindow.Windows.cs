using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;


namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        // Here are all the things, that open a window such as
        // Buddy list, Ignore list, Hosting a game, Settings

        // Opens a window to host a game
        private void GameHosting(object sender, RoutedEventArgs e)
        {
            if (GameListChannel == null || !GameListChannel.Joined)
            {
                MessageBox.Show(this, "You have to join a channel before host a game!", "Fail", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            if (!File.Exists(Path.GetFullPath("Hoster.exe")))
            {
                MessageBox.Show(this, "Hoster.exe doesn't exist! You can't host without that file!", "Hoster.exe missing", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            if (HostGame.IsBusy)
            {
                MessageBox.Show(this, "You are already hosted a game!", "Already hosted", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (StartGame.IsBusy)
            {
                MessageBox.Show(this, "You are in a game!", "Fail", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!CheckWAExe())
                return;

            string hexcc = "6487" + WormNetCharTable.Encode[GlobalManager.User.Country.CountryCode[1]].ToString("X") + WormNetCharTable.Encode[GlobalManager.User.Country.CountryCode[0]].ToString("X");

            HostingWindow = new Hosting(ServerAddress, GameListChannel.Name.Substring(1), GameListChannel.Scheme, hexcc);
            HostingWindow.GameHosted += GameHosted;
            HostingWindow.Closing += HostingClosing;
            HostingWindow.Owner = this;
            HostingWindow.ShowDialog();
        }

        private void HostingClosing(object sender, CancelEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                var obj = sender as Hosting;
                obj.GameHosted -= GameHosted;
                obj.Closing -= HostingClosing;
            }
            ));
        }

        private void GameHosted(string parameters, bool exit)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                ExitSnooper = exit;
                if (!SnooperClosing)
                    HostGame.RunWorkerAsync(parameters);
            }
            ));
        }

        // Settings window
        private void SettingsClicked(object sender, RoutedEventArgs e)
        {
            UserSettings window = new UserSettings();
            window.MessageSettingsEvent += MessageSettingChanged;
            window.SoundChanged += SoundChanged;
            window.SoundEnabledChanged += SoundEnabledChanged;
            window.Closing += SettingsClosing;
            window.Owner = this;
            window.ShowDialog();
            e.Handled = true;
        }

        // These are here coz I didn't want to use lockers
        private void SoundEnabledChanged(string name, bool enabled)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                switch (name)
                {
                    case "PMBeepEnabled":
                        Properties.Settings.Default.PMBeepEnabled = enabled;
                        break;
                    case "HBeepEnabled":
                        Properties.Settings.Default.HBeepEnabled = enabled;
                        break;
                    case "BJBeepEnabled":
                        Properties.Settings.Default.BJBeepEnabled = enabled;
                        break;
                    case "LeagueFoundBeepEnabled":
                        Properties.Settings.Default.LeagueFoundBeepEnabled = enabled;
                        break;
                    case "LeagueFailBeepEnabled":
                        Properties.Settings.Default.LeagueFailBeepEnabled = enabled;
                        break;
                }
                Properties.Settings.Default.Save();
            }
            ));
        }

        // These are here coz I didn't want to use lockers
        private void SoundChanged(string name)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                try
                {
                    switch (name)
                    {
                        case "PMBeepChange":
                            PrivateMessageBeep = new SoundPlayer(new FileInfo(Properties.Settings.Default.PMBeep).FullName);
                            break;
                        case "HbeepChange":
                            HighlightBeep = new SoundPlayer(new FileInfo(Properties.Settings.Default.HBeep).FullName);
                            break;
                        case "BJBeepChange":
                            BuddyOnlineBeep = new SoundPlayer(new FileInfo(Properties.Settings.Default.BJBeep).FullName);
                            break;
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.log(e);
                }
            }
            ));
        }

        private void MessageSettingChanged()
        {
            foreach (var item in WormNetM.ChannelList)
            {
                if (item.Value.Joined)
                    LoadMessages(item.Value, GlobalManager.MaxMessagesDisplayed, true);
            }
        }

        private void SettingsClosing(object sender, CancelEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                var obj = sender as UserSettings;
                obj.MessageSettingsEvent -= MessageSettingChanged;
                obj.SoundChanged -= SoundChanged;
                obj.SoundEnabledChanged -= SoundEnabledChanged;
                obj.Closing -= SettingsClosing;
            }
            ));
        }

        private void BuddyListClicked(object sender, RoutedEventArgs e)
        {
            SortedObservableCollection<string> List2 = new SortedObservableCollection<string>();
            foreach (var item in WormNetM.BuddyList)
                List2.Add(item.Value);

            ListEditor window = new ListEditor(List2, "Your buddy list");
            window.Closing += BuddyListWindowClosed;
            window.ItemRemoved += RemoveUserFromBuddyList;
            window.ItemAdded += AddUserToBuddyList;
            window.Owner = this;
            window.ShowDialog();
            e.Handled = true;
        }

        private void BuddyListWindowClosed(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                var obj = sender as ListEditor;
                obj.Closing -= BuddyListWindowClosed;
                obj.ItemRemoved -= RemoveUserFromBuddyList;
                obj.ItemAdded -= AddUserToBuddyList;
            }
            ));
        }

        private void RemoveUserFromBuddyList(string name)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                WormNetM.RemoveBuddy(name);
            }
            ));
        }

        private void AddUserToBuddyList(string name)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                WormNetM.AddBuddy(name);
            }
            ));
        }

        private void BanListClicked(object sender, RoutedEventArgs e)
        {
            SortedObservableCollection<string> List2 = new SortedObservableCollection<string>();
            foreach (var item in WormNetM.BanList)
                List2.Add(item.Value);

            ListEditor window = new ListEditor(List2, "Your ignore list");
            window.Closing += BanListWindowClosed;
            window.ItemRemoved += RemoveUserFromBanList;
            window.ItemAdded += AddUserToBanList;
            window.Owner = this;
            window.ShowDialog();
            e.Handled = true;
        }

        private void BanListWindowClosed(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                var obj = sender as ListEditor;
                obj.Closing -= BanListWindowClosed;
                obj.ItemRemoved -= RemoveUserFromBanList;
                obj.ItemAdded -= AddUserToBanList;
            }
            ));
        }

        private void RemoveUserFromBanList(string name)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                WormNetM.RemoveBan(name);
            }
            ));
        }

        private void AddUserToBanList(string name)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                WormNetM.AddBan(name);
            }
            ));
        }

        // League searcher
        private void LeagueSearcher(object sender, RoutedEventArgs e)
        {
            if (Leagues.Count == 0)
            {
                MessageBox.Show(this, "Failed to load league games!", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (GameListChannel == null || !GameListChannel.Joined)
            {
                MessageBox.Show(this, "You have to join a channel, where you can look for league games!", "Fail", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            LeagueSearcher window = new LeagueSearcher(Leagues, FoundUsers, SpamText != string.Empty, SearchHere);
            window.LuckyLuke += LuckyLuke;
            window.Closing += SearcherClosing;
            window.Owner = this;
            window.ShowDialog();
            e.Handled = true;
        }

        private void SearcherClosing(object sender, CancelEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                LeagueSearcher window = (LeagueSearcher)sender;
                window.LuckyLuke -= LuckyLuke;
                window.Closing -= SearcherClosing;
            }
            ));
        }

        private void LuckyLuke(Dictionary<string, string> leagues, bool spam)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                if (leagues == null) // Stop
                {
                    // Same goes in MainWindow.xaml.cs!
                    SearchCounter = 100;
                    SpamCounter = 0;
                    SearchHere = null;
                    SpamText = string.Empty;
                    FoundUsers.Clear();
                }
                else
                {
                    SearchHere = GameListChannel;

                    if (spam)
                    {
                        var sb = new System.Text.StringBuilder();
                        int i = 0;
                        foreach (var item in leagues)
                        {
                            i++;
                            sb.Append(item.Value);
                            if (i < leagues.Count)
                                sb.Append(" or ");
                        }
                        sb.Append(" anyone?");
                        SpamText = sb.ToString();
                    }

                    FoundUsers.Clear();
                    foreach (var item in leagues)
                        FoundUsers.Add(item.Key, new List<string>());
                }
            }
            ));
        }

        private void AwayManager(object sender, RoutedEventArgs e)
        {
            AwayManager window = new AwayManager(AwayText);
            window.AwayChanged += AwayChanged;
            window.Closing += AwayManagerClosing;
            window.Owner = this;
            window.ShowDialog();
            e.Handled = true;
        }

        private void AwayManagerClosing(object sender, CancelEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                AwayManager window = (AwayManager)sender;
                window.AwayChanged -= AwayChanged;
                window.Closing -= AwayManagerClosing;
            }
            ));
        }

        private void AwayChanged(bool Away)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                if (Away)
                    this.AwayText = (Properties.Settings.Default.AwayMessage.Length == 0) ? "No reason specified." : Properties.Settings.Default.AwayMessage;
                else
                    this.AwayText = string.Empty;
            }
            ));
        }

        private void OpenNewsWindow()
        {
            News window = new News(NewsList, NewsSeen);
            window.Owner = this;
            window.ShowDialog();
        }
    }
}
