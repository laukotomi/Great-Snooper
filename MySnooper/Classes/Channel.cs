using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace MySnooper
{
    public class Channel : IComparable, INotifyPropertyChanged
    {
        private static System.Text.RegularExpressions.Regex DateRegex = new System.Text.RegularExpressions.Regex(@"[^0-9]");

        private readonly MainWindow mw;
        private StringBuilder sb;

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

        // Messages
        public List<MessageClass> Messages { get; private set; }
        public bool BeepSoundPlay { get; set; }
        public bool SendAway { get; set; }
        public bool SendBack { get; set; }
        public int MessagesLoadedFrom { get; set; }

        // Clients
        public SortedObservableCollection<Client> Clients { get; private set; }

        // GameList
        public SortedObservableCollection<Game> GameList { get; private set; }
        public DateTime GameListUpdatedTime { get; set; }

        // Up & Down keys
        private List<string> userMessages;
        public int UserMessageLoadedIdx { get; set; }
        public string TempMessage { get; set; } // We store the user message here when the user presses up or down keys to make it possible to restore the message

        // View variables
        public TabItem ChannelTabItem { get; private set; }
        public Border DisconnectedLayout { get; private set; }
        private Border connectedLayout;
        private TabItem gameListTabItem;
        private TextBlock header;
        public RichTextBox TheRichTextBox { get; private set; }
        public FlowDocument TheFlowDocument { get; private set; }
        public ListBox GameListBox { get; private set; }
        public TextBox TheTextBox { get; private set; }
        public DataGrid TheDataGrid { get; private set; }
        public bool ShowOlderMessagesInserted { get; private set; }

        public bool IsReconnecting { get; private set; }

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
                _newMessages = value;
                if (this.IsPrivMsgChannel)
                    GenerateHeader();
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
            set
            {
                _disabled = value;
                this.TheTextBox.IsReadOnly = value;
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

        public bool Joined
        {
            get { return _joined; }
            private set
            {
                _joined = value;
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
            this.MessagesLoadedFrom = 0;

            // Make channel layout
            this.ChannelTabItem = new TabItem();
            this.ChannelTabItem.DataContext = this;

            if (theClient != null)
            {
                this.IsPrivMsgChannel = true;
                this.Clients = new SortedObservableCollection<Client>();
                if (this.Name.Contains(","))
                {
                    string[] helper = this.Name.Split(new char[] { ',' });
                    foreach (string clientName in helper)
                    {
                        Client c = null;
                        if (!this.Server.Clients.TryGetValue(clientName.ToLower(), out c))
                        {
                            c = new Client(clientName);
                            c.IsBanned = mw.IsBanned(c.LowerName);
                            c.IsBuddy = mw.IsBuddy(c.LowerName);
                            c.OnlineStatus = 2;
                            this.Server.Clients.Add(c.LowerName, c);
                        }
                        
                        this.Clients.Add(c);
                        c.PMChannels.Add(this);
                        c.PropertyChanged += TheClient_PropertyChanged;
                    }
                }
                else
                {
                    this.Clients.Add(theClient);
                    theClient.PMChannels.Add(this);
                    theClient.PropertyChanged += TheClient_PropertyChanged;
                }


                this.ChannelTabItem.Style = (Style)mw.Channels.FindResource("PrivMsgTabItem");
                this.ChannelTabItem.ApplyTemplate();
                this.header = (TextBlock)this.ChannelTabItem.Template.FindName("ContentSite", this.ChannelTabItem);
                this.LoadConnectedLayout();
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

                this.gameListTabItem = mw.MakeGameListTabItem(this);
                mw.GameList.Items.Add(gameListTabItem);
            }

            this.GameListUpdatedTime = new DateTime(1999, 5, 31);
            this.BeepSoundPlay = true;
            this.SendAway = mw.AwayText != string.Empty;
            this.SendBack = false;
            this.IsLoading = false;
            this.UserMessageLoadedIdx = -1;
            this.ShowOlderMessagesInserted = false;
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
                    case 0:
                        if (isMouseOver || this.ChannelTabItem.IsSelected)
                            inline.Foreground = Brushes.Red;
                        else
                            inline.Foreground = Brushes.DarkRed;
                        break;
                    case 1:
                        if (isMouseOver || this.ChannelTabItem.IsSelected)
                            inline.Foreground = Brushes.YellowGreen;
                        else
                            inline.Foreground = Brushes.Green;
                        break;
                    case 2:
                        if (isMouseOver || this.ChannelTabItem.IsSelected)
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
            if (e.PropertyName == "OnlineStatus" || e.PropertyName == "Name")
            {
                GenerateHeader();
            }
            else if (this.Clients.Count == 1)
            {
                if (e.PropertyName == "IsBanned")
                {
                    Client c = (Client)sender;
                    if (c.IsBanned)
                    {
                        mw.CloseChannelTab(this, true);
                    }
                    else
                    {
                        mw.Channels.Items.Add(this.ChannelTabItem);
                    }
                }
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
            if (this.DisconnectedLayout != null)
            {
                this.ChannelTabItem.Content = this.DisconnectedLayout;
                return;
            }

            Border border = mw.MakeDisConnectedLayout(this);
            this.DisconnectedLayout = border;
            this.DisconnectedLayout.DataContext = this;
            this.ChannelTabItem.Content = this.DisconnectedLayout;
        }

        public void Join(WormageddonWebComm wormWebC)
        {
            if (Server.IsWormNet && Scheme == string.Empty)
            {
                this.Scheme = wormWebC.SetChannelScheme(this);
                if (this.CanHost && this.GameList == null)
                {
                    this.GameList = new SortedObservableCollection<Game>();
                    this.GameList.CollectionChanged += GameList_CollectionChanged;
                    ListBox lb = (ListBox)((Border)((Grid)this.gameListTabItem.Content).Children[1]).Child;
                    lb.ItemsSource = this.GameList;
                }
            }

            if (this.Clients == null)
            {
                this.Clients = new SortedObservableCollection<Client>();
                this.Clients.CollectionChanged += Clients_CollectionChanged;
                this.TheDataGrid.ItemsSource = this.Clients;
                if (!Properties.Settings.Default.ShowBannedUsers)
                {
                    var view = CollectionViewSource.GetDefaultView(this.TheDataGrid.ItemsSource);
                    if (view != null)
                    {
                        view.Filter = o =>
                        {
                            Client c = o as Client;
                            if (c.IsBanned)
                                return false;
                            return true;
                        };
                    }
                }

                if (Server.IsWormNet)
                {
                    string[] order = Properties.Settings.Default.ColumnOrder.Split(new char[] { '|' });
                    if (order.Length == 2)
                    {
                        ListSortDirection dir = order[1] == "D" ? ListSortDirection.Descending : ListSortDirection.Ascending;
                        mw.SetOrderForDataGrid(this, order[0], dir);
                    }
                    else
                        mw.SetOrderForDataGrid(this, "Nick", ListSortDirection.Ascending);
                }
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
            if (_clientCount != Clients.Count)
            {
                _clientCount = Clients.Count;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ClientCount"));
            }
        }


        // Add a message
        public void AddMessage(Client sender, string message, MessageSetting style, string highlightWord = "")
        {
            if (Messages.Count + 1 > GlobalManager.MaxMessagesInMemory)
            {
                Log(GlobalManager.NumOfOldMessagesToBeLoaded);
            }

            MessageClass msg = new MessageClass(sender, message, style);
            Messages.Add(msg);
            if (!sender.IsBanned || !IsPrivMsgChannel && Properties.Settings.Default.ShowBannedMessages)
                mw.AddNewMessage(this, msg, false, highlightWord);
        }

        public void ClearClients()
        {
            if (this.Clients != null)
            {
                for (int i = 0; i < this.Clients.Count; i++)
                {
                    Client c = this.Clients[i];
                    if (IsPrivMsgChannel)
                    {
                        c.PMChannels.Remove(this);
                        c.PropertyChanged -= TheClient_PropertyChanged;

                        if (!this._disabled && this.Clients.Count > 1 && c.OnlineStatus != 0 && this.Messages.Count > 0)
                        {
                            this.Server.Send("PRIVMSG " + c.Name + " :" + "\x01" + "CLEAVING " + this.HashName + "\x01");
                        }
                    }
                    else
                    {
                        c.Channels.Remove(this);
                    }

                    if (c.Channels.Count == 0)
                    {
                        if (c.PMChannels.Count > 0)
                            c.OnlineStatus = 2;
                        else
                            this.Server.Clients.Remove(c.LowerName);
                    }
                }

                Clients.Clear();
            }
        }

        public void Part()
        {
            ClearClients();

            if (this.Messages != null)
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

            if (this.IsLoading)
                this.Loading(false);
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
            return HashName.GetHashCode();
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
                StackPanel sp = (StackPanel)this.DisconnectedLayout.Child;
                sp.Children[0].Visibility = System.Windows.Visibility.Hidden;
                sp.Children[1].Visibility = System.Windows.Visibility.Hidden;
                ((ProgressRing)sp.Children[2]).IsActive = true;
            }
            else
            {
                StackPanel sp = (StackPanel)this.DisconnectedLayout.Child;
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
                    if (!IsPrivMsgChannel)
                        this.ClearClients();
                    this.LoadDisconnectedLayout();
                }
                this.Loading(true);
            }
            else
            {
                this.Loading(false);
                if (this.Joined)
                    this.LoadConnectedLayout();
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
                        mw.Channels.SelectedIndex = i;
                        this.Clients.Remove(c); // Undo modifications
                        return;
                    }
                }

                c.PMChannels.Add(this);
            }

            if (broadcast && this.Messages.Count > 0)
            {
                foreach (Client client in this.Clients)
                {
                    if (client.OnlineStatus != 0 && client != c)
                        this.Server.Send("PRIVMSG " + client.Name + " :" + "\x01" + "CLIENTADD " + this.HashName + "|" + c.Name + "\x01");
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
                        mw.Channels.SelectedIndex = i;
                        this.Clients.Add(c); // Undo
                        return;
                    }
                }

                c.PMChannels.Remove(this);

                if (c.Channels.Count == 0 && c.PMChannels.Count == 0)
                    this.Server.Clients.Remove(c.LowerName);
            }
            
            if (broadcast && this.Messages.Count > 0)
            {
                foreach (Client client in this.Clients)
                {
                    if (client.OnlineStatus != 0)
                        this.Server.Send("PRIVMSG " + client.Name + " :" + "\x01" + "CLIENTREM " + this.HashName + "|" + c.Name + "\x01");
                }

                if (c.OnlineStatus != 0)
                    this.Server.Send("PRIVMSG " + c.Name + " :" + "\x01" + "CLIENTREM " + this.HashName + "|" + c.Name + "\x01");
            }

            // update hashname
            this.Server.ChannelList.Remove(this.HashName);
            this.HashName = newHashName;
            this.Server.ChannelList.Add(this.HashName, this);
            GenerateHeader();
        }

        private string GetNewHashName()
        {
            if (sb == null)
                sb = new StringBuilder();
            else
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
