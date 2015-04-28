using Hardcodet.Wpf.TaskbarNotification;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;


namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        private readonly string ServerAddress;

        // Lists
        private readonly Dictionary<string, string> leagues = new Dictionary<string,string>();
        private readonly Dictionary<string, string> banList = new Dictionary<string, string>();
        private readonly Dictionary<string, string> buddyList = new Dictionary<string, string>();
        public readonly Dictionary<string, string> AutoJoinList = new Dictionary<string, string>();
        public readonly List<IRCCommunicator> Servers = new List<IRCCommunicator>();
        private readonly List<Dictionary<string, string>> newsList = new List<Dictionary<string,string>>();
        private readonly List<int> visitedChannels = new List<int>();
        private readonly Dictionary<string, bool> newsSeen = new Dictionary<string,bool>();
        public readonly Dictionary<string, List<string>> FoundUsers = new Dictionary<string, List<string>>();
        public readonly List<NotificatorClass> Notifications = new List<NotificatorClass>();

        // Buffer things + communication
        private readonly byte[] recvBuffer = new byte[10240]; // 10kB
        private readonly StringBuilder recvHTML = new StringBuilder(10240); // 10kB
        private readonly CancellationTokenSource TusCTS = new CancellationTokenSource();
        private CancellationTokenSource loadSettingsCTS = new CancellationTokenSource();
        private Task tusTask;
        private Task loadSettings;
        private string latestVersion = string.Empty;

        // WormNet Web Communicator
        public readonly WormageddonWebComm WormWebC;

        // Helpers
        private Channel gameListChannel;
        public bool GameSurgeIsConnected = false;
        public bool IsWindowFocused = true;
        private WindowState lastWindowState = WindowState.Maximized;
        public TaskbarIcon NotifyIcon
        {
            get { return myNotifyIcon; }
        }

        // Timers
        private DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Input);
        private DispatcherTimer focusTimer = new DispatcherTimer(DispatcherPriority.Input);
        private DispatcherTimer filterTimer = new DispatcherTimer(DispatcherPriority.Input);
        private int gameListCounter = 0;
        private int tusRequestCounter = 10;
        public bool GameListForce = false;
        public bool TusForce = false;
        private bool snooperClosing = false;
        private bool spamAllowed = false;
        public bool EnergySaveModeOn = false;

        // Sounds
        private Dictionary<string, SoundPlayer> soundPlayers = new Dictionary<string,SoundPlayer>();

        // League Seacher things
        private Channel _searchHere;
        public Channel SearchHere
        {
            get
            {
                return _searchHere;
            }
            set
            {
                _searchHere = value;
                if (leagueSearcherImage != null)
                {
                    if (value == null)
                        leagueSearcherImage.Source = leagueSearcherOff;
                    else
                        leagueSearcherImage.Source = leagueSearcherOn;
                }
            }
        }
        private string spamText = string.Empty;
        private int searchCounter = 100;
        private int spamCounter;

        // Keyboard events
        public static RoutedCommand NextChannelCommand = new RoutedCommand();
        public static RoutedCommand PrevChannelCommand = new RoutedCommand();
        public static RoutedCommand CloseChannelCommand = new RoutedCommand();
        public static RoutedCommand FilterCommand = new RoutedCommand();
        public static RoutedCommand DoubleClickCommand = new RoutedCommand();

        // Away
        private string _awayText = string.Empty;
        private string backText = string.Empty;
        public string AwayText
        {
            get
            {
                return _awayText;
            }
            private set
            {
                _awayText = value;
                if (awayOnOffImage != null)
                {
                    if (value == string.Empty)
                    {
                        awayOnOffImage.Source = awayOffImage;
                        awayOnOffButton.ToolTip = awayOnOffDefaultTooltip;
                    }
                    else
                    {
                        awayOnOffImage.Source = awayOnImage;
                        awayOnOffButton.ToolTip = "You are away: " + value;
                    }
                }
                if (value != string.Empty)
                {
                    for (int i = 0; i < Servers.Count; i++)
                    {
                        if (Servers[i].IsRunning)
                        {
                            foreach (var item in Servers[i].ChannelList)
                            {
                                if (item.Value.IsPrivMsgChannel)
                                {
                                    item.Value.SendAway = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < Servers.Count; i++)
                    {
                        if (Servers[i].IsRunning)
                        {
                            foreach (var item in Servers[i].ChannelList)
                            {
                                if (item.Value.IsPrivMsgChannel)
                                {
                                    item.Value.SendAway = false;

                                    if (Properties.Settings.Default.SendBack && item.Value.SendBack && item.Value.Messages.Count > 0 && backText.Length > 0)
                                    {
                                        SendMessageToChannel(backText, item.Value);
                                    }

                                    item.Value.SendBack = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Constructor        
        public MainWindow() { } // Never used, but visual stdio throws an error if not exists
        public MainWindow(IRCCommunicator WormNetC, string serverAddress)
        {
            InitializeComponent();
            GameListGridRow.Height = new GridLength(Properties.Settings.Default.GameListGridRowStarts, GridUnitType.Star);
            RightColumn.Width = new GridLength(Properties.Settings.Default.RightColumnStars, GridUnitType.Star);
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            this.DataContext = this;
            this.ServerAddress = serverAddress;
            this.WelcomeText.Text = "Welcome " + GlobalManager.User.Name + "!";

            // Servers
            WormNetC.ConnectionState += ConnectionState;
            Servers.Add(WormNetC);

            IRCCommunicator gameSurge = new IRCCommunicator("irc.gamesurge.net", 6667, false);
            gameSurge.ConnectionState += ConnectionState;
            Servers.Add(gameSurge);

            // Wormageddonweb Communicator
            WormWebC = new WormageddonWebComm(this, serverAddress);

            // Focustimer will focus to the textbox of a channel when we change channel
            focusTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
            focusTimer.Tick += SetFocusTextBox;

            filterTimer.Interval = new TimeSpan(0, 0, 0, 0, 300);
            filterTimer.Tick += filterTimer_Tick;


            // Load sounds
            if (File.Exists(Properties.Settings.Default.PMBeep))
                soundPlayers.Add("PMBeep", new SoundPlayer(new FileInfo(Properties.Settings.Default.PMBeep).FullName));
            if (File.Exists(Properties.Settings.Default.BJBeep))
                soundPlayers.Add("BJBeep", new SoundPlayer(new FileInfo(Properties.Settings.Default.BJBeep).FullName));
            if (File.Exists(Properties.Settings.Default.HBeep))
                soundPlayers.Add("HBeep", new SoundPlayer(new FileInfo(Properties.Settings.Default.HBeep).FullName));
            if (File.Exists(Properties.Settings.Default.LeagueFoundBeep))
                soundPlayers.Add("LeagueFoundBeep", new SoundPlayer(new FileInfo(Properties.Settings.Default.LeagueFoundBeep).FullName));
            if (File.Exists(Properties.Settings.Default.LeagueFailBeep))
                soundPlayers.Add("LeagueFailBeep", new SoundPlayer(new FileInfo(Properties.Settings.Default.LeagueFailBeep).FullName));
            if (File.Exists(Properties.Settings.Default.NotificatorSound))
                soundPlayers.Add("NotificatorSound", new SoundPlayer(new FileInfo(Properties.Settings.Default.NotificatorSound).FullName));

            // Deserialize lists
            this.buddyList.DeSerialize(Properties.Settings.Default.BuddyList);
            this.banList.DeSerialize(Properties.Settings.Default.BanList);
            this.AutoJoinList.DeSerialize(Properties.Settings.Default.AutoJoinChannels);

            // Unserialize newsseen
            string[] list = Properties.Settings.Default.NewsSeen.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < list.Length; i++)
                newsSeen.Add(list[i], false);

            if (Properties.Settings.Default.SaveInstantColors)
            {
                // Unserialize instant colors
                list = Properties.Settings.Default.InstantColors.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < list.Length; i++)
                {
                    string[] keyValue = list[i].Split(new char[] { ':' });
                    var color = Color.FromRgb(
                        byte.Parse(keyValue[1].Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                        byte.Parse(keyValue[1].Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                        byte.Parse(keyValue[1].Substring(4, 2), System.Globalization.NumberStyles.HexNumber)
                    );
                    var scb = new SolidColorBrush(color);
                    scb.Freeze();
                    ChoosedColors.Add(keyValue[0], scb);
                }
            }

            // Initialize a timer which will help updating things that should be updated periodically (game list, news..)
            // Initialize a timer which will process data that is sent to the UI thread from the irc threads
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            timer.Tick += timer_Tick;
            timer.Start();

            // Get channels
            Channels.Items.Clear();
            WormNetC.GetChannelList();

            this.StateChanged += MainWindow_StateChanged;
        }

        void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.EnergySaveMode)
            {
                if (this.WindowState == System.Windows.WindowState.Minimized && !EnergySaveModeOn)
                    EnterEnergySaveMode();
                else if (EnergySaveModeOn)
                    LeaveEnergySaveMode();
            }
        }

        private void EnterEnergySaveMode()
        {
            EnergySaveModeOn = true;
            for (int i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].IsRunning)
                {
                    foreach (var item in Servers[i].ChannelList)
                    {
                        if (item.Value.Joined)
                        {
                            if (item.Value.TheDataGrid != null)
                                item.Value.TheDataGrid.ItemsSource = null;
                            if (item.Value.GameListBox != null)
                                item.Value.GameListBox.ItemsSource = null;
                        }
                    }
                }
            }
        }

        private void LeaveEnergySaveMode()
        {
            EnergySaveModeOn = false;
            for (int i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].IsRunning)
                {
                    foreach (var item in Servers[i].ChannelList)
                    {
                        if (item.Value.Joined)
                        {
                            if (item.Value.TheDataGrid != null && item.Value.TheDataGrid.ItemsSource == null)
                                item.Value.TheDataGrid.ItemsSource = item.Value.Clients;
                            if (item.Value.GameListBox != null && item.Value.GameListBox.ItemsSource == null)
                                item.Value.GameListBox.ItemsSource = item.Value.GameList;
                            if (item.Value.MessageReloadNeeded)
                            {
                                item.Value.MessageReloadNeeded = false;
                                LoadMessages(item.Value, GlobalManager.MaxMessagesDisplayed, true);
                            }
                        }
                    }
                }
            }
        }

        private void MainWindow_Loaded(object sender, EventArgs e)
        {
            soundEnabled = !Properties.Settings.Default.MuteState;

            // Download news and league list
            LoadSettings();
        }

        private void LoadSettings()
        {
            loadSettings = Task.Factory.StartNew(() =>
            {
                string SettingsXML = GlobalManager.SettingsPath + @"\Settings.xml";

                try
                {
                    string SettingsXMLTemp = GlobalManager.SettingsPath + @"\SettingsTemp.xml";

                    using (WebDownload webClient = new WebDownload() { Proxy = null })
                    {
                        webClient.DownloadFile("http://mediacreator.hu/SnooperSettings.xml", SettingsXMLTemp);
                    }

                    // If downloading will fail then leagues won't be loaded. If they would, it could be hacked easily.
                    spamAllowed = true;

                    if (File.Exists(SettingsXML))
                        File.Delete(SettingsXML);

                    File.Move(SettingsXMLTemp, SettingsXML);
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }

                loadSettingsCTS.Token.ThrowIfCancellationRequested();

                if (File.Exists(SettingsXML))
                {
                    Dictionary<string, string> serverList = new Dictionary<string, string>();
                    serverList.DeSerialize(Properties.Settings.Default.ServerAddresses);
                    bool update = false;

                    using (XmlReader xml = XmlReader.Create(SettingsXML))
                    {
                        xml.ReadToFollowing("servers");
                        using (XmlReader inner = xml.ReadSubtree())
                        {
                            while (inner.ReadToFollowing("server"))
                            {
                                inner.MoveToFirstAttribute();
                                string server = inner.Value;
                                if (!serverList.ContainsKey(server.ToLower()))
                                {
                                    serverList.Add(server.ToLower(), server);
                                    update = true;
                                }
                            }
                        }

                        xml.ReadToFollowing("leagues");
                        using (XmlReader inner = xml.ReadSubtree())
                        {
                            while (inner.ReadToFollowing("league"))
                            {
                                inner.MoveToFirstAttribute();
                                string name = inner.Value;
                                inner.MoveToNextAttribute();
                                leagues.Add(inner.Value, name);
                            }
                        }

                        xml.ReadToFollowing("news");
                        using (XmlReader inner = xml.ReadSubtree())
                        {
                            while (inner.ReadToFollowing("bbnews"))
                            {
                                Dictionary<string, string> newsthings = new Dictionary<string, string>();
                                inner.MoveToFirstAttribute();
                                newsthings.Add(inner.Name, inner.Value);
                                while (inner.MoveToNextAttribute())
                                    newsthings.Add(inner.Name, inner.Value);

                                newsList.Add(newsthings);
                            }
                        }

                        xml.ReadToFollowing("version");
                        xml.MoveToFirstAttribute();
                        latestVersion = xml.Value;
                    }

                    if (update)
                    {
                        Properties.Settings.Default.ServerAddresses = serverList.Serialize();
                        Properties.Settings.Default.Save();
                    }
                }

                loadSettingsCTS.Token.ThrowIfCancellationRequested();
            }, loadSettingsCTS.Token)
            .ContinueWith((t) =>
            {
                if (loadSettings.IsCanceled || loadSettingsCTS.Token.IsCancellationRequested)
                {
                    this.Close();
                    return;
                }

                if (!spamAllowed)
                {
                    ErrorLog.Log(loadSettings.Exception);
                    MessageBox.Show(this, "Failed to load the common settings!" + Environment.NewLine + "You will not be able to spam for league games (but you can still look for them). If this problem doesn't get away then the snooper may need to be updated.", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (loadSettings.IsFaulted)
                {
                    ErrorLog.Log(loadSettings.Exception);
                    return;
                }
                else if (Math.Sign(App.GetVersion().CompareTo(latestVersion)) == -1) // we need update only if it is newer than this version
                {
                    MessageBoxResult result = MessageBox.Show(this, "There is a new update available for Great Snooper!" + Environment.NewLine + "Would you like to download it now?", "New version available", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process p = new System.Diagnostics.Process();
                            if (Environment.OSVersion.Version.Major >= 6) // Vista or higher (to get admin rights).. on xp this causes fail!
                            {
                                p.StartInfo.UseShellExecute = true;
                                p.StartInfo.Verb = "runas";
                            }
                            p.StartInfo.FileName = "Updater2.exe";
                            p.Start();
                            snooperClosing = true;
                            this.Close();
                            return;
                        }
                        catch (Exception ex)
                        {
                            ErrorLog.Log(ex);
                            MessageBox.Show(this, "Failed to start the updater! Please try to run it manually from the installation directory of Great Snooper!", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }

                bool open = false;
                bool first = true;
                foreach (Dictionary<string, string> item in newsList)
                {
                    try
                    {
                        if (item["show"] == "1")
                        {
                            if (first)
                            {
                                if (!newsSeen.ContainsKey(item["id"]))
                                    open = true;
                                first = false;
                            }

                            if (newsSeen.ContainsKey(item["id"]))
                                newsSeen[item["id"]] = true;
                        }
                    }
                    catch (Exception) { }
                }

                List<string> toRemove = new List<string>();
                foreach (var item in newsSeen)
                {
                    if (!item.Value)
                        toRemove.Add(item.Key);
                }
                for (int i = 0; i < toRemove.Count; i++)
                    newsSeen.Remove(toRemove[i]);

                if (open)
                    OpenNewsWindow();

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        #region Keyboard events
        public void NextChannel(object sender, ExecutedRoutedEventArgs e)
        {
            if (Channels.Items.Count > 0)
            {
                if (Channels.SelectedIndex + 1 < Channels.Items.Count)
                    Channels.SelectedIndex = Channels.SelectedIndex + 1;
                else
                    Channels.SelectedIndex = 0;
            }
            e.Handled = true;
        }

        public void PrevChannel(object sender, ExecutedRoutedEventArgs e)
        {
            if (Channels.Items.Count > 0)
            {
                if (Channels.SelectedIndex - 1 > -1)
                    Channels.SelectedIndex = Channels.SelectedIndex - 1;
                else
                    Channels.SelectedIndex = Channels.Items.Count - 1;
            }
            e.Handled = true;
        }

        private void CloseChannel(object sender, ExecutedRoutedEventArgs e)
        {
            if (Channels.SelectedItem != null)
            {
                Channel ch = (Channel)((TabItem)Channels.SelectedItem).DataContext;
                if (ch.IsPrivMsgChannel)
                    CloseChannelTab(ch);
            }
            e.Handled = true;
        }

        private void FilterShortcut(object sender, ExecutedRoutedEventArgs e)
        {
            Filter.Focus();
            e.Handled = true;
        }

        public void CanExecuteCustomCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }
        #endregion

        #region Timers
        private void SetFocusTextBox(object sender, EventArgs e)
        {
            focusTimer.Stop();
            if (Channels.SelectedItem != null)
            {
                Channel ch = (Channel)((TabItem)Channels.SelectedItem).DataContext;
                ch.TheTextBox.Focus();
            }
        }

        private void ClockTick()
        {
            // Game list refresh
            if (gameListChannel != null && gameListChannel.Joined && gameListChannel.CanHost)
            {
                gameListCounter++;

                if (!snooperClosing && (GameListForce || gameListCounter >= 10) && DateTime.Now >= gameListChannel.GameListUpdatedTime.AddSeconds(3) && (WormWebC.LoadGamesTask == null || WormWebC.LoadGamesTask.IsCompleted))
                {
                    WormWebC.GetGamesOfChannel(gameListChannel);

                    GameListForce = false;
                    gameListCounter = 0;
                }
            }
            else if (gameListCounter != 0)
                gameListCounter = 0;

            // Get online tus users
            if (gameListChannel != null && gameListChannel.Joined && !gameListChannel.IsPrivMsgChannel)
            {
                tusRequestCounter++;
                if (!snooperClosing && (TusForce || tusRequestCounter >= 20) && (tusTask == null || tusTask.IsCompleted))
                {
                    tusTask = StartTusCommunication().ContinueWith((t) => TUSLoaded(t.Result), TaskScheduler.FromCurrentSynchronizationContext());
                    tusRequestCounter = 0;
                    TusForce = false;
                }
            }
            else if (tusRequestCounter != 0)
                tusRequestCounter = 0;

            // Leagues search (spamming)
            if (SearchHere != null && spamText != string.Empty)
            {
                if (!SearchHere.Joined) // reset
                {
                    ClearSpamming();
                }
                else
                {
                    searchCounter++;
                    if (searchCounter >= 90)
                    {
                        SendMessageToChannel(spamText, SearchHere);
                        searchCounter = 0;

                        spamCounter++;
                        if (spamCounter >= 10)
                        {
                            SearchHere.AddMessage(GlobalManager.SystemClient, "Great Snooper stopped spamming and searching for league game(s)!", MessageSettings.OfflineMessage);
                            ClearSpamming();

                            if (Properties.Settings.Default.LeagueFailBeepEnabled)
                                this.PlaySound("LeagueFailBeep");
                        }
                    }
                }
            }
        }

        private void ClearSpamming()
        {
            searchCounter = 100;
            spamCounter = 0;
            SearchHere = null;
            spamText = string.Empty;
            FoundUsers.Clear();
        }

        // All the tasks arrive from an IRC server will be processed here
        bool oddTurn = true;
        void timer_Tick(object sender, EventArgs e)
        {
            oddTurn = !oddTurn;

            if (oddTurn)
            {
                ClockTick();
                if (gameProcess != null)
                    GameProcess();
            }

            UITask task;
            while (GlobalManager.UITasks.TryDequeue(out task))
                task.DoTask(this);
        }
        #endregion

        #region Channel things


        // Leave a channel
        private void LeaveChannel(object sender, RoutedEventArgs e)
        {
            if (gameListChannel != null && gameListChannel.Joined)
            {
                gameListChannel.Part();
            }
            e.Handled = true;
        }


        Channel oldChannel;
        // If we changed our channel save the actual channel
        private void ChannelChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            focusTimer.Stop();

            if (oldChannel != null && oldChannel.IsPrivMsgChannel)
                oldChannel.GenerateHeader();

            if (Channels.SelectedItem == null)
            {
                oldChannel = null;
                return;
            }

            Channel ch = (Channel)((TabItem)Channels.SelectedItem).DataContext;
            oldChannel = ch;

            ch.BeepSoundPlay = true;
            ch.NewMessages = false;
            ch.UserMessageLoadedIdx = -1;

            visitedChannels.Remove(Channels.SelectedIndex);
            visitedChannels.Add(Channels.SelectedIndex);


            if (!ch.IsPrivMsgChannel)
            {
                GameList.SelectedIndex = Channels.SelectedIndex;
                UserList.SelectedIndex = Channels.SelectedIndex;

                // Clear filter
                if (gameListChannel != null)
                    SetDefaultViewForChannel(gameListChannel);

                gameListChannel = ch;
                GameListForce = true;
            }

            if (ch.Joined)
            {
                focusTimer.Start();
            }
        }

        public void SetDefaultViewForChannel(Channel ch)
        {
            if (ch.TheDataGrid != null)
            {
                var view = CollectionViewSource.GetDefaultView(ch.TheDataGrid.ItemsSource);
                if (view != null && view.Filter != null)
                {
                    Filter.Text = "Filter..";
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
        #endregion

        // Buddy and Ignore things
        public void AddOrRemoveBuddy(object sender, RoutedEventArgs e)
        {
            var obj = sender as MenuItem;
            var contextMenu = obj.Parent as ContextMenu;
            var item = contextMenu.PlacementTarget as DataGrid;
            if (item.SelectedIndex != -1)
            {
                var client = item.SelectedItem as Client;
                if (client.IsBuddy)
                    RemoveBuddy(client.Name);
                else
                    AddBuddy(client.Name);
            }
            e.Handled = true;
        }

        public void AddOrRemoveBan(object sender, RoutedEventArgs e)
        {
            var obj = sender as MenuItem;
            var contextMenu = obj.Parent as ContextMenu;
            var dg = contextMenu.PlacementTarget as DataGrid;
            if (dg.SelectedIndex != -1)
            {
                var client = dg.SelectedItem as Client;
                if (client.IsBanned)
                    RemoveBan(client.Name);
                else
                    AddBan(client.Name);

                // Reload channel messages
                if (!Properties.Settings.Default.ShowBannedMessages)
                {
                    for (int i = 0; i < Servers.Count; i++)
                    {
                        if (Servers[i].IsRunning && Servers[i].Clients.ContainsKey(client.LowerName))
                        {
                            foreach (var item in Servers[i].ChannelList)
                            {
                                if (!item.Value.IsPrivMsgChannel && item.Value.Joined && item.Value.Clients.Contains(client))
                                    LoadMessages(item.Value, GlobalManager.MaxMessagesDisplayed, true);
                            }
                        }
                    }
                }
            }
            e.Handled = true;
        }

        public void ContextMenuBuilding(object sender, ContextMenuEventArgs e)
        {
            var obj = sender as DataGrid;
            if (obj.SelectedItem != null)
            {
                var client = obj.SelectedItem as Client;
                Channel ch = (Channel)((TabItem)Channels.SelectedItem).DataContext;

                MenuItem chat = (MenuItem)obj.ContextMenu.Items[0];
                if (client.LowerName != ch.Server.User.LowerName)
                {
                    chat.Tag = client;
                    chat.IsEnabled = true;
                }
                else
                    chat.IsEnabled = false;

                MenuItem conversation = (MenuItem)obj.ContextMenu.Items[1];
                conversation.Header = "Add to conversation";
                if (client.CanConversation() && client.LowerName != ch.Server.User.LowerName)
                {
                    if (ch.IsPrivMsgChannel && ch.Clients[0].CanConversation())
                    {
                        conversation.Tag = new object[] { client, ch };
                        if (ch.IsInConversation(client))
                        {
                            conversation.Header = "Remove from conversation";
                            if (ch.Clients.Count == 1 && ch.Clients[0] == client)
                                conversation.IsEnabled = false;
                            else
                                conversation.IsEnabled = true;
                        }
                        else
                        {
                            conversation.IsEnabled = true;
                        }
                    }
                    else
                        conversation.IsEnabled = false;
                }
                else
                    conversation.IsEnabled = false;

                MenuItem buddy = (MenuItem)obj.ContextMenu.Items[2];
                if (client.IsBuddy)
                    buddy.Header = "Remove from buddy list";
                else
                    buddy.Header = "Add to buddy list";

                MenuItem ignore = (MenuItem)obj.ContextMenu.Items[3];
                if (client.IsBanned)
                    ignore.Header = "Remove from ignore list";
                else
                    ignore.Header = "Add to ignore list";

                MenuItem tusInfo = (MenuItem)obj.ContextMenu.Items[4];
                if (client.TusActive)
                {
                    tusInfo.Tag = client;
                    tusInfo.Header = "View " + client.TusNick + "'s profile";
                    tusInfo.Visibility = System.Windows.Visibility.Visible;
                }
                else
                    tusInfo.Visibility = System.Windows.Visibility.Collapsed;

                MenuItem appinfo = (MenuItem)obj.ContextMenu.Items[5];
                appinfo.Header = "Info: " + client.ClientApp;
            }
        }

        #region Private chat UI things (open, close)


        public void OpenPrivateChat(Client client, IRCCommunicator server)
        {
            if (client.IsBanned)
                return;

            // Test if we already have an opened chat with the user
            for (int i = 0; i < Channels.Items.Count; i++)
            {
                Channel temp = (Channel)((TabItem)Channels.Items[i]).DataContext;
                if (temp.HashName == client.Name && temp.Server == server)
                {
                    Channels.SelectedIndex = i;
                    return;
                }
            }

            // Make new channel
            Channel ch = new Channel(this, server, client.Name, "Chat with " + client.Name, client);

            // Select it
            for (int i = 0; i < Channels.Items.Count; i++)
            {
                Channel temp = (Channel)((TabItem)Channels.Items[i]).DataContext;
                if (temp.HashName == client.Name && temp.Server == server)
                {
                    Channels.SelectedIndex = i;
                    return;
                }
            }
        }

        // Close private chat
        private void ChannelMBClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
            {
                e.Handled = true;
                Channel ch = (Channel)((Border)sender).DataContext;
                if (ch.IsPrivMsgChannel)
                    CloseChannelTab(ch);
            }
        }

        // Close private chat
        private void PrivateChatClose(object sender, RoutedEventArgs e)
        {
            var obj = (MenuItem)sender;
            Channel ch = (Channel)(obj.DataContext);
            if (ch.IsPrivMsgChannel)
                CloseChannelTab(ch);
            e.Handled = true;
        }

        public void CloseChannelTab(Channel ch, bool hideOnly = false)
        {
            if (Channels.SelectedItem != null && (Channel)((TabItem)Channels.SelectedItem).DataContext == ch) // Channel is selected
            {
                int index = Channels.SelectedIndex;
                visitedChannels.Remove(index);
                for (int i = 0; i < visitedChannels.Count; i++)
                {
                    if (visitedChannels[i] > index)
                        visitedChannels[i]--;
                }
                int lastindex = visitedChannels[visitedChannels.Count - 1];

                if (!hideOnly)
                {
                    if (ch.Joined)
                        ch.Part();
                    ch.Server.ChannelList.Remove(ch.HashName);
                }
                Channels.Items.RemoveAt(index);
                Channels.SelectedIndex = lastindex;
            }
            else
            {
                int index = -1;
                for (int i = 0; i < Channels.Items.Count; i++)
                {
                    if ((Channel)((TabItem)Channels.Items[i]).DataContext == ch)
                    {
                        index = i;
                        break;
                    }
                }
                visitedChannels.Remove(index);
                for (int i = 0; i < visitedChannels.Count; i++)
                {
                    if (visitedChannels[i] > index)
                        visitedChannels[i]--;
                }

                if (!hideOnly)
                {
                    if (ch.Joined)
                        ch.Part();
                    ch.Server.ChannelList.Remove(ch.HashName);
                }
                Channels.Items.RemoveAt(index);
            }
        }
        #endregion

        #region Filter things
        private void FilterEntered(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Filter.Text == "Filter..")
                Filter.Text = "";
        }

        private void FilterLeft(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Filter.Text.Trim() == string.Empty)
            {
                Filter.Text = "Filter..";
            }
        }

        private void Filtering(object sender, KeyEventArgs e)
        {
            filterTimer.Stop();
            filterTimer.Start();
        }

        void filterTimer_Tick(object sender, EventArgs e)
        {
            filterTimer.Stop();
            if (gameListChannel != null && gameListChannel.Joined)
            {
                var view = CollectionViewSource.GetDefaultView(gameListChannel.TheDataGrid.ItemsSource);
                if (view != null)
                {
                    string[] filtersTemp = Filter.Text.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> filters = new List<string>();
                    for (int i = 0; i < filtersTemp.Length; i++)
                    {
                        string temp = filtersTemp[i].Trim();
                        if (temp.Length >= 2)
                            filters.Add(temp.ToLower());
                    }

                    if (filters.Count == 0)
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
                    else
                    {
                        view.Filter = o =>
                        {
                            Client c = o as Client;
                            if (!Properties.Settings.Default.ShowBannedUsers && c.IsBanned)
                                return false;

                            for (int i = 0; i < filters.Count; i++)
                            {
                                if (
                                    c.LowerName.Contains(filters[i])
                                    || c.TusActive && c.TusLowerNick.Contains(filters[i])
                                    || c.Clan.Length >= filters[i].Length && c.Clan.Substring(0, filters[i].Length).ToLower() == filters[i]
                                    || c.Country != null && c.Country.LowerName.Length >= filters[i].Length && c.Country.LowerName.Substring(0, filters[i].Length) == filters[i]
                                    || c.Rank != null && c.Rank.LowerName.Length >= filters[i].Length && c.Rank.LowerName.Substring(0, filters[i].Length) == filters[i]
                                    || Properties.Settings.Default.ShowInfoColumn && c.ClientAppL.Contains(filters[i])
                                    || c.IsBuddy && "buddy".Length >= filters[i].Length && "buddy".Substring(0, filters[i].Length) == filters[i]
                                    || c.IsBanned && "ignored".Length >= filters[i].Length && "ignored".Substring(0, filters[i].Length) == filters[i]
                                )
                                    return true;
                            }
                            return false;
                        };
                    }
                }
            }
        }
        #endregion

        #region Other things to increase user experience
        // Need to know that if the window is activated to play beep sounds
        private void WindowActivated(object sender, EventArgs e)
        {
            IsWindowFocused = true;
            if (Channels.SelectedItem != null)
            {
                Channel ch = (Channel)((TabItem)Channels.SelectedItem).DataContext;
                if (ch.Joined)
                {
                    ch.BeepSoundPlay = true;
                    ch.NewMessages = false;
                    ch.TheTextBox.Focus();
                }
            }

            if (Properties.Settings.Default.TrayFlashing)
                this.StopFlashingWindow();
        }

        // Need to know that if the window is activated to play beep sounds
        private void WindowDeactivated(object sender, EventArgs e)
        {
            IsWindowFocused = false;
        }

        private void GameList_LostFocus(object sender, RoutedEventArgs e)
        {
            var obj = sender as ListBox;
            obj.SelectedIndex = -1;
            e.Handled = true;
        }

        #endregion

        #region Closing things
        // If we want to quit, send a message to stop the IRCThread
        private void My_WormNet_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!snooperClosing && Properties.Settings.Default.CloseToTray)
            {
                HideWindow();
                if (Properties.Settings.Default.TrayNotifications)
                    myNotifyIcon.ShowBalloonTip(null, "Great Snooper is still running here.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                e.Cancel = true;
                return;
            }

            snooperClosing = true;

            bool taskRunning = false;

            // Stop the clock
            timer.Stop();

            if (loadSettings.Status == TaskStatus.WaitingForActivation)
            {
                loadSettingsCTS.Cancel();
                taskRunning = true;
            }

            if (gameProcess != null)
            {
                gameProcess.Dispose();
                gameProcess = null;
            }

            if (tusTask != null && tusTask.Status == TaskStatus.WaitingForActivation)
            {
                TusCTS.Cancel();
                taskRunning = true;
            }

            if (WormWebC.LoadGamesTask != null && WormWebC.LoadGamesTask.Status == TaskStatus.WaitingForActivation)
            {
                WormWebC.LoadGamesCTS.Cancel();
                taskRunning = true;
            }

            // Stop servers
            for (int i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].IsRunning)
                {
                    Servers[i].Cancel = true;
                    taskRunning = true;
                }
            }

            if (taskRunning)
            {
                e.Cancel = true;
                return;
            }
            
            // Log channel messages
            for (int i = 0; i < Servers.Count; i++)
            {
                foreach (var item in Servers[i].ChannelList)
                {
                    if (item.Value.Joined)
                    {
                        if (Servers[i].IsWormNet)
                        {
                            if (Properties.Settings.Default.QuitMessagee.Length > 0)
                                item.Value.AddMessage(item.Value.Server.User, "has left WormNet (" + Properties.Settings.Default.QuitMessagee + ").", MessageSettings.QuitMessage);
                            else
                                item.Value.AddMessage(item.Value.Server.User, "has left WormNet.", MessageSettings.QuitMessage);
                        }
                        else
                        {
                            if (Properties.Settings.Default.QuitMessagee.Length > 0)
                                item.Value.AddMessage(item.Value.Server.User, "has left the server (" + Properties.Settings.Default.QuitMessagee + ").", MessageSettings.QuitMessage);
                            else
                                item.Value.AddMessage(item.Value.Server.User, "has left the server.", MessageSettings.QuitMessage);
                        }
                        item.Value.Log(item.Value.Messages.Count, true);
                    }
                }
            }

            // Serialize buddy list and ban list and save them
            Properties.Settings.Default.BuddyList = this.buddyList.Serialize();
            Properties.Settings.Default.BanList = this.banList.Serialize();

            if (Properties.Settings.Default.SaveInstantColors)
            {
                // Serialize instant colors
                var sb = new System.Text.StringBuilder();
                foreach (var item in ChoosedColors)
                {
                    sb.Append(item.Key);
                    sb.Append(":");
                    sb.Append(string.Format("{0:X2}{1:X2}{2:X2}", item.Value.Color.R, item.Value.Color.G, item.Value.Color.B));
                    sb.Append(',');
                }
                Properties.Settings.Default.InstantColors = sb.ToString();
            }
            Properties.Settings.Default.Save();

            myNotifyIcon.Dispose();
            myNotifyIcon = null;
        }

        // IRCThread will notify us when it is done. Then we close the window
        private void ConnectionState(object sender, ConnectionStateEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                IRCCommunicator server = (IRCCommunicator)sender;

                if (snooperClosing)
                {
                    if (server.IsRunning)
                        server.Cancel = true;
                    else
                        this.Close();
                }
                else if (e.State == IRCCommunicator.ConnectionStates.Connected)
                {
                    bool reconnecting = false;
                    foreach (var item in server.ChannelList)
                    {
                        if (item.Value.IsReconnecting)
                        {
                            reconnecting = true;
                            break;
                        }
                    }

                    if (reconnecting || server.IsWormNet)
                    {
                        foreach (var item in server.ChannelList)
                        {
                            item.Value.Reconnecting(false);
                            item.Value.AddMessage(GlobalManager.SystemClient, "Great Snooper has reconnected.", MessageSettings.OfflineMessage);

                            if (item.Value.Joined && !item.Value.IsPrivMsgChannel)
                            {
                                server.JoinChannel(item.Value.Name);
                                server.GetChannelClients(item.Value.Name);
                            }
                        }
                    }
                    else if (GameSurgeIsConnected)
                    {
                        server.JoinChannel("#worms");
                    }
                }
                /*
                else if (state == IRCCommunicator.ConnectionStates.AuthOK && gameSurgeIsConnected)
                {
                    sender.JoinChannel("#worms");
                }
                else if (state == IRCCommunicator.ConnectionStates.AuthBad)
                {
                    sender.ChannelList["#worms"].Part();
                    gameSurgeIsConnected = false;
                    sender.CancelAsync();
                }
                */
                else if (e.State != IRCCommunicator.ConnectionStates.Disconnected)
                {
                    if (!server.IsWormNet && e.State == IRCCommunicator.ConnectionStates.UsernameInUse)
                    {
                        bool reconnecting = false;
                        foreach (var item in server.ChannelList)
                        {
                            if (item.Value.Joined)
                            {
                                reconnecting = true;
                                break;
                            }
                        }

                        if (!reconnecting)
                        {
                            foreach (var item in server.ChannelList)
                            {
                                item.Value.Loading(false);
                            }

                            MessageBox.Show("Your nickname is already used by somebody on this server. You can choose another nickname in Settings.", "Nickname is in use", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }

                    // Reconnect needed
                    foreach (var item in server.ChannelList)
                    {
                        item.Value.Reconnecting(true);
                    }
                    server.Reconnect();
                }
            }
            ));
        }
        #endregion

        // Important, because it will fire also Channels.SelectionChanged if this one is missing!
        private void NoSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
        }

        private void OpenURL(object sender, RoutedEventArgs e)
        {
            try
            {
                var obj = (MenuItem)sender;
                System.Diagnostics.Process.Start((string)obj.Tag);
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
            e.Handled = true;
        }

        private void WiewTusProfile(object sender, RoutedEventArgs e)
        {
            Client client = (Client)((MenuItem)sender).Tag;

            try
            {
                System.Diagnostics.Process.Start(client.TusLink);
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
            e.Handled = true;
        }

        // Buddy list things
        #region Buddy list
        public void AddBuddy(string name)
        {
            string lowerName = name.ToLower();
            buddyList.Add(lowerName, name);

            for (int i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].IsRunning)
                {
                    Client c;
                    if (Servers[i].Clients.TryGetValue(lowerName, out c))
                    {
                        c.IsBuddy = true;

                        // Refresh sorting
                        foreach (Channel ch in c.Channels)
                        {
                            ch.Clients.Remove(c);
                            ch.Clients.Add(c);
                        }
                    }
                }
            }
        }

        public void RemoveBuddy(string name)
        {
            string lowerName = name.ToLower();
            buddyList.Remove(lowerName);

            for (int i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].IsRunning)
                {
                    Client c;
                    if (Servers[i].Clients.TryGetValue(lowerName, out c))
                    {
                        c.IsBuddy = false;

                        // Refresh sorting
                        foreach (Channel ch in c.Channels)
                        {
                            ch.Clients.Remove(c);
                            ch.Clients.Add(c);
                        }
                    }
                }
            }
        }

        public bool IsBuddy(string name)
        {
            return buddyList.ContainsKey(name);
        }
        #endregion



        // Ban list things
        #region Ban list
        public void AddBan(string name)
        {
            string lowerName = name.ToLower();
            banList.Add(lowerName, name);

            for (int i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].IsRunning)
                {
                    Client c;
                    if (Servers[i].Clients.TryGetValue(lowerName, out c))
                    {
                        c.IsBanned = true;

                        // Refresh sorting
                        foreach (Channel ch in c.Channels)
                        {
                            ch.Clients.Remove(c);
                            ch.Clients.Add(c);
                        }
                    }
                }
            }
        }

        public void RemoveBan(string name)
        {
            string lowerName = name.ToLower();
            banList.Remove(lowerName);

            for (int i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].IsRunning)
                {
                    Client c;
                    if (Servers[i].Clients.TryGetValue(lowerName, out c))
                    {
                        c.IsBanned = false;

                        // Refresh sorting
                        foreach (Channel ch in c.Channels)
                        {
                            ch.Clients.Remove(c);
                            ch.Clients.Add(c);
                        }
                    }
                }
            }
        }

        public bool IsBanned(string name)
        {
            return banList.ContainsKey(name);
        }
        #endregion

        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            this.snooperClosing = true;
            this.Close();
            e.Handled = true;
        }

        private void LogoutClicked(object sender, RoutedEventArgs e)
        {
            new Login().Show();
            this.snooperClosing = true;
            this.Close();
            e.Handled = true;
        }

        private void NotifyIconDoubleClick(object sender, ExecutedRoutedEventArgs e)
        {
            RestoreWindow();
            e.Handled = true;
        }

        private void ShowSnooper(object sender, RoutedEventArgs e)
        {
            RestoreWindow();
            e.Handled = true;
        }

        private void HideWindow()
        {
            if (Properties.Settings.Default.EnergySaveMode && !EnergySaveModeOn)
                EnterEnergySaveMode();

            if (this.WindowState != System.Windows.WindowState.Minimized)
                lastWindowState = this.WindowState;
            this.Hide();
        }

        private void RestoreWindow()
        {
            if (Properties.Settings.Default.EnergySaveMode && EnergySaveModeOn)
                LeaveEnergySaveMode();

            this.Show();
            if (this.WindowState == System.Windows.WindowState.Minimized)
                this.WindowState = lastWindowState;
            this.Activate();
        }

        private void LayoutChanged(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Properties.Settings.Default.GameListGridRowStarts = Convert.ToInt32((GameListGridRow.ActualHeight / ChannelsGridRow.ActualHeight) * 100);
            Properties.Settings.Default.Save();
        }

        private void LayoutChanged2(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Properties.Settings.Default.RightColumnStars = Convert.ToInt32((RightColumn.ActualWidth / LeftColumn.ActualWidth) * 100);
            Properties.Settings.Default.Save();
        }

        private void SetClientListDGColumns(DataGrid dg = null)
        {
            string[] settings;
            settings = Properties.Settings.Default.ClientListDGColumns.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (settings.Length == 0)
                return;

            if (dg == null)
            {
                for (int j = 0; j < Servers.Count; j++)
                {
                    foreach (var item in Servers[j].ChannelList)
                    {
                        if (!item.Value.IsPrivMsgChannel && item.Value.TheDataGrid != null)
                        {
                            SetClientListDGColumnsForDG(item.Value.TheDataGrid, settings);
                        }
                    }
                }
            }
            else
            {
                SetClientListDGColumnsForDG(dg, settings);
            }
        }

        private void SetClientListDGColumnsForDG(DataGrid dg, string[] settings)
        {
            int i = 0;
            foreach (var column in dg.Columns)
            {
                if (i < 2)
                {
                    column.Width = new DataGridLength(Convert.ToInt32(settings[i++]), DataGridLengthUnitType.Pixel);
                }
                else
                {
                    column.Width = new DataGridLength(Convert.ToInt32(settings[i++]), DataGridLengthUnitType.Star);
                }
            }
        }

        public void PlaySound(string index)
        {
            SoundPlayer sp;
            if (soundEnabled && soundPlayers.TryGetValue(index, out sp))
            {
                try
                {
                    sp.Play();
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }
            }
        }
    }
}
