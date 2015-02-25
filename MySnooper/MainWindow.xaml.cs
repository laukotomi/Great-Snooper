using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Net;
using System.Runtime.InteropServices;
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
        private readonly Dictionary<string, string> autoJoinList = new Dictionary<string, string>();
        private readonly List<IRCCommunicator> servers = new List<IRCCommunicator>();
        private readonly List<Dictionary<string, string>> newsList = new List<Dictionary<string,string>>();
        private readonly List<int> visitedChannels = new List<int>();
        private readonly Dictionary<string, bool> newsSeen = new Dictionary<string,bool>();
        private readonly Dictionary<string, List<string>> foundUsers = new Dictionary<string, List<string>>();
        public readonly List<NotificatorClass> Notifications = new List<NotificatorClass>();

        // Buffer things + communication
        private readonly byte[] recvBuffer = new byte[10240]; // 10kB
        private readonly System.Text.StringBuilder recvHTML = new System.Text.StringBuilder(10240); // 10kB
        private readonly CancellationTokenSource TusCTS = new CancellationTokenSource();
        private Task tusTask;
        private Task loadSettings;
        private CancellationTokenSource loadSettingsCTS = new CancellationTokenSource();
        private string latestVersion = string.Empty;

        // WormNet Web Communicator
        private readonly WormageddonWebComm wormWebC;

        // Helpers
        private Channel gameListChannel;
        private bool gameSurgeIsConnected = false;
        private bool isWindowFocused = true;

        // Timers
        private DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Input);
        private DispatcherTimer focusTimer = new DispatcherTimer(DispatcherPriority.Input);
        private int gameListCounter = 0;
        private int tusRequestCounter = 10;
        private bool gameListForce = false;
        private bool tusForce = false;
        private bool snooperClosing = false;
        private bool spamAllowed = false;

        // Sounds
        private Dictionary<string, SoundPlayer> soundPlayers = new Dictionary<string,SoundPlayer>();

        // League Seacher things
        private Channel _searchHere;
        private Channel searchHere
        {
            get
            {
                return _searchHere;
            }
            set
            {
                _searchHere = value;
                if (LeagueSearcherImage != null)
                {
                    if (value == null)
                        LeagueSearcherImage.Source = LeagueSearcherOff;
                    else
                        LeagueSearcherImage.Source = LeagueSearcherOn;
                }
            }
        }
        private string spamText = string.Empty;
        private int SearchCounter = 100;
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
                if (AwayOnOffImage != null)
                {
                    if (value == string.Empty)
                    {
                        AwayOnOffImage.Source = AwayOffImage;
                        AwayOnOffButton.ToolTip = AwayOnOffDefaultTooltip;
                    }
                    else
                    {
                        AwayOnOffImage.Source = AwayOnImage;
                        AwayOnOffButton.ToolTip = "You are away: " + value;
                    }
                }
                if (value != string.Empty)
                {
                    for (int i = 0; i < servers.Count; i++)
                    {
                        if (servers[i].IsRunning)
                        {
                            foreach (var item in servers[i].ChannelList)
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
                    for (int i = 0; i < servers.Count; i++)
                    {
                        if (servers[i].IsRunning)
                        {
                            foreach (var item in servers[i].ChannelList)
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

        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);


        // Constructor        
        public MainWindow() { } // Never used, but visual stdio throws an error if not exists
        public MainWindow(IRCCommunicator WormNetC, string serverAddress)
        {
            InitializeComponent();
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            this.DataContext = this;
            this.ServerAddress = serverAddress;
            this.WelcomeText.Text = "Welcome " + GlobalManager.User.Name + "!";

            // Servers
            WormNetC.ConnectionState += ConnectionState;
            servers.Add(WormNetC);

            IRCCommunicator gameSurge = new IRCCommunicator("irc.gamesurge.net", 6667, false);
            gameSurge.ConnectionState += ConnectionState;
            servers.Add(gameSurge);

            // Wormageddonweb Communicator
            wormWebC = new WormageddonWebComm(this, serverAddress);

            // Focustimer will focus to the textbox of a channel when we change channel
            focusTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
            focusTimer.Tick += SetFocusTextBox;

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
            this.autoJoinList.DeSerialize(Properties.Settings.Default.AutoJoinChannels);

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
        }

        private void MainWindow_Loaded(object sender, EventArgs e)
        {
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

                if (loadSettings.IsFaulted)
                {
                    ErrorLog.Log(loadSettings.Exception);
                    return;
                }

                if (!spamAllowed)
                {
                    ErrorLog.Log(loadSettings.Exception);
                    MessageBox.Show(this, "Failed to load the common settings!" + Environment.NewLine + "You will not be able to spam for league games (but you can still look for them). If this problem doesn't get away then the snooper may need to be updated.", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            MessageBox.Show(this, "Failed to start the updater! Please restart Great Snooper with administrator rights! (Right click on the icon and Run as Administrator)", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
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

                if (!snooperClosing && (gameListForce || gameListCounter >= 10) && DateTime.Now >= gameListChannel.GameListUpdatedTime.AddSeconds(3) && (wormWebC.LoadGamesTask == null || wormWebC.LoadGamesTask.IsCompleted))
                {
                    wormWebC.GetGamesOfChannel(gameListChannel);

                    gameListForce = false;
                    gameListCounter = 0;
                }
            }
            else if (gameListCounter != 0)
                gameListCounter = 0;

            // Get online tus users
            if (gameListChannel != null && gameListChannel.Joined && !gameListChannel.IsPrivMsgChannel)
            {
                tusRequestCounter++;
                if (!snooperClosing && (tusForce || tusRequestCounter >= 20) && (tusTask == null || tusTask.IsCompleted))
                {
                    tusTask = StartTusCommunication().ContinueWith((t) => TUSLoaded(t.Result), TaskScheduler.FromCurrentSynchronizationContext());
                    tusRequestCounter = 0;
                    tusForce = false;
                }
            }
            else if (tusRequestCounter != 0)
                tusRequestCounter = 0;

            // Leagues search (spamming)
            if (searchHere != null && spamText != string.Empty)
            {
                if (!searchHere.Joined) // reset
                {
                    ClearSpamming();
                }
                else
                {
                    SearchCounter++;
                    if (SearchCounter >= 60)
                    {
                        SendMessageToChannel(spamText, searchHere);
                        SearchCounter = 0;

                        spamCounter++;
                        if (spamCounter >= 10)
                        {
                            ClearSpamming();
                            searchHere.AddMessage(GlobalManager.SystemClient, "Great snooper stopped spamming and searching for league game(s)!", MessageSettings.OfflineMessage);
                            myNotifyIcon.ShowBalloonTip(null, "Great snooper stopped spamming and searching for league game(s)!", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

                            SoundPlayer sp;
                            if (Properties.Settings.Default.LeagueFailBeepEnabled && SoundEnabled && soundPlayers.TryGetValue("LeagueFailBeep", out sp))
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
            }
        }

        private void ClearSpamming()
        {
            SearchCounter = 100;
            spamCounter = 0;
            searchHere = null;
            spamText = string.Empty;
            foundUsers.Clear();
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
            {
                Type taskType = task.GetType();

                // Information about a client
                if (taskType == typeof(ClientUITask))
                {
                    ClientUITask client = (ClientUITask)task;
                    DoClientUITask(client);
                }

                // A client left the server
                else if (taskType == typeof(QuitUITask))
                {
                    QuitUITask quit = (QuitUITask)task;
                    DoQuitUITask(quit);
                }

                // A client parts a channel
                else if (taskType == typeof(PartedUITask))
                {
                    PartedUITask parted = (PartedUITask)task;
                    DoPartedUITask(parted);
                }

                // A client joins a channel
                else if (taskType == typeof(JoinedUITask))
                {
                    JoinedUITask joined = (JoinedUITask)task;
                    DoJoinedUITask(joined);
                }

                // Message
                else if (taskType == typeof(MessageUITask))
                {
                    MessageUITask message = (MessageUITask)task;
                    DoMessageUITask(message);
                }

                else if (taskType == typeof(ChannelListUITask))
                {
                    ChannelListUITask cList = (ChannelListUITask)task;
                    foreach (var item in cList.ChannelList)
                    {
                        Channel ch = new Channel(this, cList.Sender, item.Key, item.Value);

                        if (autoJoinList.ContainsKey(ch.HashName))
                        {
                            ch.Loading(true);
                            ch.Server.JoinChannel(ch.Name);
                        }
                    }

                    Channel worms = new Channel(this, servers[1], "#worms", "Place for hardcore wormers");
                    if (autoJoinList.ContainsKey(worms.HashName))
                    {
                        worms.Loading(true);
                        gameSurgeIsConnected = true;
                        worms.Server.Connect();
                    }
                }

                // Offline user notification
                else if (taskType == typeof(OfflineUITask))
                {
                    OfflineUITask offline = (OfflineUITask)task;

                    Client c;
                    // Send a message to the private message channel that the user is offline
                    if (offline.Sender.Clients.TryGetValue(offline.ClientName.ToLower(), out c))
                    {
                        c.OnlineStatus = 0;
                        foreach (Channel ch in c.PMChannels)
                        {
                            ch.AddMessage(GlobalManager.SystemClient, offline.ClientName + " is currently offline.", MessageSettings.OfflineMessage);
                        }
                    }
                }
                else if (taskType == typeof(NickUITask))
                {
                    NickUITask nick = (NickUITask)task;
                    DoNickUITask(nick);
                }
                else if (taskType == typeof(ClientAddOrRemoveTask))
                {
                    ClientAddOrRemoveTask addOrRemove = (ClientAddOrRemoveTask)task;
                    DoClientAddOrRemoveTask(addOrRemove);
                }
                else if (taskType == typeof(ClientLeaveConvTask))
                {
                    ClientLeaveConvTask clientLeave = (ClientLeaveConvTask)task;
                    Channel ch = null;
                    if (!clientLeave.Sender.ChannelList.TryGetValue(clientLeave.ChannelHash, out ch))
                        return;

                    Client c = null;
                    if (!clientLeave.Sender.Clients.TryGetValue(clientLeave.ClientName.ToLower(), out c))
                        return;

                    ch.RemoveClientFromConversation(c, false);
                    ch.AddMessage(GlobalManager.SystemClient, clientLeave.ClientName + " has left the conversation.", MessageSettings.OfflineMessage);
                }
                else if (taskType == typeof(NickNameInUseTask))
                {
                    Channel ch = servers[1].ChannelList["#worms"];
                    if (ch.Joined)
                        ch.AddMessage(GlobalManager.SystemClient, "The selected nickname is already in use!", MessageSettings.OfflineMessage);
                }
            }
        }

        // When a user changes his/her name
        private void DoNickUITask(NickUITask task)
        {
            Client c;
            string oldLowerName = task.OldClientName.ToLower();
            if (task.Sender.Clients.TryGetValue(oldLowerName, out c))
            {
                if (c == task.Sender.User)
                    task.Sender.User.Name = task.NewClientName;

                // To keep SortedDictionary sorted, first client will be removed..
                foreach (Channel ch in c.Channels)
                    ch.Clients.Remove(c);
                foreach (Channel ch in c.PMChannels)
                    ch.RemoveClientFromConversation(c, false, false);

                c.Name = task.NewClientName;
                task.Sender.Clients.Remove(oldLowerName);
                task.Sender.Clients.Add(c.LowerName, c);

                // then later it will be readded with new Name
                foreach (Channel ch in c.Channels)
                {
                    ch.Clients.Add(c);
                    ch.AddMessage(GlobalManager.SystemClient, task.OldClientName + " is now known as " + task.NewClientName + ".", MessageSettings.OfflineMessage);
                }
                foreach (Channel ch in c.PMChannels)
                {
                    ch.AddClientToConversation(c, false, false);
                    ch.AddMessage(GlobalManager.SystemClient, task.OldClientName + " is now known as " + task.NewClientName + ".", MessageSettings.OfflineMessage);
                }
            }
        }

        // Adds or removes a client from a conversation
        private void DoClientAddOrRemoveTask(ClientAddOrRemoveTask task)
        {
            Channel ch = null;
            if (!task.Sender.ChannelList.TryGetValue(task.ChannelHash, out ch))
                return;

            Client c = null;
            if (!task.Sender.Clients.TryGetValue(task.ClientName.ToLower(), out c))
                return;

            Client c2 = null;
            if (!task.Sender.Clients.TryGetValue(task.SenderName.ToLower(), out c2) || !ch.IsInConversation(c2))
                return;

            if (task.Type == ClientAddOrRemoveTask.TaskType.Add)
            {
                ch.AddClientToConversation(c, false);
                ch.AddMessage(GlobalManager.SystemClient, task.SenderName + " has added " + task.ClientName + " to the conversation.", MessageSettings.OfflineMessage);
            }
            else
            {
                if (task.ClientName.ToLower() == task.Sender.User.LowerName)
                {
                    ch.AddMessage(GlobalManager.SystemClient, "You have been removed from this conversation.", MessageSettings.OfflineMessage);
                    ch.Disabled = true;
                }
                else
                {
                    ch.RemoveClientFromConversation(c, false);
                    ch.AddMessage(GlobalManager.SystemClient, task.SenderName + " has removed " + task.ClientName + " from the conversation.", MessageSettings.OfflineMessage);
                }
            }
        }

        // When a message arrives
        private void DoMessageUITask(MessageUITask task)
        {
            Client c = null;
            Channel ch = null;
            string fromLow = task.ClientName.ToLower();

            // If the message arrived in a closed channel
            if (task.Sender.ChannelList.TryGetValue(task.ChannelHash, out ch) && !ch.Joined)
                return;

            // If the user doesn't exists we create one
            if (!task.Sender.Clients.TryGetValue(fromLow, out c))
            {
                c = new Client(task.ClientName);
                c.IsBanned = IsBanned(fromLow);
                c.IsBuddy = IsBuddy(fromLow);
                c.OnlineStatus = 2;
                task.Sender.Clients.Add(c.LowerName, c);
            }

            if (ch == null) // New private message arrived for us
                ch = new Channel(this, task.Sender, task.ChannelHash, "Chat with " + c.Name, c);

            // Search for league or hightlight our name
            if (!ch.IsPrivMsgChannel)
            {
                string highlightWord = string.Empty;
                bool LookForLeague = task.Setting.Type == MessageTypes.Channel && searchHere == ch;
                bool notificationSearch = false;
                bool notif = true; // to ensure that only one notification will be sent
                if (Notifications.Count > 0)
                {
                    foreach (NotificatorClass nc in Notifications)
                    {
                        if (notif && nc.InMessages)
                        {
                            notificationSearch = true;
                        }
                        if (nc.InMessageSenders && nc.TryMatch(c.LowerName))
                        {
                            notif = false;
                            NotificatorFound(c.Name + ": " + task.Message, ch); break;
                        }
                    }
                }

                if (task.Setting.Type == MessageTypes.Channel || LookForLeague)
                {
                    string[] words = task.Message.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < words.Length; i++)
                    {
                        if (words[i] == task.Sender.User.Name)
                        {
                            Highlight(ch);
                        }
                        else if (LookForLeague)
                        {
                            string lower = words[i].ToLower();
                            // foundUsers.ContainsKey(lower) == league name we are looking for
                            // foundUsers[lower].Contains(c.LowerName) == the user we found for league lower
                            foreach (var item in foundUsers)
                            {
                                if (lower.Contains(item.Key) && !foundUsers[item.Key].Contains(c.LowerName))
                                {
                                    foundUsers[item.Key].Add(c.LowerName);
                                    highlightWord = words[i];
                                    if (!isWindowFocused)
                                        this.FlashWindow();
                                    myNotifyIcon.ShowBalloonTip(null, c.Name + ": " + task.Message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

                                    SoundPlayer sp;
                                    if (Properties.Settings.Default.LeagueFoundBeepEnabled && SoundEnabled && soundPlayers.TryGetValue("LeagueFoundBeep", out sp))
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
                                    break;
                                }
                            }

                        }
                        else if (notif && notificationSearch)
                        {
                            foreach (NotificatorClass nc in Notifications)
                            {
                                if (nc.InMessages && nc.TryMatch(words[i].ToLower()))
                                {
                                    highlightWord = words[i];
                                    NotificatorFound(c.Name + ": " + task.Message, ch);
                                    break;
                                }
                            }
                        }
                    }
                }
                ch.AddMessage(c, task.Message, task.Setting, highlightWord);
            }
            // Beep user that new private message arrived
            else
            {
                // This way away message will be added to the channel later than the arrived message
                ch.AddMessage(c, task.Message, task.Setting);

                if (!c.IsBanned)
                {
                    Channel selectedCH = null;
                    if (Channels.SelectedItem != null)
                        selectedCH = (Channel)((TabItem)Channels.SelectedItem).DataContext;

                    // Private message arrived notification
                    if (ch.BeepSoundPlay && (ch != selectedCH || !isWindowFocused))
                    {
                        ch.NewMessages = true;
                        ch.BeepSoundPlay = false;
                        if (!isWindowFocused)
                            this.FlashWindow();
                        myNotifyIcon.ShowBalloonTip(null, c.Name + ": " + task.Message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

                        SoundPlayer pmbeep;
                        if (Properties.Settings.Default.PMBeepEnabled && SoundEnabled && soundPlayers.TryGetValue("PMBeep", out pmbeep))
                        {
                            try
                            {
                                pmbeep.Play();
                            }
                            catch (Exception ex)
                            {
                                ErrorLog.Log(ex);
                            }
                        }
                    }

                    // Send back away message if needed
                    if (AwayText != string.Empty && ch.SendAway && ch.Messages.Count > 0 && (selectedCH != ch || !isWindowFocused))
                    {
                        SendMessageToChannel(AwayText, ch);
                        ch.SendAway = false;
                        ch.SendBack = true;
                    }
                }
            }
        }

        // When a user joins a channel
        private void DoJoinedUITask(JoinedUITask task)
        {
            Channel ch;
            if (!task.Sender.ChannelList.TryGetValue(task.ChannelHash, out ch))
                return;

            string lowerName = task.ClientName.ToLower();
            bool buddyJoined = false;
            bool userJoined = false;

            if (lowerName != task.Sender.User.LowerName)
            {
                if (ch.Joined)
                {
                    Client c = null;
                    if (!task.Sender.Clients.TryGetValue(lowerName, out c))// Register the new client
                    {
                        c = new Client(task.ClientName, task.Clan);
                        c.IsBanned = IsBanned(lowerName);
                        c.IsBuddy = IsBuddy(lowerName);
                        task.Sender.Clients.Add(lowerName, c);
                    }

                    if (c.OnlineStatus != 1)
                    {
                        ch.Server.GetInfoAboutClient(task.ClientName);
                        c.TusActive = false;
                        c.ClientGreatSnooper = false;
                        c.OnlineStatus = 1;

                        foreach (Channel channel in c.PMChannels)
                            channel.AddMessage(c, "is online.", MessageSettings.JoinMessage);
                    }

                    c.Channels.Add(ch);
                    ch.Clients.Add(c);

                    if (c.IsBuddy)
                    {
                        buddyJoined = true;
                        ch.AddMessage(c, "joined the channel.", MessageSettings.BuddyJoinedMessage);
                        myNotifyIcon.ShowBalloonTip(null, c.Name + " is online.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    }
                    else
                        ch.AddMessage(c, "joined the channel.", MessageSettings.JoinMessage);

                    if (Notifications.Count > 0)
                    {
                        foreach (NotificatorClass nc in Notifications)
                        {
                            if (nc.InJoinMessages && nc.TryMatch(c.LowerName))
                            {
                                NotificatorFound(c.Name + " joined " + ch.Name + "!", ch);
                                break;
                            }
                        }
                    }

                }
                else
                    return;
            }
            else if (!ch.Joined) // We joined a channel
            {
                ch.Join(wormWebC);
                ch.Server.GetChannelClients(ch.Name); // get the users in the channel

                userJoined = true;
                ch.AddMessage(task.Sender.User, "joined the channel.", MessageSettings.JoinMessage);

                if (Channels.SelectedItem != null)
                {
                    Channel selectedCH = (Channel)((TabItem)Channels.SelectedItem).DataContext;
                    if (ch == selectedCH)
                    {
                        if (ch.CanHost)
                            this.gameListForce = true;
                        this.tusForce = true;
                    }
                }
            }

            SoundPlayer sp;
            if (buddyJoined && Properties.Settings.Default.BJBeepEnabled && SoundEnabled && soundPlayers.TryGetValue("BJBeep", out sp))
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

            if (userJoined && Channels.SelectedItem != null)
            {
                Channel selectedCH = (Channel)((TabItem)Channels.SelectedItem).DataContext;
                if (ch == selectedCH)
                {
                    ch.ChannelTabItem.UpdateLayout();
                    ch.TheTextBox.Focus();
                }
            }
        }

        // When a user parts a channel
        private void DoPartedUITask(PartedUITask task)
        {
            Channel ch;
            if (!task.Sender.ChannelList.TryGetValue(task.ChannelHash, out ch) || !ch.Joined)
                return;

            // This can reagate for force PART (if that exists :D) - this was the old way to PART a channel (was waiting for a PART message from the server as an answer for the PART command sent by the client)
            if (task.ClientNameL == task.Sender.User.LowerName)
            {
                if (task.Sender.IsWormNet)
                    ch.Part();
                else
                {
                    ch.Part();
                    gameListChannel.Server.CancelAsync();
                }
            }
            else
            {
                Client c;
                if (task.Sender.Clients.TryGetValue(task.ClientNameL, out c))
                {
                    ch.Clients.Remove(c);
                    c.Channels.Remove(ch);
                    if (c.Channels.Count == 0)
                    {
                        if (c.PMChannels.Count > 0)
                            c.OnlineStatus = 2;
                        else
                            task.Sender.Clients.Remove(task.ClientNameL);
                    }
                    ch.AddMessage(c, "has left the channel.", MessageSettings.PartMessage);
                }
            }
        }

        // When a user quits
        private void DoQuitUITask(QuitUITask task)
        {
            Client c;
            if (task.Sender.Clients.TryGetValue(task.ClientNameL, out c))
            {
                string msg;
                if (task.Sender.IsWormNet)
                {
                    if (task.Message.Length > 0)
                        msg = "has left WormNet (" + task.Message + ").";
                    else
                        msg = "has left WormNet.";
                }
                else
                {
                    if (task.Message.Length > 0)
                        msg = "has left the server (" + task.Message + ").";
                    else
                        msg = "has left the server.";
                }

                // Send quit message to the channels where the user was active
                for (int i = 0; i < c.Channels.Count; i++)
                {
                    c.Channels[i].AddMessage(c, msg, MessageSettings.QuitMessage);
                    c.Channels[i].Clients.Remove(c);
                }
                c.Channels.Clear();

                for (int i = 0; i < c.PMChannels.Count; i++)
                    c.PMChannels[i].AddMessage(c, msg, MessageSettings.QuitMessage);

                if (c.PMChannels.Count == 0)
                    task.Sender.Clients.Remove(task.ClientNameL);
                // If we had a private chat with the user
                else
                    c.OnlineStatus = 0;
            }
        }

        // Information about a user
        private void DoClientUITask(ClientUITask task)
        {
            Channel ch;
            bool channelBad = !task.Sender.ChannelList.TryGetValue(task.ChannelHash, out ch) || !ch.Joined; // GameSurge may send info about client with channel name: *.. so we try to process all these messages

            Client c = null;
            string lowerName = task.ClientName.ToLower();

            if (!task.Sender.Clients.TryGetValue(lowerName, out c))
            {
                if (!channelBad)
                {
                    c = new Client(task.ClientName, task.Clan);
                    c.IsBanned = IsBanned(lowerName);
                    c.IsBuddy = IsBuddy(lowerName);
                    task.Sender.Clients.Add(lowerName, c);
                }
                else // we don't have any common channel with this client
                    return;
            }

            c.OnlineStatus = 1;
            if (!c.TusActive)
            {
                c.Country = task.Country;
                c.Rank = RanksClass.GetRankByInt(task.Rank);
            }
            c.ClientGreatSnooper = task.ClientGreatSnooper;
            c.ClientApp = task.ClientApp;

            // This is needed, because when we join a channel we get information about the channel users using the WHO command
            if (!channelBad && !c.Channels.Contains(ch))
            {
                c.Channels.Add(ch);
                ch.Clients.Add(c);
            }
        }
        #endregion

        #region Channel things


        // Leave a channel
        private void LeaveChannel(object sender, RoutedEventArgs e)
        {
            if (gameListChannel != null && gameListChannel.Joined)
            {
                if (gameListChannel.Server.IsWormNet && !gameListChannel.IsLoading)
                {
                    gameListChannel.Server.LeaveChannel(gameListChannel.Name);
                    gameListChannel.Part();
                }
                else
                {
                    gameListChannel.Part();
                    gameSurgeIsConnected = false;
                    gameListChannel.Server.CancelAsync();
                }
            }
            e.Handled = true;
        }


        Channel oldChannel;
        // If we changed our channel save the actual channel
        private void ChannelChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            focusTimer.Stop();

            if (Channels.SelectedItem == null)
                return;

            if (oldChannel != null && oldChannel.IsPrivMsgChannel)
                oldChannel.GenerateHeader();

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
                {
                    var view = CollectionViewSource.GetDefaultView(gameListChannel.TheDataGrid.ItemsSource);
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

                gameListChannel = ch;
                gameListForce = true;
            }

            if (ch.Joined)
            {
                focusTimer.Start();
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
                for (int i = 0; i < servers.Count; i++)
                {
                    if (servers[i].IsRunning && servers[i].Clients.ContainsKey(client.LowerName))
                    {
                        foreach (var item in servers[i].ChannelList)
                        {
                            if (!item.Value.IsPrivMsgChannel && item.Value.Joined && item.Value.Clients.Contains(client))
                                LoadMessages(item.Value, GlobalManager.MaxMessagesDisplayed, true);
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
                    chat.Visibility = System.Windows.Visibility.Visible;
                }
                else
                    chat.Visibility = System.Windows.Visibility.Collapsed;

                MenuItem conversation = (MenuItem)obj.ContextMenu.Items[1];
                if (client.CanConversation() && client.LowerName != ch.Server.User.LowerName)
                {
                    if (ch.IsPrivMsgChannel && ch.Clients[0].CanConversation())
                    {
                        conversation.Tag = new object[] { client, ch };
                        if (ch.IsInConversation(client))
                        {
                            if (ch.Clients.Count == 1 && ch.Clients[0] == client)
                                conversation.Visibility = System.Windows.Visibility.Collapsed;
                            else
                            {
                                conversation.Header = "Remove from conversation";
                                conversation.Visibility = System.Windows.Visibility.Visible;
                            }
                        }
                        else
                        {
                            conversation.Header = "Add to conversation";
                            conversation.Visibility = System.Windows.Visibility.Visible;
                        }
                    }
                    else
                        conversation.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                    conversation.Visibility = System.Windows.Visibility.Collapsed;

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
            var obj = sender as TextBox;
            if (obj.Text == "Filter..")
                obj.Text = "";
        }

        private void FilterLeft(object sender, KeyboardFocusChangedEventArgs e)
        {
            var obj = sender as TextBox;
            if (obj.Text.Trim() == string.Empty)
            {
                obj.Text = "Filter..";
            }
        }

        private void Filtering(object sender, KeyEventArgs e)
        {
            if (gameListChannel != null && gameListChannel.Joined)
            {
                var obj = sender as TextBox;
                var view = CollectionViewSource.GetDefaultView(gameListChannel.TheDataGrid.ItemsSource);
                if (view != null)
                {
                    string[] filters = obj.Text.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < filters.Length; i++)
                        filters[i] = filters[i].Trim().ToLower();

                    if (filters.Length == 0)
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

                            for (int i = 0; i < filters.Length; i++)
                            {
                                if (
                                    c.LowerName.Contains(filters[i])
                                    || c.TusActive && c.TusLowerNick.Contains(filters[i])
                                    || c.Clan.Length >= filters[i].Length && c.Clan.Substring(0, filters[i].Length).ToLower() == filters[i]
                                    || c.Country != null && c.Country.LowerName.Length >= filters[i].Length && c.Country.LowerName.Substring(0, filters[i].Length) == filters[i]
                                    || c.Rank != null && c.Rank.LowerName.Length >= filters[i].Length && c.Rank.LowerName.Substring(0, filters[i].Length) == filters[i]
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
            isWindowFocused = true;
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
            this.StopFlashingWindow();
        }

        // Need to know that if the window is activated to play beep sounds
        private void WindowDeactivated(object sender, EventArgs e)
        {
            isWindowFocused = false;
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
                this.Hide();
                myNotifyIcon.ShowBalloonTip(null, "Great Snooper is still running here.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                e.Cancel = true;
                return;
            }

            snooperClosing = true;

            // Stop the clock
            timer.Stop();

            if (!loadSettings.IsCompleted)
            {
                loadSettingsCTS.Cancel();
                e.Cancel = true;
                return;
            }

            // Stop backgroundworkers
            if (gameProcess != null)
            {
                gameProcess.Dispose();
                gameProcess = null;
            }

            if (tusTask != null && !tusTask.IsCompleted)
            {
                TusCTS.Cancel();
                e.Cancel = true;
                return;
            }

            if (wormWebC.LoadGamesTask != null && !wormWebC.LoadGamesTask.IsCompleted)
            {
                wormWebC.LoadGamesCTS.Cancel();
                e.Cancel = true;
                return;
            }

            // Stop servers
            for (int i = 0; i < servers.Count; i++)
            {
                if (servers[i].IsRunning)
                {
                    servers[i].CancelAsync();
                    e.Cancel = true;
                    return;
                }
            }
            
            // Log channel messages
            for (int i = 0; i < servers.Count; i++)
            {
                foreach (var item in servers[i].ChannelList)
                {
                    if (item.Value.Joined)
                    {
                        if (servers[i].IsWormNet)
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
        private void ConnectionState(IRCCommunicator sender, IRCCommunicator.ConnectionStates state)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                if (snooperClosing)
                {
                    if (state == IRCCommunicator.ConnectionStates.Connected)
                        sender.CancelAsync();
                    else
                        this.Close();
                }
                else if (state == IRCCommunicator.ConnectionStates.Connected)
                {
                    bool reconnecting = false;
                    foreach (var item in sender.ChannelList)
                    {
                        if (item.Value.Joined)
                        {
                            reconnecting = true;
                            break;
                        }
                    }

                    if (reconnecting)
                    {
                        foreach (var item in sender.ChannelList)
                        {
                            item.Value.Reconnecting(false);
                            if (item.Value.Joined && !item.Value.IsPrivMsgChannel)
                            {
                                sender.JoinChannel(item.Value.Name);
                                sender.GetChannelClients(item.Value.Name);
                            }
                        }
                    }
                    else if (!sender.IsWormNet && gameSurgeIsConnected)
                        sender.JoinChannel("#worms");
                }
                else
                {
                    if (!sender.IsWormNet)
                    {
                        if (state == IRCCommunicator.ConnectionStates.UsernameInUse)
                        {
                            bool reconnecting = false;
                            foreach (var item in sender.ChannelList)
                            {
                                if (item.Value.Joined)
                                {
                                    reconnecting = true;
                                    break;
                                }
                            }

                            if (!reconnecting)
                            {
                                foreach (var item in sender.ChannelList)
                                {
                                    item.Value.Loading(false);
                                }

                                MessageBox.Show("Your nickname is already used by somebody on this server. You can choose another nickname in Settings.", "Nickname is in use", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                        }

                        if (!gameSurgeIsConnected)
                            return;
                    }

                    // Reconnect needed
                    foreach (var item in sender.ChannelList)
                    {
                        item.Value.Reconnecting(true);
                    }
                    sender.Clients.Clear();
                    sender.Reconnect();
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

            for (int i = 0; i < servers.Count; i++)
            {
                if (servers[i].IsRunning)
                {
                    Client c;
                    if (servers[i].Clients.TryGetValue(lowerName, out c))
                        c.IsBuddy = true;
                }
            }
        }

        public void RemoveBuddy(string name)
        {
            string lowerName = name.ToLower();
            buddyList.Remove(lowerName);

            for (int i = 0; i < servers.Count; i++)
            {
                if (servers[i].IsRunning)
                {
                    Client c;
                    if (servers[i].Clients.TryGetValue(lowerName, out c))
                        c.IsBuddy = false;
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

            for (int i = 0; i < servers.Count; i++)
            {
                if (servers[i].IsRunning)
                {
                    Client c;
                    if (servers[i].Clients.TryGetValue(lowerName, out c))
                        c.IsBanned = true;
                }
            }
        }

        public void RemoveBan(string name)
        {
            string lowerName = name.ToLower();
            banList.Remove(lowerName);

            for (int i = 0; i < servers.Count; i++)
            {
                if (servers[i].IsRunning)
                {
                    Client c;
                    if (servers[i].Clients.TryGetValue(lowerName, out c))
                        c.IsBanned = false;
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
            this.Show();
            this.Activate();
            e.Handled = true;
        }

        private void ShowSnooper(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.Activate();
            e.Handled = true;
        }
    }
}
