using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MySnooper
{
    public class Channel : IComparable, INotifyPropertyChanged
    {
        private static Regex DateRegex = new Regex(@"[^0-9]");

        private readonly MainWindow mw;
        private readonly StringBuilder sb = new StringBuilder();

        public IRCCommunicator Server { get; private set; }

        // Private variables to make properties work well
        private string _scheme = string.Empty;
        private bool _isPrivMsgChannel = false;
        private bool _newMessages = false;
        private int _clientCount = 0;
        private int _gameCount = 0;
        private Visibility _canHostVisibility = Visibility.Collapsed;
        private Visibility _leaveChannelVisibility = Visibility.Collapsed;
        private bool _canHost = false;
        private bool _joined = false;
        private string _name;
        private bool _disabled = false;

        // Channel Variables
        public string HashName { get; private set; }
        public string Description { get; private set; }
        public bool IsLoading { get; private set; }

        // Lists
        public List<MessageClass> Messages { get; private set; }
        public SortedObservableCollection<Client> Clients { get; private set; }
        public SortedObservableCollection<Game> GameList { get; private set; }

        // Away things
        public bool BeepSoundPlay { get; set; }
        public bool SendAway { get; set; }
        public bool SendBack { get; set; }

        // GameList
        public DateTime GameListUpdatedTime { get; set; }

        // Up & Down keys
        private List<string> userMessages;
        public int MessagesLoadedFrom { get; set; }
        public int UserMessageLoadedIdx { get; set; }
        public string TempMessage { get; set; } // We store the user message here when the user presses up or down keys to make it possible to restore the message

        // View variables
        public TabItem ChannelTabItem { get; private set; }
        public RichTextBox TheRichTextBox { get; private set; }
        public FlowDocument TheFlowDocument { get; private set; }
        public ListBox GameListBox { get; private set; }
        public TextBox TheTextBox { get; private set; }
        public DataGrid TheDataGrid { get; private set; }
        public Border disconnectedLayout;
        private Border connectedLayout;
        private TextBlock header;

        // Reconnecting
        public bool IsReconnecting { get; private set; }
        
        // EnergySaveMode
        public bool MessageReloadNeeded { get; set; } // When a message arrives while the snooper is in EnerySave mode

        // These properties may change and then they may notify the UI thread about that
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public string Scheme
        {
            get
            {
                return _scheme;
            }
            private set
            {
                _scheme = value;
                // Now we know if we can host or not in this channel
                CanHost = _scheme != string.Empty && !_scheme.Contains("Tf");
            }
        }
        public bool IsPrivMsgChannel
        {
            get
            {
                return _isPrivMsgChannel;
            }
            private set
            {
                _isPrivMsgChannel = value;
                if (_isPrivMsgChannel)
                {
                    Joined = true;
                }
            }
        }
        public bool NewMessages
        {
            get
            {
                return _newMessages;
            }
            set
            {
                if (_newMessages != value)
                {
                    _newMessages = value;
                    if (this.IsPrivMsgChannel)
                        GenerateHeader();
                }
            }
        }

        public string ClientCount
        {
            get
            {
                return _clientCount.ToString();
            }
        }

        public string GameCount
        {
            get
            {
                return _gameCount.ToString();
            }
        }

        public bool CanHost
        {
            get { return _canHost; }
            private set
            {
                _canHost = value;
                if (value && Joined)
                    CanHostVisibility = Visibility.Visible;
                else
                    CanHostVisibility = Visibility.Collapsed;
            }
        }

        public bool Disabled
        {
            get
            {
                return _disabled;
            }
            set
            {
                _disabled = value;
                this.TheTextBox.IsReadOnly = value;
            }
        }

        public bool Joined
        {
            get { return _joined; }
            private set
            {
                _joined = value;
                if (value)
                {
                    this.BeepSoundPlay = true;
                    this.GameListUpdatedTime = new DateTime(1999, 5, 31);
                    this.MessageReloadNeeded = false;
                    this.MessagesLoadedFrom = 0;
                    this.NewMessages = false;
                    this.SendAway = mw.AwayText != string.Empty;
                    this.SendBack = false;
                    this.UserMessageLoadedIdx = -1;
                }

                if (value && !IsPrivMsgChannel)
                {
                    if (CanHost)
                        CanHostVisibility = Visibility.Visible;
                    LeaveChannelVisibility = Visibility.Visible;
                }
                else
                {
                    CanHostVisibility = Visibility.Collapsed;
                    LeaveChannelVisibility = Visibility.Collapsed;
                }
            }
        }

        public Visibility CanHostVisibility
        {
            get { return _canHostVisibility; }
            private set
            {
                _canHostVisibility = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("CanHostVisibility"));
            }
        }

        public Visibility LeaveChannelVisibility
        {
            get { return _leaveChannelVisibility; }
            private set
            {
                _leaveChannelVisibility = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("LeaveChannelVisibility"));
            }
        }

        // Constructor
        public Channel(MainWindow mw, IRCCommunicator server, string name, string description, Client theClient = null)
        {
            this.mw = mw;
            this.Server = server;
            this.Name = name;
            this.Description = description;

            // Make channel layout
            this.ChannelTabItem = new TabItem();
            this.ChannelTabItem.DataContext = this;

            if (theClient != null)
            {
                this.Clients = new SortedObservableCollection<Client>();
                this.Clients.CollectionChanged += Clients_CollectionChanged;
                this.ChannelTabItem.Style = (Style)mw.Channels.FindResource("PrivMsgTabItem");
                this.ChannelTabItem.ApplyTemplate();
                this.header = (TextBlock)this.ChannelTabItem.Template.FindName("ContentSite", this.ChannelTabItem);
                this.LoadConnectedLayout();
                this.IsPrivMsgChannel = true;

                if (this.Name.Contains(","))
                {
                    string[] helper = this.Name.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string clientName in helper)
                    {
                        Client c = null;
                        if (!this.Server.Clients.TryGetValue(clientName.ToLower(), out c))
                            c = new Client(clientName, Server);
                        
                        this.Clients.Add(c);
                    }
                }
                else
                    this.Clients.Add(theClient);

                this.HashName = GetNewHashName();
                this.GenerateHeader();

                Border b = (Border)this.ChannelTabItem.Template.FindName("Border", this.ChannelTabItem);
                b.MouseEnter += b_MouseEnter;
                b.MouseLeave += b_MouseLeave;

                this.Messages = new List<MessageClass>(GlobalManager.MaxMessagesInMemory);
                this.userMessages = new List<string>(5);

                if (!theClient.IsBanned)
                    mw.Channels.Items.Add(this.ChannelTabItem);
            }
            else
            {
                this.ChannelTabItem.Header = this.Name;
                this.HashName = name.ToLower();
                this.ChannelTabItem.Style = (Style)mw.Channels.FindResource("ChannelTabItem");
                this.LoadDisconnectedLayout();
                if (Name != "#worms" || Properties.Settings.Default.ShowWormsChannel)
                    mw.Channels.Items.Add(this.ChannelTabItem);

                // Make user list layout
                this.TheDataGrid = mw.MakeUserListTemplate();
                this.TheDataGrid.DataContext = this;

                TabItem ti = new TabItem();
                ti.Content = this.TheDataGrid;
                mw.UserList.Items.Add(ti);

                TabItem gameListTabItem = mw.MakeGameListTabItem(this);
                GameListBox = (ListBox)((Border)((Grid)gameListTabItem.Content).Children[1]).Child;
                mw.GameList.Items.Add(gameListTabItem);
            }

            this.Server.ChannelList.Add(this.HashName, this);
        }

        void b_MouseLeave(object sender, MouseEventArgs e)
        {
            GenerateHeader();
        }

        void b_MouseEnter(object sender, MouseEventArgs e)
        {
            GenerateHeader(true);
        }

        public void GenerateHeader(bool isMouseOver = false)
        {
            /*
                <ControlTemplate.Triggers>
                    <DataTrigger Binding="{Binding NewMessages}" Value="true">
                        <Setter Property="FontWeight" Value="Bold" />
                    </DataTrigger>
                    <DataTrigger Binding="{Binding Path=TheClient.OnlineStatus}" Value="1">
                        <Setter Property="Foreground" Value="Green" />
                    </DataTrigger>
                    <Trigger Property="IsSelected" Value="true">
                        <Setter Property="Foreground" Value="GreenYellow"></Setter>
                    </Trigger>
                    <DataTrigger Binding="{Binding Path=TheClient.OnlineStatus}" Value="0">
                        <Setter Property="Foreground" Value="DarkRed" />
                    </DataTrigger>
                    <DataTrigger Binding="{Binding Path=TheClient.OnlineStatus}" Value="2">
                        <Setter Property="Foreground" Value="Yellow" />
                    </DataTrigger>
                    <Trigger Property="IsMouseOver" Value="true" SourceName="ContentSite">
                        <Setter Property="Foreground" Value="{DynamicResource GrayHoverBrush}"></Setter>
                    </Trigger>
                </ControlTemplate.Triggers>
            */
            int i = 0;

            if (this.header.Inlines.Count != Clients.Count * 2 - 1)
            {
                this.header.Inlines.Clear();
                foreach (Client c in this.Clients)
                {
                    this.header.Inlines.Add(new Run(c.Name));
                    if (i + 1 < this.Clients.Count)
                        this.header.Inlines.Add(new Run(" | ") { Foreground = Brushes.Gray });
                    i++;
                }
            }

            i = 0;
            int j = 0;
            foreach (Inline inline in this.header.Inlines)
            {
                if (i % 2 == 1)
                {
                    i++;
                    continue;
                }

                if (this.NewMessages)
                    inline.FontWeight = FontWeights.Bold;
                else
                    inline.FontWeight = FontWeights.Normal;

                switch (this.Clients[j].OnlineStatus)
                {
                    case Client.Status.Offline:
                        if (this.ChannelTabItem.IsSelected)
                            inline.Foreground = Brushes.Red;
                        else if (isMouseOver)
                            inline.Foreground = Brushes.Firebrick;
                        else
                            inline.Foreground = Brushes.DarkRed;
                        break;
                    case Client.Status.Online:
                        if (this.ChannelTabItem.IsSelected)
                            inline.Foreground = Brushes.GreenYellow;
                        else if (isMouseOver)
                            inline.Foreground = Brushes.YellowGreen;
                        else
                            inline.Foreground = Brushes.Green;
                        break;
                    case Client.Status.Unknown:
                        if (this.ChannelTabItem.IsSelected)
                            inline.Foreground = Brushes.Goldenrod;
                        else if (isMouseOver)
                            inline.Foreground = Brushes.LightYellow;
                        else
                            inline.Foreground = Brushes.Yellow;
                        break;
                }
                i++;
                j++;
            }
        }

        private void TheClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "OnlineStatus":
                case "Name":
                    GenerateHeader();
                    break;

                case "IsBanned":
                    if (this.Clients.Count == 1)
                    {
                        Client c = (Client)sender;
                        if (c.IsBanned)
                            mw.CloseChannelTab(this, true);
                        else
                            mw.Channels.Items.Add(this.ChannelTabItem);
                    }
                    else
                        GenerateHeader();
                    break;
            }

        }

        private void LoadConnectedLayout()
        {
            if (this.connectedLayout != null)
            {
                this.ChannelTabItem.Content = this.connectedLayout;
                return;
            }

            Border border = mw.MakeConnectedLayout();
            this.connectedLayout = border;
            this.TheRichTextBox = (RichTextBox)((ScrollViewer)((Border)((Grid)border.Child).Children[0]).Child).Content;
            this.TheFlowDocument = (FlowDocument)this.TheRichTextBox.Document;
            this.TheTextBox = (TextBox)((Grid)border.Child).Children[1];
            this.ChannelTabItem.Content = this.connectedLayout;
        }

        private void LoadDisconnectedLayout()
        {
            if (this.disconnectedLayout != null)
            {
                this.ChannelTabItem.Content = this.disconnectedLayout;
                return;
            }

            Border border = mw.MakeDisConnectedLayout(this);
            this.disconnectedLayout = border;
            this.disconnectedLayout.DataContext = this;
            this.ChannelTabItem.Content = this.disconnectedLayout;
        }

        public void Join()
        {
            if (Server.IsWormNet && Scheme == string.Empty)
            {
                this.Scheme = mw.SetChannelScheme(this);
                if (this.CanHost && this.GameList == null)
                {
                    this.GameList = new SortedObservableCollection<Game>();
                    this.GameList.CollectionChanged += GameList_CollectionChanged;
                    this.GameListBox.ItemsSource = this.GameList;
                }
            }

            if (this.Clients == null)
            {
                this.Clients = new SortedObservableCollection<Client>();
                this.Clients.CollectionChanged += Clients_CollectionChanged;
                this.TheDataGrid.ItemsSource = this.Clients;
                mw.SetDefaultViewForChannel(this);
                mw.SetDefaultOrderForChannel(this);
            }

            if (this.Messages == null)
                this.Messages = new List<MessageClass>(GlobalManager.MaxMessagesInMemory);
            if (this.userMessages == null)
                userMessages = new List<string>(5);

            this.LoadConnectedLayout();
            Joined = true;
            this.Loading(false);
        }

        void GameList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_gameCount != GameList.Count)
            {
                _gameCount = GameList.Count;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("GameCount"));
            }
        }

        void Clients_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (IsPrivMsgChannel)
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    Client c = (Client)e.NewItems[0];
                    c.PMChannels.Add(this);
                    c.PropertyChanged += TheClient_PropertyChanged;
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    Client c = (Client)e.OldItems[0];
                    c.PMChannels.Remove(this);
                    c.PropertyChanged -= TheClient_PropertyChanged;
                }
            }
            else
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    Client c = (Client)e.NewItems[0];
                    c.Channels.Add(this);
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    Client c = (Client)e.OldItems[0];
                    c.Channels.Remove(this);
                }

                if (_clientCount != Clients.Count)
                {
                    _clientCount = Clients.Count;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("ClientCount"));
                }
            }
        }

        // Add a message
        public void AddMessage(Client sender, string message, MessageSetting style)
        {
            if (Messages.Count + 1 > GlobalManager.MaxMessagesInMemory)
            {
                Log(GlobalManager.NumOfOldMessagesToBeLoaded);
            }

            MessageClass msg = new MessageClass(sender, message, style);
            Messages.Add(msg);

            if (!sender.IsBanned || !IsPrivMsgChannel && Properties.Settings.Default.ShowBannedMessages)
            {
                if (mw.EnergySaveModeOn)
                    this.MessageReloadNeeded = true;
                else
                    mw.AddNewMessage(this, msg, false);
            }
        }

        public void AddMessage(MessageClass msg)
        {
            if (Messages.Count + 1 > GlobalManager.MaxMessagesInMemory)
            {
                Log(GlobalManager.NumOfOldMessagesToBeLoaded);
            }

            Messages.Add(msg);

            if (!msg.Sender.IsBanned || !IsPrivMsgChannel && Properties.Settings.Default.ShowBannedMessages)
            {
                if (mw.EnergySaveModeOn)
                    this.MessageReloadNeeded = true;
                else
                    mw.AddNewMessage(this, msg, false);
            }
        }

        private void ClearClients()
        {
            for (int i = 0; i < this.Clients.Count; i++)
            {
                Client c = this.Clients[i];
                if (IsPrivMsgChannel)
                {
                    c.PMChannels.Remove(this);
                    c.PropertyChanged -= TheClient_PropertyChanged;

                    if (!this._disabled && this.Clients.Count > 1 && c.OnlineStatus != Client.Status.Offline && this.Messages.Count > 0)
                        this.Server.SendCTCPMessage(c.Name, "CLEAVING", this.HashName);
                }
                else
                    c.Channels.Remove(this);

                if (c.Channels.Count == 0)
                {
                    if (c.PMChannels.Count > 0)
                        c.OnlineStatus = Client.Status.Unknown;
                    else
                        this.Server.Clients.Remove(c.LowerName);
                }
            }

            Clients.Clear();
        }

        public void Part()
        {
            if (this.Clients != null)
                ClearClients();

            if (this.Messages != null && this.Messages.Count > 0)
            {
                this.AddMessage(this.Server.User, "has left the channel.", MessageSettings.PartMessage);
                Log(Messages.Count, true);
                Messages.Clear();
            }

            if (GameList != null)
                GameList.Clear();
            if (userMessages != null)
                userMessages.Clear();
            if (TheFlowDocument != null)
                TheFlowDocument.Blocks.Clear();

            if (!IsPrivMsgChannel)
            {
                this.LoadDisconnectedLayout();
                Joined = false;
            }

            if (this.IsReconnecting)
                this.Reconnecting(false);
            else if (this.IsLoading)
                this.Loading(false);

            if (this.Server.IsWormNet)
            {
                this.Server.LeaveChannel(this.Name);
            }
            else
            {
                mw.GameSurgeIsConnected = false;
                this.Server.Cancel = true;
            }
        }

        public void UserMessagesAdd(string message)
        {
            if (userMessages.Count == 0 || userMessages[0] != message)
            {
                if (userMessages.Count == userMessages.Capacity)
                    userMessages.RemoveAt(userMessages.Count - 1);
                userMessages.Insert(0, message);
            }
        }

        public string LoadNextUserMessage()
        {
            if (UserMessageLoadedIdx + 1 == userMessages.Count)
            {
                UserMessageLoadedIdx++;
                return string.Empty;
            }
            else if (UserMessageLoadedIdx + 1 > userMessages.Count)
                return string.Empty;

            return userMessages[++UserMessageLoadedIdx];
        }

        public string LoadPrevUserMessage()
        {
            if (UserMessageLoadedIdx - 1 == -1)
            {
                UserMessageLoadedIdx--;
                return string.Empty;
            }
            else if (UserMessageLoadedIdx - 1 < -1)
                return string.Empty;

            return userMessages[--UserMessageLoadedIdx];
        }


        public void Log(int db, bool makeend = false)
        {
            if (db == 0) return;

            if (!Directory.Exists(GlobalManager.SettingsPath + @"\Logs\" + Name))
                Directory.CreateDirectory(GlobalManager.SettingsPath + @"\Logs\" + Name);

            string logpath = GlobalManager.SettingsPath + @"\Logs\" + Name + "\\" + DateRegex.Replace(DateTime.Now.ToString("d"), "-") + ".log";

            using (StreamWriter writer = new StreamWriter(logpath, true)) 
            {
                for (int i = 0; i < db && Messages.Count > 0; i++)
                {
                    MessageClass msg = Messages[0];
                    writer.WriteLine("(" + msg.Style.Type.ToString() + ") " + msg.Time.ToString("G") + " " + msg.Sender.Name + ": " + msg.Message);
                    Messages.RemoveAt(0);
                }

                if (makeend)
                {
                    writer.WriteLine(DateTime.Now.ToString("G") + " - Channel closed.");
                    writer.WriteLine("-----------------------------------------------------------------------------------------");
                    writer.WriteLine(Environment.NewLine + Environment.NewLine);
                }
            }
        }


        // IComparable interface
        public int CompareTo(object obj)
        {
            var obj2 = obj as Channel;
            int first = _isPrivMsgChannel.CompareTo(obj2._isPrivMsgChannel);
            if (first != 0)
                return first;
            return HashName.CompareTo(obj2.HashName);
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }
            
            // If parameter cannot be cast to Point return false.
            Channel ch = obj as Channel;
            if ((System.Object)ch == null)
            {
                return false;
            }

            // Return true if the fields match:
            return HashName == ch.HashName && Server == ch.Server;
        }

        public bool Equals(Channel ch)
        {
            // If parameter is null return false:
            if ((object)ch == null)
            {
                return false;
            }
            
            // Return true if the fields match:
            return HashName == ch.HashName && Server == ch.Server;
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136261;
                // Suitable nullity checks etc, of course :)
                hash = hash * 16777619 ^ HashName.GetHashCode();
                hash = hash * 16777619 ^ Server.IsWormNet.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Channel a, Channel b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.HashName == b.HashName && a.Server == b.Server;
        }

        public static bool operator !=(Channel a, Channel b)
        {
            return !(a == b);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Loading(bool loading)
        {
            this.IsLoading = loading;
            if (loading)
            {
                StackPanel sp = (StackPanel)this.disconnectedLayout.Child;
                sp.Children[0].Visibility = System.Windows.Visibility.Hidden;
                sp.Children[1].Visibility = System.Windows.Visibility.Hidden;
                ((ProgressRing)sp.Children[2]).IsActive = true;
            }
            else
            {
                StackPanel sp = (StackPanel)this.disconnectedLayout.Child;
                sp.Children[0].Visibility = System.Windows.Visibility.Visible;
                sp.Children[1].Visibility = System.Windows.Visibility.Visible;
                ((ProgressRing)sp.Children[2]).IsActive = false;
            }
        }

        public void Reconnecting(bool reconnecting)
        {
            this.IsReconnecting = reconnecting;
            if (reconnecting)
            {
                if (this.Joined)
                {
                    if (!IsPrivMsgChannel && this.Clients != null)
                        this.ClearClients();
                    this.LoadDisconnectedLayout();
                    this.LeaveChannelVisibility = Visibility.Collapsed;
                }
                this.Loading(true);
            }
            else
            {
                this.Loading(false);
                if (this.Joined)
                {
                    this.LoadConnectedLayout();
                    this.LeaveChannelVisibility = Visibility.Visible;
                }
            }
        }

        public bool IsInConversation(Client client)
        {
            foreach (Client c in this.Clients)
            {
                if (c == client)
                    return true;
            }
            return false;
        }

        public void AddClientToConversation(Client c, bool broadcast = true, bool canModifyChannel = true)
        {
            this.Clients.Add(c);
            string newHashName = this.GetNewHashName();

            if (canModifyChannel)
            {
                // Test if we already have an opened chat with the user
                for (int i = 0; i < mw.Channels.Items.Count; i++)
                {
                    Channel temp = (Channel)((TabItem)mw.Channels.Items[i]).DataContext;
                    if (temp.HashName == newHashName && temp.Server == this.Server)
                    {
                        if (mw.Channels.SelectedIndex != i)
                            mw.Channels.SelectedIndex = i;
                        else
                            temp.TheTextBox.Focus();
                        this.Clients.Remove(c); // Undo modifications
                        return;
                    }
                }
            }

            if (broadcast && this.Messages.Count > 0)
            {
                foreach (Client client in this.Clients)
                {
                    if (client.OnlineStatus != Client.Status.Offline && client != c)
                        this.Server.SendCTCPMessage(client.Name, "CLIENTADD", this.HashName + "|" + c.Name);
                }
            }

            // update hashname
            this.Server.ChannelList.Remove(this.HashName);
            this.HashName = newHashName;
            this.Server.ChannelList.Add(this.HashName, this);
            GenerateHeader();
        }

        public void RemoveClientFromConversation(Client c, bool broadcast = true, bool canModifyChannel = true)
        {
            if (!this.IsInConversation(c))
                return;

            this.Clients.Remove(c);
            string newHashName = this.GetNewHashName();

            if (canModifyChannel)
            {
                // Test if we already have an opened chat with the user
                for (int i = 0; i < mw.Channels.Items.Count; i++)
                {
                    Channel temp = (Channel)((TabItem)mw.Channels.Items[i]).DataContext;
                    if (temp.HashName == newHashName && temp.Server == this.Server)
                    {
                        if (mw.Channels.SelectedIndex != i)
                            mw.Channels.SelectedIndex = i;
                        else
                            temp.TheTextBox.Focus();
                        this.Clients.Add(c); // Undo
                        return;
                    }
                }

                if (c.Channels.Count == 0 && c.PMChannels.Count == 0)
                    this.Server.Clients.Remove(c.LowerName);
            }
            
            if (broadcast && this.Messages.Count > 0)
            {
                foreach (Client client in this.Clients)
                {
                    if (client.OnlineStatus != Client.Status.Offline)
                        this.Server.SendCTCPMessage(client.Name, "CLIENTREM", this.HashName + "|" + c.Name);
                }

                if (c.OnlineStatus != Client.Status.Offline)
                    this.Server.SendCTCPMessage(c.Name, "CLIENTREM", this.HashName + "|" + c.Name);
            }

            // update hashname
            this.Server.ChannelList.Remove(this.HashName);
            this.HashName = newHashName;
            this.Server.ChannelList.Add(this.HashName, this);
            GenerateHeader();
        }

        private string GetNewHashName()
        {
            sb.Clear();

            for (int i = 0; i < this.Clients.Count; i++)
            {
                sb.Append(this.Clients[i].Name);
                if (i + 1 < this.Clients.Count)
                    sb.Append(",");
            }
            return sb.ToString();
        }
    }
}
