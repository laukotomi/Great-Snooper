using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;


namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        // User datas
        private string ServerAddress;
        private Dictionary<string, string> Leagues;

        // Away
        private string _AwayText;
        private string AwayText
        {
            get
            {
                return _AwayText;
            }
            set
            {
                _AwayText = value;
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
                    foreach (var item in WormNetM.ChannelList)
                        item.Value.AwaySent = false;
                }
            }
        }

        // Buffer things
        private byte[] RecvBuffer;
        private System.Text.StringBuilder RecvHTML;

        // WormNet Web Communicator
        WormageddonWebComm WormWebC;

        // Helpers
        private Channel GameListChannel;
        private bool IsWindowFocused = true;
        private List<int> VisitedChannels;
        private List<Dictionary<string, string>> NewsList;
        private Dictionary<string, bool> NewsSeen;

        // Sounds
        private SoundPlayer PrivateMessageBeep;
        private SoundPlayer BuddyOnlineBeep;
        private SoundPlayer HighlightBeep;
        private SoundPlayer LeagueGameFoundBeep;
        private SoundPlayer LeagueGameFailBeep;

        // League Seacher things
        private Dictionary<string, List<string>> FoundUsers = new Dictionary<string, List<string>>();
        private Channel _SearchHere;
        private Channel SearchHere
        {
            get
            {
                return _SearchHere;
            }
            set
            {
                _SearchHere = value;
                if (LeagueSearcherImage != null)
                {
                    if (value == null)
                        LeagueSearcherImage.Source = LeagueSearcherOff;
                    else
                        LeagueSearcherImage.Source = LeagueSearcherOn;
                }
            }
        }
        private string SpamText = string.Empty;
        private int SpamCounter;
        private int SearchCounter = 100;



        private System.Windows.Threading.DispatcherTimer Clock;
        private System.Windows.Threading.DispatcherTimer FocusTimer;
        private int GameListCounter = 0;
        private int TUSRequestCounter = 10;
        private int ReconnectCounter = 100;
        private bool GameListForce = false;
        private bool SnooperClosing = false;


        public static RoutedCommand NextChannelCommand = new RoutedCommand();
        public static RoutedCommand PrevChannelCommand = new RoutedCommand();
        public static RoutedCommand CloseChannelCommand = new RoutedCommand();
        public static RoutedCommand FilterCommand = new RoutedCommand();

        // Constructor        
        public MainWindow() { } // Never used, but visual stdio throws an error if not exists
        public MainWindow(IRCCommunicator WormNetC, System.Threading.Thread IrcThread, string serverAddress, Dictionary<string, string> Leagues, List<Dictionary<string, string>> NewsList)
        {
            InitializeComponent();

            RecvBuffer = new byte[10240]; // 10kB
            RecvHTML = new System.Text.StringBuilder(10240); // 10kB
            VisitedChannels = new List<int>();
            AwayText = string.Empty;


            this.ServerAddress = serverAddress;
            this.Leagues = Leagues;
            this.NewsList = NewsList;


            this.IrcThread = IrcThread;
            this.WormNetC = WormNetC;
            WormNetC.ConnectionState += ConnectionState;
            //WormNetC.Channel += IrcChannel;
            WormNetC.ListEnd += ListEnd;
            WormNetC.Client += Client;
            WormNetC.Joined += Joined;
            WormNetC.Parted += Parted;
            WormNetC.Quitted += Quitter;
            WormNetC.Message += MessageToChannel;
            WormNetC.OfflineUser += OfflineUser;


            // Wormageddonweb Communicator
            WormWebC = new WormageddonWebComm(this, serverAddress);


            // Initialize the WormNet Manipulator
            WormNetM = new IRCManipulator(serverAddress, WormNetC, WormWebC);


            string[] SavedSearchings = Properties.Settings.Default.SearchForThese.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in SavedSearchings)
                FoundUsers.Add(str, new List<string>());

            // Initialize a Timer which will help updating things that should be updated periodically (game list, news..)
            Clock = new System.Windows.Threading.DispatcherTimer();
            Clock.Interval = new TimeSpan(0, 0, 1);
            Clock.Tick += ClockTick;
            Clock.Start();

            FocusTimer = new DispatcherTimer();
            FocusTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
            FocusTimer.Tick += SetFocusTextBox;

            // Load sounds
            if (File.Exists(Properties.Settings.Default.PMBeep))
                PrivateMessageBeep = new SoundPlayer(new FileInfo(Properties.Settings.Default.PMBeep).FullName);
            if (File.Exists(Properties.Settings.Default.BJBeep))
                BuddyOnlineBeep = new SoundPlayer(new FileInfo(Properties.Settings.Default.BJBeep).FullName);
            if (File.Exists(Properties.Settings.Default.HBeep)) 
                HighlightBeep = new SoundPlayer(new FileInfo(Properties.Settings.Default.HBeep).FullName);
            if (File.Exists(Properties.Settings.Default.LeagueFoundBeep))
                LeagueGameFoundBeep = new SoundPlayer(new FileInfo(Properties.Settings.Default.LeagueFoundBeep).FullName);
            if (File.Exists(Properties.Settings.Default.LeagueFailBeep))
                LeagueGameFailBeep = new SoundPlayer(new FileInfo(Properties.Settings.Default.LeagueFailBeep).FullName);


            // Initialize backgroundworkers for games
            Games();

            // Initialize backgroundworkers for TUS
            TUS();

            // Get channels
            Channels.Items.Clear();
            WormNetC.GetChannelList();
        }

        private void MainWindow_Loaded(object sender, EventArgs e)
        {
            // News
            NewsSeen = new Dictionary<string, bool>();
            string[] temp = Properties.Settings.Default.NewsSeen.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < temp.Length; i++)
                NewsSeen.Add(temp[i], false);

            bool open = false;
            foreach (Dictionary<string, string> item in NewsList)
            {
                try
                {
                    if (item["show"] == "1")
                    {
                        if (!NewsSeen.ContainsKey(item["id"]))
                            open = true;
                        else
                            NewsSeen[item["id"]] = true;
                    }
                }
                catch (Exception) { }
            }

            List<string> toRemove = new List<string>();
            foreach (var item in NewsSeen)
            {
                if (!item.Value)
                    toRemove.Add(item.Key);
            }
            for (int i = 0; i < toRemove.Count; i++)
                NewsSeen.Remove(toRemove[i]);

            if (open)
                OpenNewsWindow();
        }

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
                Channel ch = (Channel)((TabItem)Channels.SelectedItem).Tag;
                if (ch.IsPrivMsgChannel)
                {
                    int index = Channels.SelectedIndex;
                    VisitedChannels.Remove(index);
                    for (int i = 0; i < VisitedChannels.Count; i++)
                    {
                        if (VisitedChannels[i] > index)
                            VisitedChannels[i]--;
                    }
                    int lastindex = VisitedChannels[VisitedChannels.Count - 1];

                    ch.Log(ch.Messages.Count, true);
                    WormNetM.ChannelList.Remove(ch.LowerName);
                    Channels.Items.RemoveAt(index);
                    Channels.SelectedIndex = lastindex;
                }
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

        private void SetFocusTextBox(object sender, EventArgs e)
        {
            FocusTimer.Stop();
            if (Channels.SelectedItem != null)
            {
                Channel ch = (Channel)((TabItem)Channels.SelectedItem).Tag;
                ch.TheTextBox.Focus();
            }
        }


        private void ClockTick(object sender, EventArgs e)
        {
            // Reconnecting
            if (!SnooperClosing && !IrcThread.IsAlive)
            {
                ReconnectCounter++;

                if (ReconnectCounter >= 30)
                {
                    LoadingRing.IsActive = true;
                    foreach (var item in WormNetM.ChannelList)
                    {
                        if (item.Value.Joined)
                        {
                            item.Value.TheTextBox.IsEnabled = false;
                            item.Value.Clients.Clear();
                        }
                    }
                    WormNetM.Clients.Clear();
                    TusUsers.Clear();
                    WormNetC.ClearRequests();

                    // Start WormNet communicator thread
                    IrcThread = new System.Threading.Thread(new System.Threading.ThreadStart(WormNetC.run));
                    IrcThread.Start();

                    ReconnectCounter = 0;
                }
            }
            else if (ReconnectCounter != 0)
                ReconnectCounter = 0;


            // Game list refresh
            if (GameListChannel != null && GameListChannel.CanHost && GameListChannel.Joined)
            {
                GameListCounter++;

                if ((GameListForce || GameListCounter >= 10) && DateTime.Now >= GameListChannel.GameListUpdatedTime.AddSeconds(3))
                {
                    if (WormWebC.GetGamesOfChannel(GameListChannel))
                        GameListChannel.GameListUpdatedTime = DateTime.Now;

                    GameListForce = false;
                    GameListCounter = 0;
                }
            }
            else if (GameListCounter != 0)
                GameListCounter = 0;

            // Get online tus users
            if (GameListChannel != null && GameListChannel.Joined && GameListChannel.Clients.Count > 0)
            {
                TUSRequestCounter++;
                if (!SnooperClosing && TUSRequestCounter >= 15 && !TUSCommunicator.IsBusy)
                {
                    TUSCommunicator.RunWorkerAsync();
                    TUSRequestCounter = 0;
                }
            }
            else if (TUSRequestCounter != 0)
                TUSRequestCounter = 0;

            // Leagues search
            if (SearchHere != null && SpamText != string.Empty)
            {
                if (!SearchHere.Joined)
                {
                    SearchCounter = 100;
                    SpamCounter = 0;
                    SearchHere = null;
                    SpamText = string.Empty;
                    FoundUsers.Clear();
                }
                else
                {
                    SearchCounter++;
                    if (SearchCounter >= 60)
                    {
                        SendMessageToChannel(SpamText, SearchHere);
                        SearchCounter = 0;

                        SpamCounter++;
                        if (SpamCounter >= 10)
                        {
                            SearchHere.AddMessage(GlobalManager.SystemClient, "Great snooper stopped spamming and searching for league game(s)!", MessageTypes.Offline);
                            // Same goes in MainWindow.Windows.cs!
                            SearchCounter = 100;
                            SpamCounter = 0;
                            SearchHere = null;
                            SpamText = string.Empty;
                            FoundUsers.Clear();

                            if (Properties.Settings.Default.LeagueFailBeepEnabled && SoundEnabled && LeagueGameFailBeep != null)
                            {
                                try
                                {
                                    LeagueGameFailBeep.Play();
                                }
                                catch (Exception ex)
                                {
                                    ErrorLog.log(ex);
                                }
                            }
                        }
                    }
                }
            }
        }

        #region Channel things
        // Enter a channel
        private void Enter_Channel(object sender, RoutedEventArgs e)
        {
            Channel ch = (Channel)((Button)sender).Tag;

            if (!ch.Joined)
                WormNetC.JoinChannel(ch.Name);
            e.Handled = true;
        }

        // Leave a channel
        private void Leave_Channel(object sender, RoutedEventArgs e)
        {
            if (GameListChannel != null && GameListChannel.Joined)
            {
                WormNetC.LeaveChannel(GameListChannel.Name);
                GameListChannel.Part();
            }
            e.Handled = true;
        }


        private void ChannelLeaving(Channel ch)
        {
            for (int i = 0; i < ch.Clients.Count; i++)
            {
                Client c = ch.Clients[i];
                c.Channels.Remove(ch);
                if (c.Channels.Count == 0)
                {
                    WormNetM.Clients.Remove(c.LowerName);
                    TusUsers.Remove(c.LowerName);
                    // If we had a private message channel with the user we sign that we don't know if the user is online or not
                    foreach (var item in WormNetM.ChannelList)
                    {
                        if (item.Value.IsPrivMsgChannel && item.Value.LowerName == c.LowerName)
                        {
                            item.Value.TheClient.OnlineStatus = 2;
                            break;
                        }
                    }
                }
            }
            UpdateDescription();
        }


        // If we changed our channel save the actual channel
        private void ChannelChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;

            if (Channels.SelectedItem == null)
                return;

            Channel ch = (Channel)((TabItem)Channels.SelectedItem).Tag;

            ch.BeepSoundPlay = true;
            ch.NewMessages = false;
            ch.UserMessageLoadedIdx = -1;

            VisitedChannels.Remove(Channels.SelectedIndex);
            VisitedChannels.Add(Channels.SelectedIndex);


            if (!ch.IsPrivMsgChannel)
            {
                GameList.SelectedIndex = Channels.SelectedIndex;
                UserList.SelectedIndex = Channels.SelectedIndex;

                // Clear filter
                if (GameListChannel != null)
                {
                    var view = CollectionViewSource.GetDefaultView(GameListChannel.TheDataGrid.ItemsSource);
                    if (view != null && view.Filter != null)
                    {
                        Filter.Text = "Filter..";
                        view.Filter = null;
                    }
                }

                GameListChannel = ch;
                GameListForce = true;
            }

            UpdateDescription();

            if (ch.Joined)
            {
                FocusTimer.Start();
            }
        }

        private void UpdateDescription()
        {
            if (GameListChannel != null)
            {
                if (GameListChannel.Joined)
                    ChannelThings.Text = GameListChannel.Name + " | " + GameListChannel.Clients.Count + " users online";
                else
                    ChannelThings.Text = GameListChannel.Name;
            }
            else
                ChannelThings.Text = "";
        }

        #endregion

        // Buddy and Ignore things
        private void AddOrRemoveBuddy(object sender, RoutedEventArgs e)
        {
            var obj = sender as MenuItem;
            var contextMenu = obj.Parent as ContextMenu;
            var item = contextMenu.PlacementTarget as DataGrid;
            if (item.SelectedIndex != -1)
            {
                var client = item.SelectedItem as Client;
                if (client.IsBuddy)
                    WormNetM.RemoveBuddy(client.Name);
                else
                    WormNetM.AddBuddy(client.Name);
            }
            e.Handled = true;
        }

        private void AddOrRemoveBan(object sender, RoutedEventArgs e)
        {
            var obj = sender as MenuItem;
            var contextMenu = obj.Parent as ContextMenu;
            var dg = contextMenu.PlacementTarget as DataGrid;
            if (dg.SelectedIndex != -1)
            {
                var client = dg.SelectedItem as Client;
                if (client.IsBanned)
                {
                    WormNetM.RemoveBan(client.Name);
                    if (WormNetM.Clients.ContainsKey(client.LowerName))
                    {
                        foreach (var item in WormNetM.ChannelList)
                        {
                            if (!item.Value.IsPrivMsgChannel && item.Value.Clients.Contains(client))
                                LoadMessages(item.Value, GlobalManager.MaxMessagesDisplayed, true);
                            else if (item.Value.IsPrivMsgChannel && item.Value.TheClient == client)
                            {
                                AddToChannels(item.Value);
                                LoadMessages(item.Value, GlobalManager.MaxMessagesDisplayed, true);
                            }
                        }
                    }
                }
                else
                {
                    WormNetM.AddBan(client.Name);
                    if (WormNetM.Clients.ContainsKey(client.LowerName))
                    {
                        foreach (var item in WormNetM.ChannelList)
                        {
                            if (!item.Value.IsPrivMsgChannel && item.Value.Clients.Contains(client))
                                LoadMessages(item.Value, GlobalManager.MaxMessagesDisplayed, true);
                            else if (item.Value.IsPrivMsgChannel && item.Value.TheClient == client)
                                RemoveFromChannels(item.Value);
                        }
                    }
                }
            }
            e.Handled = true;
        }

        private void RemoveFromChannels(Channel channel)
        {
            for (int i = 0; i < Channels.Items.Count; i++)
            {
                Channel ch = (Channel)((TabItem)Channels.Items[i]).Tag;
                if (ch == channel)
                {
                    Channels.Items.RemoveAt(i);
                    return;
                }
            }
        }

        private void ContextMenuBuilding(object sender, ContextMenuEventArgs e)
        {
            var obj = sender as DataGrid;
            if (obj.SelectedItem != null)
            {
                var client = obj.SelectedItem as Client;
                bool tushandled = false;

                ((MenuItem)obj.ContextMenu.Items[0]).Tag = client;

                for (int i = 1; i < obj.ContextMenu.Items.Count; i++)
                {
                    var menuitem = obj.ContextMenu.Items[i] as MenuItem;
                    if (menuitem.Name == "Buddy")
                    {
                        if (client.IsBuddy)
                        {
                            menuitem.Header = "Remove from buddy list";
                        }
                        else
                        {
                            menuitem.Header = "Add to buddy list";
                        }
                    }
                    else if (menuitem.Name == "Ignore")
                    {
                        if (client.IsBanned)
                        {
                            menuitem.Header = "Remove from ignore list";
                        }
                        else
                        {
                            menuitem.Header = "Add to ignore list";
                        }
                    }
                    else if (menuitem.Name == "WiewTusProfile")
                    {
                        tushandled = true;
                        if (client.TusActive)
                            menuitem.Header = "View " + client.TusNick + "'s profile";
                        else
                        {
                            menuitem.Click -= WiewTusProfile;
                            obj.ContextMenu.Items.Remove(menuitem);
                        }
                    }
                }

                if (!tushandled && client.TusActive)
                {
                    MenuItem item = new MenuItem();
                    item.Name = "WiewTusProfile";
                    item.Header = "View " + client.TusNick + "'s profile";
                    item.Click += WiewTusProfile;
                    obj.ContextMenu.Items.Add(item);
                }
            }
        }

        private void ContextMenuClear(object sender, ContextMenuEventArgs e)
        {
            var obj = sender as DataGrid;
            for (int i = 0; i < obj.ContextMenu.Items.Count; i++)
            {
                var menuitem = obj.ContextMenu.Items[i] as MenuItem;
                if (menuitem.Name == "WiewTusProfile")
                {
                    menuitem.Click -= WiewTusProfile;
                    obj.ContextMenu.Items.Remove(menuitem);
                }
            }
        }

        #region Private chat UI things (open, close)

        // If we open a private chat
        private void PrivateMessageClick(object sender, MouseButtonEventArgs e)
        {
            var obj = sender as DataGrid;
            if (obj.SelectedItem != null)
            {
                Client c = obj.SelectedItem as Client;
                if (c.LowerName != GlobalManager.User.LowerName)
                {
                    OpenPrivateChat(c);
                }
            }
            e.Handled = true;
        }

        private void PrivateMessageClick2(object sender, RoutedEventArgs e)
        {
            Client c = (Client)((MenuItem)sender).Tag;
            if (c.LowerName != GlobalManager.User.LowerName)
            {
                OpenPrivateChat(c);
            }
            e.Handled = true;
        }


        private void OpenPrivateChat(Client client)
        {
            // Test if we already have an opened chat with the user
            for (int i = 0; i < Channels.Items.Count; i++)
            {
                Channel temp = (Channel)((TabItem)Channels.Items[i]).Tag;
                if (temp.LowerName == client.LowerName)
                {
                    Channels.SelectedIndex = i;
                    return;
                }
            }

            Channel ch = new Channel(client.Name, "Chat with " + client.Name, client);
            // If we open a private chat, then we didn't want to send old away message
            ch.AwaySent = true;
            ch.NewMessageAdded += AddNewMessage;

            MakeConnectedLayout(ch);
            WormNetM.ChannelList.Add(ch.LowerName, ch);
            AddToChannels(ch);

            // Select it
            for (int i = 0; i < Channels.Items.Count; i++)
            {
                Channel temp = (Channel)((TabItem)Channels.Items[i]).Tag;
                if (temp.LowerName == ch.LowerName)
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
                {
                    if (Channels.SelectedItem != null && (Channel)((TabItem)Channels.SelectedItem).Tag == ch) // Channel is selected
                    {
                        int index = Channels.SelectedIndex;
                        VisitedChannels.Remove(index);
                        for (int i = 0; i < VisitedChannels.Count; i++)
                        {
                            if (VisitedChannels[i] > index)
                                VisitedChannels[i]--;
                        }
                        int lastindex = VisitedChannels[VisitedChannels.Count - 1];

                        ch.Log(ch.Messages.Count, true);
                        WormNetM.ChannelList.Remove(ch.LowerName);
                        Channels.Items.RemoveAt(index);
                        Channels.SelectedIndex = lastindex;
                    }
                    else
                    {
                        int index = -1;
                        for (int i = 0; i < Channels.Items.Count; i++)
                        {
                            if ((Channel)((TabItem)Channels.Items[i]).Tag == ch)
                            {
                                index = i;
                                break;
                            }
                        }
                        VisitedChannels.Remove(index);
                        for (int i = 0; i < VisitedChannels.Count; i++)
                        {
                            if (VisitedChannels[i] > index)
                                VisitedChannels[i]--;
                        }
                        ch.Log(ch.Messages.Count, true);
                        WormNetM.ChannelList.Remove(ch.LowerName);
                        Channels.Items.RemoveAt(index);
                    }
                }
            }
        }

        // Close private chat
        private void PrivateChatClose(object sender, RoutedEventArgs e)
        {
            var obj = (MenuItem)sender;
            var ch = (Channel)(obj.DataContext);
            if (ch.IsPrivMsgChannel)
            {
                int index = 0, i = 0;
                foreach (var item in WormNetM.ChannelList)
                {
                    if (ch == item.Value)
                    {
                        index = i;
                        break;
                    }
                    i++;
                }

                VisitedChannels.Remove(index);
                for (i = 0; i < VisitedChannels.Count; i++)
                {
                    if (VisitedChannels[i] > index)
                        VisitedChannels[i]--;
                }
                int lastindex = VisitedChannels[VisitedChannels.Count - 1];

                ch.Log(ch.Messages.Count, true);
                WormNetM.ChannelList.Remove(ch.LowerName);
                Channels.Items.RemoveAt(index);
                Channels.SelectedIndex = lastindex;
            }
            e.Handled = true;
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
            if (GameListChannel != null && GameListChannel.Joined)
            {
                var obj = sender as TextBox;
                var view = CollectionViewSource.GetDefaultView(GameListChannel.TheDataGrid.ItemsSource);
                if (view != null)
                {
                    string[] filters = obj.Text.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < filters.Length; i++)
                        filters[i] = filters[i].Trim().ToLower();

                    if (filters.Length == 0)
                    {
                        view.Filter = null;
                    }
                    else
                    {
                        view.Filter = o =>
                        {
                            Client c = o as Client;
                            for (int i = 0; i < filters.Length; i++)
                            {
                                if (
                                    c.LowerName.Contains(filters[i])
                                    || c.TusActive && c.TusLowerNick.Contains(filters[i])
                                    || c.Clan.Length >= filters[i].Length && c.Clan.Substring(0, filters[i].Length).ToLower() == filters[i]
                                    || c.Country != null && c.Country.LowerName.Length >= filters[i].Length && c.Country.LowerName.Substring(0, filters[i].Length) == filters[i]
                                    || c.IsBuddy && "buddy".Length >= filters[i].Length && "buddy".Substring(0, filters[i].Length) == filters[i]
                                    || c.IsBanned && "ignored".Length >= filters[i].Length && "ignored".Substring(0, filters[i].Length) == filters[i]
                                    || c.Rank != null && c.Rank.LowerName.Length >= filters[i].Length && c.Rank.LowerName.Substring(0, filters[i].Length) == filters[i]
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
                Channel ch = (Channel)((TabItem)Channels.SelectedItem).Tag;
                ch.BeepSoundPlay = true;
                ch.NewMessages = false;
                ch.TheTextBox.Focus();
            }
            this.StopFlashingWindow();
        }

        // Need to know that if the window is activated to play beep sounds
        private void WindowDeactivated(object sender, EventArgs e)
        {
            IsWindowFocused = false;
        }

        private void ClientList_LostFocus(object sender, RoutedEventArgs e)
        {
            ((DataGrid)sender).SelectedIndex = -1;
            e.Handled = true;
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
            SnooperClosing = true;

            // Stop IRC thread
            if (IrcThread.IsAlive)
            {
                WormNetC.Disconnect();
                e.Cancel = true;
                return;
            }

            // Stop backgroundworkers
            if (StartGame.IsBusy)
            {
                StartGame.CancelAsync();
                e.Cancel = true;
                return;
            }
            if (HostGame.IsBusy)
            {
                HostGame.CancelAsync();
                e.Cancel = true;
                return;
            }
            if (TUSCommunicator.IsBusy)
            {
                TUSCommunicator.CancelAsync();
                e.Cancel = true;
                return;
            }
            if (WormWebC.LoadHostedGames.IsBusy)
            {
                WormWebC.Stop = true;
                WormWebC.LoadHostedGames.CancelAsync();
                e.Cancel = true;
                return;
            }
            
            // Log channel messages
            foreach (var item in WormNetM.ChannelList)
            {
                if (item.Value.Joined)
                    item.Value.Log(item.Value.Messages.Count, true);
            }

            // Serialize buddy list and ban list and save them
            var sb = new System.Text.StringBuilder();
            foreach (var item in WormNetM.BuddyList)
            {
                sb.Append(item.Value);
                sb.Append(',');
            }
            Properties.Settings.Default.BuddyList = sb.ToString();

            sb.Clear();
            foreach (var item in WormNetM.BanList)
            {
                sb.Append(item.Value);
                sb.Append(',');
            }
            Properties.Settings.Default.BanList = sb.ToString();
            Properties.Settings.Default.Save();

            // Stop the clock
            Clock.Stop();
        }

        // IRCThread will notify us when it is done. Then we close the window
        private void ConnectionState(IRCConnectionStates state)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                if (SnooperClosing)
                    this.Close();
                else if (state == IRCConnectionStates.OK) // Reconnect success
                {
                    LoadingRing.IsActive = false;
                    foreach (var item in WormNetM.ChannelList)
                    {
                        if (item.Value.Joined)
                        {
                            item.Value.TheTextBox.IsEnabled = true;
                            if (!item.Value.IsPrivMsgChannel)
                            {
                                WormNetC.JoinChannel(item.Value.Name);
                                WormNetC.GetChannelClients(item.Value.Name);
                            }
                        }
                    }
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

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs args)
        {
            ErrorLog.log(args.Exception);
        }

        private void OpenURL(object sender, RoutedEventArgs e)
        {
            try
            {
                var obj = (Button)sender;
                System.Diagnostics.Process.Start((string)obj.Tag);
            }
            catch (Exception ex)
            {
                ErrorLog.log(ex);
            }
            e.Handled = true;
        }

        private void WiewTusProfile(object sender, RoutedEventArgs e)
        {
            var obj = sender as MenuItem;
            var contextMenu = obj.Parent as ContextMenu;
            var item = contextMenu.PlacementTarget as DataGrid;
            var client = item.SelectedItem as Client;


            try
            {
                System.Diagnostics.Process.Start(client.TusLink);
            }
            catch (Exception ex)
            {
                ErrorLog.log(ex);
            }
            e.Handled = true;
        }
    }
}
