using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        // Here are all the things, that open a window such as
        // Buddy list, Ignore list, Hosting a game, Settings

        // Opens a window to host a game
        private void GameHosting(object sender, RoutedEventArgs e)
        {
            if (gameListChannel == null || !gameListChannel.Joined)
            {
                MessageBox.Show(this, "You have to join a channel before host a game!", "Fail", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            if (!File.Exists(Path.GetFullPath("Hoster.exe")))
            {
                MessageBox.Show(this, "Hoster.exe doesn't exist! You can't host without that file!", "Hoster.exe missing", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            if (!CheckWAExe())
                return;

            if (gameProcess != null)
            {
                if (startedGameType == StartedGameTypes.Join)
                {
                    MessageBox.Show(this, "You are in a game!", "Fail", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else
                {
                    // Because of wormkit rehost module, this should be allowed
                    gameProcess.Dispose();
                    gameProcess = null;
                }
            }

            if (searchHere != null && Properties.Settings.Default.AskLeagueSearcherOff)
            {
                MessageBoxResult res = MessageBox.Show("Would you like to turn off league searcher?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                    ClearSpamming();
            }

            if (Notifications.Count != 0 && Properties.Settings.Default.AskNotificatorOff)
            {
                MessageBoxResult res = MessageBox.Show("Would you like to turn off notificator?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    Notifications.Clear();
                    NotificatorImage.Source = NotificatorOff;
                }
            }

            string hexcc = "6487" + WormNetCharTable.Encode[GlobalManager.User.Country.CountryCode[1]].ToString("X") + WormNetCharTable.Encode[GlobalManager.User.Country.CountryCode[0]].ToString("X");

            HostingWindow = new Hosting(ServerAddress, gameListChannel.Name.Substring(1), gameListChannel.Scheme, hexcc);
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

        private void GameHosted(object sender, GameHostedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                if (!snooperClosing)
                {
                    ExitSnooper = e.Arguments;

                    startedGameType = StartedGameTypes.Host;
                    gameProcess = new System.Diagnostics.Process();
                    gameProcess.StartInfo.UseShellExecute = false;
                    gameProcess.StartInfo.CreateNoWindow = true;
                    gameProcess.StartInfo.RedirectStandardOutput = true;
                    gameProcess.StartInfo.FileName = System.IO.Path.GetFullPath("Hoster.exe");
                    gameProcess.StartInfo.Arguments = e.Parameters;
                    gameProcess.Start();
                    string success = gameProcess.StandardOutput.ReadLine();

                    if (success == "1")
                    {
                        HostingWindow.Close();

                        if (Properties.Settings.Default.HostInfoToChannel) // GameListChannel is ok, coz it can't be changed since the Hosting window is opened
                            SendMessageToChannel(">is hosting a game: " + Properties.Settings.Default.HostGameName, gameListChannel);

                        if (ExitSnooper)
                        {
                            snooperClosing = true;
                            this.Close();
                            return;
                        }

                        if (Properties.Settings.Default.MarkAway)
                            SendMessageToChannel("/away", null);

                        if (Properties.Settings.Default.HideSnooper)
                            this.Hide();
                    }
                    else
                    {
                        HostingWindow.RestoreHostButton();
                        MessageBox.Show(this, "Failed to host a game. You may host too many games recently. Please wait some minutes!", "Failed to host a game", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            ));
        }

        // Settings window
        private void SettingsClicked(object sender, RoutedEventArgs e)
        {
            UserSettings window = new UserSettings();
            window.SettingChanged += SettingChanged;
            window.Closing += SettingsClosing;
            window.Owner = this;
            window.ShowDialog();
            e.Handled = true;
        }

        private void SettingChanged(object sender, SettingChangedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                switch (e.Type)
                {
                    case SettingChangedType.Sound:
                        string value = (string)(Properties.Settings.Default.GetType().GetProperty(e.SettingName).GetValue(Properties.Settings.Default, null));
                        if (soundPlayers.ContainsKey(e.SettingName))
                            soundPlayers[e.SettingName] = new SoundPlayer(new FileInfo(value).FullName);
                        else
                            soundPlayers.Add(e.SettingName, new SoundPlayer(new FileInfo(value).FullName));
                        break;

                    case SettingChangedType.Style:
                        for (int i = 0; i < servers.Count; i++)
                        {
                            if (servers[i].IsRunning)
                            {
                                foreach (var item in servers[i].ChannelList)
                                {
                                    if (item.Value.Joined)
                                        LoadMessages(item.Value, GlobalManager.MaxMessagesDisplayed, true);
                                }
                            }
                        }
                        break;

                    default:
                        switch (e.SettingName)
                        {
                            case "ShowWormsChannel":
                                Channel ch = servers[1].ChannelList["#worms"];
                                if (Properties.Settings.Default.ShowWormsChannel)
                                {
                                    for (int i = 0; i < Channels.Items.Count; i++)
                                    {
                                        Channel channel = (Channel)((TabItem)Channels.Items[i]).DataContext;
                                        if (channel.IsPrivMsgChannel)
                                        {
                                            Channels.Items.Insert(i, ch.ChannelTabItem);
                                            return;
                                        }
                                    }
                                    Channels.Items.Add(ch.ChannelTabItem);
                                }
                                else
                                {
                                    if (ch.Joined)
                                    {
                                        gameSurgeIsConnected = false;
                                        ch.Part();
                                        ch.Server.CancelAsync();
                                    }
                                    CloseChannelTab(ch, true);
                                }
                                break;

                            case "ShowBannedUsers":
                                for (int i = 0; i < servers.Count; i++)
                                {
                                    foreach (var item in servers[i].ChannelList)
                                    {
                                        if (!item.Value.IsPrivMsgChannel && item.Value.TheDataGrid.ItemsSource != null)
                                        {
                                            var view = CollectionViewSource.GetDefaultView(item.Value.TheDataGrid.ItemsSource);
                                            if (view != null)
                                            {
                                                if (!Properties.Settings.Default.ShowBannedUsers)
                                                {
                                                    view.Filter = o =>
                                                    {
                                                        Client c = o as Client;
                                                        if (c.IsBanned)
                                                            return false;
                                                        return true;
                                                    };
                                                }
                                                else
                                                    view.Filter = null;
                                            }
                                        }
                                    }
                                }
                                break;

                            case "MessageTime":
                            case "ShowBannedMessages":
                                // Reload messages
                                for (int i = 0; i < servers.Count; i++)
                                {
                                    if (servers[i].IsRunning)
                                    {
                                        foreach (var item in servers[i].ChannelList)
                                        {
                                            if (item.Value.Joined)
                                                LoadMessages(item.Value, GlobalManager.MaxMessagesDisplayed, true);
                                        }
                                    }
                                }
                                break;

                            case "ShowInfoColumn":
                                for (int i = 0; i < servers.Count; i++)
                                {
                                    foreach (var item in servers[i].ChannelList)
                                    {
                                        if (!item.Value.IsPrivMsgChannel && item.Value.TheDataGrid != null)
                                        {
                                            item.Value.TheDataGrid.Columns[4].Visibility = (Properties.Settings.Default.ShowInfoColumn) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                                        }
                                    }
                                }
                                break;
                        }
                        break;
                }
            }));
        }

        private void SettingsClosing(object sender, CancelEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                var obj = sender as UserSettings;
                obj.SettingChanged -= SettingChanged;
                obj.Closing -= SettingsClosing;
            }
            ));
        }

        private void BuddyListClicked(object sender, RoutedEventArgs e)
        {
            SortedObservableCollection<string> List2 = new SortedObservableCollection<string>();
            foreach (var item in buddyList)
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

        private void RemoveUserFromBuddyList(object sender, StringEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                RemoveBuddy(e.Argument);
            }
            ));
        }

        private void AddUserToBuddyList(object sender, StringEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                AddBuddy(e.Argument);
            }
            ));
        }

        private void BanListClicked(object sender, RoutedEventArgs e)
        {
            SortedObservableCollection<string> List2 = new SortedObservableCollection<string>();
            foreach (var item in banList)
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

        private void RemoveUserFromBanList(object sender, StringEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                RemoveBan(e.Argument);
            }
            ));
        }

        private void AddUserToBanList(object sender, StringEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                AddBan(e.Argument);
            }
            ));
        }

        // League searcher
        private void LeagueSearcher(object sender, RoutedEventArgs e)
        {
            if (leagues.Count == 0)
            {
                MessageBox.Show(this, "Leagues may be still loading, please wait!", "No leagues available", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            if (gameListChannel == null || !gameListChannel.Joined)
            {
                MessageBox.Show(this, "You have to join a channel, where you can look for league games!", "Fail", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            LeagueSearcher window = new LeagueSearcher(leagues, searchHere != null, spamAllowed);
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

        private void LuckyLuke(object sender, LookForTheseEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                if (e == null) // Stop request
                {
                    ClearSpamming();
                }
                else
                {
                    searchHere = gameListChannel;

                    if (e.Spam)
                    {
                        var sb = new System.Text.StringBuilder();
                        int i = 0;
                        foreach (var item in e.Leagues)
                        {
                            i++;
                            sb.Append(item.Value);
                            if (i < e.Leagues.Count)
                                sb.Append(" or ");
                        }
                        sb.Append(" anyone?");
                        spamText = sb.ToString();
                    }

                    foundUsers.Clear();
                    foreach (var item in e.Leagues)
                        foundUsers.Add(item.Key, new List<string>());
                }
            }
            ));
        }

        private void NotificatorOpen(object sender, RoutedEventArgs e)
        {
            if (gameListChannel == null || !gameListChannel.Joined)
            {
                MessageBox.Show(this, "You have to join a channel first!", "Fail", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            Notificator window = new Notificator(Notifications.Count != 0);
            window.NotificatorEvent += window_NotificatorEvent;
            window.Closing += NotificatorClosing;
            window.Owner = this;
            window.ShowDialog();
            e.Handled = true;
        }

        void window_NotificatorEvent(object sender, NotificatorEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                if (e == null) // Stop request
                {
                    Notifications.Clear();
                    NotificatorImage.Source = NotificatorOff;
                }
                else
                {
                    foreach (NotificatorClass nc in e.NotificatorList)
                        Notifications.Add(nc);
                    NotificatorImage.Source = NotificatorOn;
                }
            }
            ));
        }

        private void NotificatorClosing(object sender, CancelEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                Notificator window = (Notificator)sender;
                window.NotificatorEvent -= window_NotificatorEvent;
                window.Closing -= NotificatorClosing;
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

        private void AwayChanged(object sender, BoolEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                if (e.Argument)
                    SendMessageToChannel("/away " + Properties.Settings.Default.AwayMessage, null);
                else
                    SendMessageToChannel("/back", null);
            }
            ));
        }

        private void OpenNewsWindow()
        {
            News window = new News(newsList, newsSeen);
            window.Owner = this;
            window.ShowDialog();
        }
    }
}
