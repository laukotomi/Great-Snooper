using Hardcodet.Wpf.TaskbarNotification;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;


namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        private readonly string ServerAddress;

        // Lists
        private readonly Dictionary<string, string> leagues = new Dictionary<string,string>();
        public readonly List<IRCCommunicator> Servers = new List<IRCCommunicator>(2);
        private readonly List<Dictionary<string, string>> newsList = new List<Dictionary<string,string>>();
        private readonly List<int> visitedChannels = new List<int>();
        private readonly Dictionary<string, bool> newsSeen = new Dictionary<string,bool>();
        public readonly Dictionary<string, List<string>> FoundUsers = new Dictionary<string, List<string>>();
        public readonly List<NotificatorClass> Notifications = new List<NotificatorClass>();

        // Tasks
        private Task tusTask;
        private Task loadSettingsTask;
        private Task loadGamesTask;
        private readonly CancellationTokenSource tusCTS = new CancellationTokenSource();
        private readonly CancellationTokenSource loadSettingsCTS = new CancellationTokenSource();
        private readonly CancellationTokenSource loadGamesCTS = new CancellationTokenSource();

        // Helpers
        private Channel gameListChannel;
        private bool snooperClosing = false;
        public bool GameSurgeIsConnected = false;
        public bool EnergySaveModeOn = false;
        private bool IsHidden = false;
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
                // 
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

        bool mute = true;

        // Constructor        
        public MainWindow() { } // Never used, but visual stdio throws an error if not exists
        public MainWindow(IRCCommunicator WormNetC, string serverAddress)
        {
            InitializeComponent();
            GlobalManager.MainWindowInit();

            GameListGridRow.Height = new GridLength(Properties.Settings.Default.GameListGridRowStarts, GridUnitType.Star);
            RightColumn.Width = new GridLength(Properties.Settings.Default.RightColumnStars, GridUnitType.Star);
            //System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            this.DataContext = this;
            this.ServerAddress = serverAddress;
            this.WelcomeText.Text = "Welcome " + GlobalManager.User.Name + "!";

            // Servers
            WormNetC.ConnectionState += ConnectionState;
            Servers.Add(WormNetC);

            IRCCommunicator gameSurge = new IRCCommunicator("irc.gamesurge.net", 6667, false);
            gameSurge.ConnectionState += ConnectionState;
            Servers.Add(gameSurge);

            // Focustimer will focus to the textbox of a channel when we change channel
            focusTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            focusTimer.Tick += SetFocusTextBox;

            filterTimer.Interval = new TimeSpan(0, 0, 0, 0, 300);
            filterTimer.Tick += filterTimer_Tick;

            // Unserialize newsseen
            string[] list = Properties.Settings.Default.NewsSeen.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < list.Length; i++)
                newsSeen.Add(list[i], false);

            // Initialize a timer which will help updating things that should be updated periodically (game list, news..)
            // Initialize a timer which will process data that is sent to the UI thread from the irc threads
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            timer.Tick += timer_Tick;
            timer.Start();

            // Get channels
            Channels.Items.Clear();
            WormNetC.GetChannelList();

            this.StateChanged += MainWindow_StateChanged;
            this.LocationChanged += MainWindow_LocationChanged;

            // Hehehe
            if (GlobalManager.User.LowerName.Contains("guuuria") || GlobalManager.User.LowerName.Contains("guuria"))
            {
                var picture = new BitmapImage();
                picture.DecodePixelWidth = 35;
                picture.DecodePixelHeight = 35;
                picture.CacheOption = BitmapCacheOption.OnLoad;
                picture.BeginInit();
                picture.UriSource = new Uri("pack://application:,,,/Resources/batlogo.ico");
                picture.EndInit();
                picture.Freeze();

                HeaderIcon.Width = 35;
                HeaderIcon.Source = picture;
            }
        }

        void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            snoopIsInOtherWindow = false;
        }

        void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.EnergySaveModeWin)
            {
                if (this.WindowState == System.Windows.WindowState.Minimized && !EnergySaveModeOn)
                    EnterEnergySaveMode();
                else if (EnergySaveModeOn)
                    LeaveEnergySaveMode();
            }
        }

        private bool snoopIsInOtherWindow = false;
        private void EnterEnergySaveMode(bool checkOtherWindowThanGame = false)
        {
            if (checkOtherWindowThanGame)
            {
                var gsScreen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
                var gameScreen = System.Windows.Forms.Screen.FromHandle(lobbyWindow);
                if (gsScreen.DeviceName != gameScreen.DeviceName)
                {
                    snoopIsInOtherWindow = true;
                    return;
                }
            }
            Debug.WriteLine("EnergySaveMode is ON", "EnergySaveMode");

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
            if (this.IsHidden || this.WindowState == System.Windows.WindowState.Minimized)
                return;
            Debug.WriteLine("EnergySaveMode is OFF", "EnergySaveMode");

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
                            {
                                item.Value.TheDataGrid.ItemsSource = item.Value.Clients;
                                SetDefaultOrderForChannel(item.Value);
                            }
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
            mute = false;

            // Download news and league list
            LoadSettings();
        }

        private void LoadSettings()
        {
            string latestVersion = string.Empty;

            loadSettingsTask = Task.Factory.StartNew(() =>
            {
                string settingsXMLPath = GlobalManager.SettingsPath + @"\Settings.xml";

                try
                {
                    string settingsXMLPathTemp = GlobalManager.SettingsPath + @"\SettingsTemp.xml";

                    using (WebDownload webClient = new WebDownload() { Proxy = null })
                    {
                        webClient.DownloadFile("http://mediacreator.hu/SnooperSettings.xml", settingsXMLPathTemp);
                    }

                    // If downloading will fail then leagues won't be loaded. If they would, it could be hacked easily.
                    GlobalManager.SpamAllowed = true;

                    if (File.Exists(settingsXMLPath))
                        File.Delete(settingsXMLPath);

                    File.Move(settingsXMLPathTemp, settingsXMLPath);
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }

                loadSettingsCTS.Token.ThrowIfCancellationRequested();

                if (File.Exists(settingsXMLPath))
                {
                    Dictionary<string, string> serverList = new Dictionary<string, string>();
                    serverList.DeSerialize(Properties.Settings.Default.ServerAddresses);
                    bool update = false;

                    using (XmlReader xml = XmlReader.Create(settingsXMLPath))
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
                if (t.IsCanceled || loadSettingsCTS.Token.IsCancellationRequested)
                {
                    this.Close();
                    return;
                }

                if (!GlobalManager.SpamAllowed)
                {
                    if (t.IsFaulted)
                        ErrorLog.Log(t.Exception);
                    MessageBox.Show(this, "Failed to load the common settings!" + Environment.NewLine + "You will not be able to spam for league games (but you can still look for them). If this problem doesn't get away then the snooper may need to be updated.", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (t.IsFaulted)
                {
                    ErrorLog.Log(t.Exception);
                    return;
                }
                else if (Math.Sign(App.GetVersion().CompareTo(latestVersion)) == -1) // we need update only if it is newer than this version
                {
                    MessageBoxResult result = MessageBox.Show(this, "There is a new update available for Great Snooper!" + Environment.NewLine + "Would you like to download it now?", "New version available", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Process p = new Process();
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

                if (!snooperClosing && (GameListForce || gameListCounter >= 10) && DateTime.Now >= gameListChannel.GameListUpdatedTime.AddSeconds(3) && (loadGamesTask == null || loadGamesTask.IsCompleted))
                {
                    GetGamesOfChannel(gameListChannel);

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
                    tusTask = StartTusCommunication().ContinueWith(TUSLoaded, TaskScheduler.FromCurrentSynchronizationContext());
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
                                Sounds.PlaySound("LeagueFailBeep");
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
                if (Filter.Text != "Filter..")
                {
                    Filter.Text = "Filter..";
                    if (gameListChannel != null)
                        SetDefaultViewForChannel(gameListChannel);
                }

                gameListChannel = ch;
                GameListForce = true;
            }
            else
                ch.GenerateHeader();

            if (ch.Joined)
                focusTimer.Start();
        }

        public void SetDefaultViewForChannel(Channel ch)
        {
            if (ch.TheDataGrid != null)
            {
                var view = CollectionViewSource.GetDefaultView(ch.TheDataGrid.ItemsSource);
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
                    else if (view.Filter != null)
                        view.Filter = null;
                }
            }
        }

        public void SetDefaultOrderForChannel(Channel ch)
        {
            if (ch.Server.IsWormNet)
            {
                string[] order = Properties.Settings.Default.ColumnOrder.Split(new char[] { '|' });
                if (order.Length == 2)
                {
                    ListSortDirection dir = order[1] == "D" ? ListSortDirection.Descending : ListSortDirection.Ascending;
                    SetOrderForDataGrid(ch, order[0], dir);
                }
                else
                    SetOrderForDataGrid(ch, "Nick", ListSortDirection.Ascending);
            }
            else
            {
                if (ch.TheDataGrid.ItemsSource != null)
                {
                    var view = CollectionViewSource.GetDefaultView(ch.TheDataGrid.ItemsSource);
                    if (view != null)
                    {
                        view.SortDescriptions.Clear();
                        view.SortDescriptions.Add(new System.ComponentModel.SortDescription("IsBanned", System.ComponentModel.ListSortDirection.Ascending));
                        view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Group.ID", System.ComponentModel.ListSortDirection.Ascending));
                        view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", ListSortDirection.Ascending));
                    }
                }

            }
        }
        #endregion

        // Ignore things
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

        private bool groupsGenerated = false;

        public void ContextMenuBuilding(object sender, ContextMenuEventArgs e)
        {
            var obj = sender as DataGrid;

            if (obj.SelectedItem == null || Channels.SelectedItem == null)
            {
                e.Handled = true;
                return;
            }

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
                if (ch.IsPrivMsgChannel && ch.Clients.Count > 0 && ch.Clients[0].CanConversation())
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
                        conversation.IsEnabled = true;
                }
                else
                    conversation.IsEnabled = false;
            }
            else
                conversation.IsEnabled = false;

            MenuItem group = (MenuItem)obj.ContextMenu.Items[2];
            if (!groupsGenerated)
            {
                group.Items.Clear();
                var defItem = new MenuItem() { Header = "No group" };
                defItem.Click += RemoveUserFromGroup;
                group.Items.Add(defItem);

                foreach (var item in UserGroups.Groups)
                {
                    var menuItem = new MenuItem() { Header = item.Value.Name, Foreground = item.Value.TextColor, Tag = item.Value };
                    menuItem.Click += AddUserToGroup;
                    group.Items.Add(menuItem);
                }
                groupsGenerated = true;
            }

            foreach (MenuItem item in group.Items)
            {
                item.FontWeight = FontWeights.Normal;
            }

            if (client.Group.ID == UserGroups.SystemGroupID)
                ((MenuItem)group.Items[0]).FontWeight = FontWeights.Bold;
            else
                ((MenuItem)group.Items[client.Group.ID + 1]).FontWeight = FontWeights.Bold;

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

        private void RemoveUserFromGroup(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var contextMenu = (ContextMenu)((MenuItem)menuItem.Parent).Parent;
            var item = contextMenu.PlacementTarget as DataGrid;
            if (item.SelectedIndex != -1)
            {
                var client = item.SelectedItem as Client;
                UserGroups.AddOrRemoveUser(client, null);
                ChangeMessageColorForClient(client, null);
            }
        }

        private void AddUserToGroup(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var contextMenu = (ContextMenu)((MenuItem)menuItem.Parent).Parent;
            var item = contextMenu.PlacementTarget as DataGrid;
            if (item.SelectedIndex != -1)
            {
                var client = item.SelectedItem as Client;
                var group = (UserGroup)menuItem.Tag;
                UserGroups.AddOrRemoveUser(client, group);
                ChangeMessageColorForClient(client, group.TextColor);
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
                    if (Channels.SelectedIndex != i)
                        Channels.SelectedIndex = i;
                    else
                        temp.TheTextBox.Focus();
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

            if (gameListChannel == null || !gameListChannel.Joined)
                return;

            string[] filtersTemp = Filter.Text.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> filters = new List<string>();
            for (int i = 0; i < filtersTemp.Length; i++)
            {
                string temp = filtersTemp[i].Trim();
                if (temp.Length >= 2)
                    filters.Add(temp.ToLower());
            }

            if (filters.Count == 0)
                SetDefaultViewForChannel(gameListChannel);
            else if (gameListChannel.TheDataGrid != null)
            {
                var view = CollectionViewSource.GetDefaultView(gameListChannel.TheDataGrid.ItemsSource);
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
                        )
                            return true;
                    }
                    return false;
                };
            }
        }
        #endregion

        #region Other things to increase user experience
        // Need to know that if the window is activated to play beep sounds
        private void WindowActivated(object sender, EventArgs e)
        {
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

            if (loadSettingsTask != null && loadSettingsTask.Status == TaskStatus.WaitingForActivation)
            {
                loadSettingsCTS.Cancel();
                taskRunning = true;
            }

            if (tusTask != null && tusTask.Status == TaskStatus.WaitingForActivation)
            {
                tusCTS.Cancel();
                taskRunning = true;
            }

            if (loadGamesTask != null && loadGamesTask.Status == TaskStatus.WaitingForActivation)
            {
                loadGamesCTS.Cancel();
                taskRunning = true;
            }

            if (gameProcess != null)
            {
                gameProcess.Dispose();
                gameProcess = null;
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
                    if (item.Value.Joined && item.Value.Messages.Count > 0)
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
            Properties.Settings.Default.BanList = GlobalManager.BanList.Serialize();
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
                    if (!server.IsWormNet)
                    {
                        foreach (var item in server.ChannelList)
                        {
                            if (item.Value.IsReconnecting)
                            {
                                reconnecting = true;
                                break;
                            }
                        }
                    }

                    if (reconnecting || server.IsWormNet)
                    {
                        foreach (var item in server.ChannelList)
                        {
                            item.Value.Reconnecting(false);
                            if (item.Value.Joined)
                            {
                                item.Value.AddMessage(GlobalManager.SystemClient, "Great Snooper has reconnected.", MessageSettings.OfflineMessage);

                                if (!item.Value.IsPrivMsgChannel)
                                {
                                    server.JoinChannel(item.Value.Name);
                                    server.GetChannelClients(item.Value.Name);
                                }
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
                Process.Start(new ProcessStartInfo((string)obj.Tag));
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
                Process.Start(new ProcessStartInfo(client.TusLink));
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
            e.Handled = true;
        }

        // Ban list things
        #region Ban list
        private void AddBan(string name)
        {
            string lowerName = name.ToLower();
            GlobalManager.BanList.Add(lowerName, name);

            for (int i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].IsRunning)
                {
                    Client c;
                    if (Servers[i].Clients.TryGetValue(lowerName, out c))
                    {
                        c.IsBanned = true;

                        // Refresh sorting
                        var temp = new List<Channel>(c.Channels);

                        foreach (Channel ch in temp)
                        {
                            ch.Clients.Remove(c);
                            ch.Clients.Add(c);
                        }
                    }
                }
            }
        }

        private void RemoveBan(string name)
        {
            string lowerName = name.ToLower();
            GlobalManager.BanList.Remove(lowerName);

            for (int i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].IsRunning)
                {
                    Client c;
                    if (Servers[i].Clients.TryGetValue(lowerName, out c))
                    {
                        c.IsBanned = false;

                        // Refresh sorting
                        var temp = new List<Channel>(c.Channels);

                        foreach (Channel ch in temp)
                        {
                            ch.Clients.Remove(c);
                            ch.Clients.Add(c);
                        }
                    }
                }
            }
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
            if (Properties.Settings.Default.EnergySaveModeWin && !EnergySaveModeOn)
                EnterEnergySaveMode();

            if (this.WindowState != System.Windows.WindowState.Minimized)
                lastWindowState = this.WindowState;
            this.Hide();
            this.IsHidden = true;
        }

        private void RestoreWindow()
        {
            this.Show();
            if (this.WindowState == System.Windows.WindowState.Minimized)
                this.WindowState = lastWindowState;
            this.Activate();
            this.IsHidden = false;

            if (Properties.Settings.Default.EnergySaveModeWin && EnergySaveModeOn)
                LeaveEnergySaveMode();
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

        // <SCHEME=Pf,Be>
        private readonly Regex SchemeRegex = new Regex(@"^<SCHEME=([^>]+)>$", RegexOptions.IgnoreCase);
        private readonly byte[] schemeRecvBuffer = new byte[100];
        private readonly StringBuilder schemeRecvSB = new StringBuilder();

        public string SetChannelScheme(Channel channel)
        {
            try
            {
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + ServerAddress + "/wormageddonweb/RequestChannelScheme.asp?Channel=" + channel.Name.Substring(1));
                myHttpWebRequest.UserAgent = "T17Client/1.2";
                myHttpWebRequest.Proxy = null;
                myHttpWebRequest.AllowAutoRedirect = false;
                using (WebResponse myHttpWebResponse = myHttpWebRequest.GetResponse())
                using (System.IO.Stream stream = myHttpWebResponse.GetResponseStream())
                {
                    int bytes;
                    schemeRecvSB.Clear();
                    while ((bytes = stream.Read(schemeRecvBuffer, 0, schemeRecvBuffer.Length)) > 0)
                    {
                        for (int j = 0; j < bytes; j++)
                        {
                            schemeRecvSB.Append(WormNetCharTable.Decode[schemeRecvBuffer[j]]);
                        }
                    }

                    // <SCHEME=Pf,Be>
                    Match m = SchemeRegex.Match(schemeRecvSB.ToString());
                    if (m.Success)
                        return m.Groups[1].Value;
                }
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }

            MessageBox.Show("Failed to load the scheme of the channel!", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
            return string.Empty;
        }

        private readonly Regex GameRegex = new Regex(@"^<GAME\s(\S*)\s(\S+)\s(\S+)\s(\S+)\s1\s(\S+)\s(\S+)\s([^>]+)>$", RegexOptions.IgnoreCase);
        private readonly byte[] gameRecvBuffer = new byte[10240];
        private readonly StringBuilder gameRecvSB = new StringBuilder(10240);

        private void GetGamesOfChannel(Channel channel)
        {
            loadGamesTask = Task.Factory.StartNew(() =>
            {
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + ServerAddress + ":80/wormageddonweb/GameList.asp?Channel=" + channel.Name.Substring(1));
                myHttpWebRequest.UserAgent = "T17Client/1.2";
                myHttpWebRequest.Proxy = null;
                myHttpWebRequest.AllowAutoRedirect = false;
                using (WebResponse myHttpWebResponse = myHttpWebRequest.GetResponse())
                using (System.IO.Stream stream = myHttpWebResponse.GetResponseStream())
                {
                    int bytes;
                    gameRecvSB.Clear();
                    while ((bytes = stream.Read(gameRecvBuffer, 0, gameRecvBuffer.Length)) > 0)
                    {
                        for (int j = 0; j < bytes; j++)
                        {
                            gameRecvSB.Append(WormNetCharTable.Decode[gameRecvBuffer[j]]);
                        }
                    }

                    gameRecvSB.Replace("\n", "");
                }

                loadGamesCTS.Token.ThrowIfCancellationRequested();
            }, loadGamesCTS.Token)
            .ContinueWith((t) =>
            {
                if (t.IsCanceled || loadGamesCTS.Token.IsCancellationRequested)
                {
                    this.Close();
                    return;
                }

                if (t.IsFaulted)
                    return;

                if (!channel.Joined) // we already left the channel
                    return;

                try
                {
                    // <GAMELISTSTART><GAME GameName Hoster HosterAddress CountryID 1 PasswordNeeded GameID HEXCC><BR><GAME GameName Hoster HosterAddress CountryID 1 PasswordNeeded GameID HEXCC><BR><GAMELISTEND>
                    //string start = "<GAMELISTSTART>"; 15 chars
                    //string end = "<GAMELISTEND>"; 13 chars
                    if (gameRecvSB.Length > 28)
                    {
                        string[] games = gameRecvSB.ToString(15, gameRecvSB.Length - 28).Split(new string[] { "<BR>" }, StringSplitOptions.RemoveEmptyEntries);

                        // Set all the games we have in !isAlive state (we will know if the game is not active anymore)
                        for (int i = 0; i < channel.GameList.Count; i++)
                            channel.GameList[i].IsAlive = false;

                        for (int i = 0; i < games.Length; i++)
                        {
                            // <GAME GameName Hoster HosterAddress CountryID 1 PasswordNeeded GameID HEXCC><BR>
                            Match m = GameRegex.Match(games[i].Trim());
                            if (m.Success)
                            {
                                string name = m.Groups[1].Value.Replace('\b', ' ').Replace("#039", "\x12");

                                // Encode the name to decode it with GameDecode
                                int bytes = 0;
                                byte b;
                                for (int j = 0; j < name.Length; j++)
                                {
                                    if (WormNetCharTable.Encode.TryGetValue(name[j], out b))
                                    {
                                        gameRecvBuffer[bytes++] = b;
                                    }
                                }
                                gameRecvSB.Clear();
                                for (int j = 0; j < bytes; j++)
                                    gameRecvSB.Append(WormNetCharTable.DecodeGame[gameRecvBuffer[j]]);
                                name = gameRecvSB.ToString();

                                string hoster = m.Groups[2].Value;
                                string address = m.Groups[3].Value;

                                int countryID;
                                if (!int.TryParse(m.Groups[4].Value, out countryID))
                                    continue;

                                bool password = m.Groups[5].Value == "1";

                                uint gameID;
                                if (!uint.TryParse(m.Groups[6].Value, out gameID))
                                    continue;

                                string hexcc = m.Groups[7].Value;


                                // Get the country of the hoster
                                CountryClass country;
                                if (hexcc.Length < 9)
                                {
                                    country = CountriesClass.GetCountryByID(countryID);
                                }
                                else
                                {
                                    string hexstr = uint.Parse(hexcc).ToString("X");
                                    if (hexstr.Length == 8 && hexstr.Substring(0, 4) == "6487")
                                    {
                                        char c1 = WormNetCharTable.Decode[byte.Parse(hexstr.Substring(6), System.Globalization.NumberStyles.HexNumber)];
                                        char c2 = WormNetCharTable.Decode[byte.Parse(hexstr.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)];
                                        country = CountriesClass.GetCountryByCC(c1.ToString() + c2.ToString());
                                    }
                                    else
                                    {
                                        country = CountriesClass.DefaultCountry;
                                    }
                                }

                                // Add the game to the list or set its isAlive state true if it is already in the list
                                Game game = null;
                                for (int j = 0; j < channel.GameList.Count; j++)
                                {
                                    if (channel.GameList[j].ID == gameID)
                                    {
                                        game = channel.GameList[j];
                                        game.IsAlive = true;
                                        break;
                                    }
                                }
                                if (game == null)
                                {
                                    channel.GameList.Add(new Game(gameID, name, address, country, hoster, password));
                                    if (Notifications.Count > 0)
                                    {
                                        foreach (NotificatorClass nc in Notifications)
                                        {
                                            if (nc.InGameNames && nc.TryMatch(name.ToLower()))
                                            {
                                                NotificatorFound(hoster + " is hosting a game: " + name, channel);
                                                break;
                                            }
                                            if (nc.InHosterNames && nc.TryMatch(hoster.ToLower()))
                                            {
                                                NotificatorFound(hoster + " is hosting a game: " + name, channel);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Delete inactive games from the list
                        for (int i = 0; i < channel.GameList.Count; i++)
                        {
                            if (!channel.GameList[i].IsAlive)
                            {
                                channel.GameList.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }

                channel.GameListUpdatedTime = DateTime.Now;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
