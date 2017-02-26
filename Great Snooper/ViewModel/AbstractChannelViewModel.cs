using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Channel;
using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.IRCTasks;
using GreatSnooper.Model;
using GreatSnooper.UserControls;

namespace GreatSnooper.ViewModel
{
    [DebuggerDisplay("{Name}")]
    public abstract class AbstractChannelViewModel : ViewModelBase, IComparable, IDisposable
    {
        private Border _connectedLayout;
        protected RichTextBox _rtb;
        protected FlowDocument _rtbDocument;
        protected TabItem _tabitem;
        private ContextMenu _instantColorMenu;

        private LastUserMessages _lastUserMessages = new LastUserMessages();
        private ChannelLogger _channelLogger = new ChannelLogger();

        private LinkedListNode<Message> _lastMessageLoaded;
        private LinkedListNode<Message> _messagesLoadedFrom;
        private bool _stopLoadingMessages;

        private bool _disabled;
        private bool _isHighlighted;
        private bool _isTBFocused;
        private bool _joined;
        private bool _loading;

        protected AbstractChannelViewModel(MainViewModel mainViewModel, AbstractCommunicator server)
        {
            this.MainViewModel = mainViewModel;
            this.Server = server;
            this.Messages = new LinkedList<Message>();
            this.Users = new SortedObservableCollection<User>();
            this.MessageText = string.Empty;
        }

        public ChannelTabControlViewModel ChannelTabVM { get; set; }

        public bool Disabled
        {
            get
            {
                return this._disabled;
            }
            set
            {
                if (this._disabled != value)
                {
                    this._disabled = value;
                    this.RaisePropertyChanged("Disabled");
                }
            }
        }

        public bool HiddenMessagesInEnergySaveMode { get; private set; }

        public bool IsHighlighted
        {
            get
            {
                return this._isHighlighted;
            }
            set
            {
                if (this._isHighlighted != value)
                {
                    this._isHighlighted = value;
                    this.RaisePropertyChanged("IsHighlighted");
                }
            }
        }

        public bool IsTBFocused
        {
            get
            {
                return this._isTBFocused;
            }
            set
            {
                if (this._isTBFocused != value)
                {
                    this._isTBFocused = value;
                    this.RaisePropertyChanged("IsTBFocused");
                    this._isTBFocused = false;
                    this.RaisePropertyChanged("IsTBFocused");
                }
            }
        }

        public bool Joined
        {
            get
            {
                return this._joined;
            }
            protected set
            {
                if (this._joined != value)
                {
                    this._joined = value;

                    if (value == false)
                    {
                        // Reset everything to default value
                        this._channelLogger.EndLogging();
                        this._messagesLoadedFrom = null;
                        this.Disabled = false;
                        this.Loading = false;
                        this.IsHighlighted = false;
                        this._lastUserMessages.Reset();
                        this._lastMessageLoaded = null;
                        this.MessageText = string.Empty;
                        this.HiddenMessagesInEnergySaveMode = false;
                        this._stopLoadingMessages = false;
                    }
                    else if (this._connectedLayout == null)
                    {
                        this.InitConnectedLayout();
                    }
                    this.JoinedChanged();
                    this.RaisePropertyChanged("Joined");
                }
            }
        }

        public bool Loading
        {
            get
            {
                return this._loading;
            }
            set
            {
                if (this._loading != value)
                {
                    this._loading = value;
                    this.RaisePropertyChanged("Loading");
                }
            }
        }

        public MainViewModel MainViewModel { get; private set; }

        public LinkedList<Message> Messages { get; private set; }

        public string MessageText { get; set; }

        public RelayCommand<KeyEventArgs> MsgKeyDownCommand
        {
            get
            {
                return new RelayCommand<KeyEventArgs>(MsgKeyDown);
            }
        }

        public RelayCommand<KeyEventArgs> MsgPreviewKeyDownCommand
        {
            get
            {
                return new RelayCommand<KeyEventArgs>(MsgPreviewKeyDown);
            }
        }

        public string Name
        {
            get;
            protected set;
        }

        public AbstractCommunicator Server
        {
            get;
            private set;
        }

        public SortedObservableCollection<User> Users
        {
            get;
            private set;
        }

        protected Border ConnectedLayout
        {
            get
            {
                if (this._connectedLayout == null)
                {
                    this.InitConnectedLayout();
                }
                return this._connectedLayout;
            }
        }

        public void AddMessage(User sender, string message, MessageSetting messageSetting)
        {
            var msg = new Message(sender, message, messageSetting, DateTime.Now);
            this.AddMessage(msg);
        }

        public void AddMessage(Message msg)
        {
            this._channelLogger.LogMessage(msg, this.Name);
            if (this.Messages.Count >= GlobalManager.MaxMessagesInMemory && this.MainViewModel.IsGameWindowOn() == false)
            {
                ClearMessages();
            }

            this.Messages.AddLast(msg);

            if (!msg.Sender.IsBanned || this.GetType() == typeof(ChannelViewModel) && Properties.Settings.Default.ShowBannedMessages)
            {
                if (this.MainViewModel.IsEnergySaveMode)
                {
                    this.HiddenMessagesInEnergySaveMode = true;
                }
                else
                {
                    this._lastMessageLoaded = this.Messages.Last;
                    if (this._messagesLoadedFrom == null)
                    {
                        this._messagesLoadedFrom = this.Messages.Last;
                    }
                    this.AddMessageToUI(msg);
                }
            }
        }

        public void ChangeMessageColorForUser(User u, SolidColorBrush color)
        {
            if (!this.Joined || this._rtbDocument == null || this._rtbDocument.Blocks.Count == 0)
            {
                return;
            }

            FontStyle fontStyle = u.Group.ID != UserGroups.SystemGroupID ? FontStyles.Italic : FontStyles.Normal;
            Paragraph p = this._rtbDocument.Blocks.FirstBlock as Paragraph;

            while (p != null)
            {
                var msg = (Message)p.Tag;
                if (msg.Sender == u)
                {
                    if (Properties.Settings.Default.MessageTime)
                    {
                        // p.Inlines.FirstInline.Foreground = (color != null) ? color : MessageSettings.MessageTimeStyle.NickColor;
                        p.Inlines.FirstInline.NextInline.Foreground = (color != null) ? color : msg.Style.NickColor;
                        p.Inlines.FirstInline.NextInline.FontStyle = fontStyle;
                    }
                    else
                    {
                        p.Inlines.FirstInline.Foreground = (color != null) ? color : msg.Style.NickColor;
                        p.Inlines.FirstInline.FontStyle = fontStyle;
                    }
                }
                p = (Paragraph)p.NextBlock;
            }


            foreach (var chvm in this.MainViewModel.AllChannels)
            {
                if (chvm.Joined && chvm._rtbDocument.Blocks.Count > 0)
                {
                }
            }
        }

        public abstract void ClearUsers();

        public int CompareTo(object obj)
        {
            var o = (AbstractChannelViewModel)obj;
            return this.Name.CompareTo(o.Name);
        }

        public abstract TabItem GetLayout();

        public void Highlight()
        {
            if (this.MainViewModel.IsWindowActive == false || this.MainViewModel.SelectedChannel != this)
            {
                this.IsHighlighted = true;
                if (this is PMChannelViewModel)
                {
                    ((PMChannelViewModel)this).GenerateHeader();
                }
            }
        }

        public int LoadMessages(int count, bool clear = false)
        {
            if (clear)
            {
                this._rtbDocument.Blocks.Clear();
            }

            // select the index from which the messages will be loaded
            if (clear)
            {
                this._messagesLoadedFrom = this.Messages.Last;
                this._lastMessageLoaded = this.Messages.Last;
            }
            else
            {
                this._messagesLoadedFrom = this._messagesLoadedFrom.Previous;
            }

            // load the message backwards
            int k = 0;
            while (true)
            {
                var msg = _messagesLoadedFrom.Value;
                if (!msg.Sender.IsBanned || Properties.Settings.Default.ShowBannedMessages)
                {
                    if (AddMessageToUI(msg, false))
                    {
                        k++;
                        if (k == count)
                        {
                            break;
                        }
                    }
                }
                if (this._messagesLoadedFrom.Previous == null)
                {
                    break;
                }
                this._messagesLoadedFrom = this._messagesLoadedFrom.Previous;
            }

            return k;
        }

        public void LoadNewMessages()
        {
            try
            {
                while (true)
                {
                    if (this._lastMessageLoaded == null || _lastMessageLoaded.Previous == null && _lastMessageLoaded.Next == null) // or is removed
                    {
                        this._lastMessageLoaded = this.Messages.First;
                    }
                    else
                    {
                        this._lastMessageLoaded = this._lastMessageLoaded.Next;
                    }

                    if (this._lastMessageLoaded == null)
                    {
                        break;
                    }

                    var msg = _lastMessageLoaded.Value;
                    if (!msg.Sender.IsBanned || Properties.Settings.Default.ShowBannedMessages)
                    {
                        AddMessageToUI(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
            this.HiddenMessagesInEnergySaveMode = false;
        }

        public abstract void ProcessMessage(MessageTask msgTask);

        public abstract void SendActionMessage(string message);

        public abstract void SendCTCPMessage(string ctcpCommand, string ctcpText, User except = null);

        public abstract void SendMessage(string message);

        public abstract void SendNotice(string message);

        public abstract void SetLoading(bool loading = true);

        protected virtual void JoinedChanged()
        {
        }

        private void AddColorChoosed(object sender, RoutedEventArgs e)
        {
            MenuItem obj = (MenuItem)sender;
            User u = ((Message)((ContextMenu)obj.Parent).Tag).Sender;
            SolidColorBrush color = (SolidColorBrush)obj.Foreground;

            this.MainViewModel.InstantColors[u.Name] = color;
            ChangeMessageColorForUser(u, color);
        }

        private bool AddMessageToUI(Message msg, bool add = true)
        {
            if (Properties.Settings.Default.ChatMode && (
                    msg.Style.Type == Message.MessageTypes.Part ||
                    msg.Style.Type == Message.MessageTypes.Join ||
                    msg.Style.Type == Message.MessageTypes.Quit))
            {
                return false;
            }

            try
            {
                Paragraph p = new Paragraph();
                MessageSettings.LoadSettingsFor(p, msg.Style);
                p.Foreground = msg.Style.MessageColor;
                p.Padding = new Thickness(0, 2, 0, 2);
                p.Tag = msg;
                p.MouseRightButtonDown += InstantColorMenu;

                // Time when the message arrived
                if (Properties.Settings.Default.MessageTime)
                {
                    Run word = new Run(msg.Time.ToString("T") + " ");
                    MessageSettings.LoadSettingsFor(word, MessageSettings.MessageTimeStyle);
                    word.Foreground = MessageSettings.MessageTimeStyle.NickColor;
                    p.Inlines.Add(word);
                }

                // Sender of the message
                Run nick = (msg.Style.Type == Message.MessageTypes.Action) ? new Run(msg.Sender.Name + " ") : new Run(msg.Sender.Name + ": ");
                msg.NickRun = nick;
                p.Inlines.Add(nick);

                // Message content
                if (msg.HighlightWords == null)
                {
                    p.Inlines.Add(new Run(msg.Text));
                }
                else
                {
                    int idx = 0;
                    foreach (var item in msg.HighlightWords)
                    {
                        if (item.Key != idx)
                        {
                            var part = msg.Text.Substring(idx, item.Key - idx);
                            p.Inlines.Add(new Run(part));
                            idx += part.Length;
                        }

                        var hword = msg.Text.Substring(idx, item.Value.Key);
                        if (item.Value.Value == Message.HightLightTypes.Highlight)
                        {
                            Run word = new Run(hword);
                            MessageSettings.LoadSettingsFor(word, MessageSettings.LeagueFoundMessage);
                            word.Foreground = MessageSettings.LeagueFoundMessage.NickColor;
                            p.Inlines.Add(word);
                        }
                        else if (item.Value.Value == Message.HightLightTypes.URI)
                        {
                            Hyperlink word = new Hyperlink(new Run(hword));
                            MessageSettings.LoadSettingsFor(word, MessageSettings.HyperLinkStyle);
                            word.Foreground = MessageSettings.HyperLinkStyle.NickColor;
                            word.Click += OpenLinkClick;
                            word.CommandParameter = hword;
                            p.Inlines.Add(word);
                        }
                        else
                        {
                            Run word = new Run(hword);
                            MessageSettings.LoadSettingsFor(word, MessageSettings.LeagueFoundMessage);
                            word.Foreground = MessageSettings.LeagueFoundMessage.NickColor;
                            p.Inlines.Add(word);
                        }
                        idx += item.Value.Key;
                    }

                    if (idx != msg.Text.Length)
                    {
                        var part = msg.Text.Substring(idx, msg.Text.Length - idx);
                        p.Inlines.Add(new Run(part));
                        idx += part.Length;
                    }
                }

                // Insert the new paragraph
                if (add == false && this._rtbDocument.Blocks.Count > 0)
                    this._rtbDocument.Blocks.InsertBefore(this._rtbDocument.Blocks.FirstBlock, p);
                else
                {
                    this._rtbDocument.Blocks.Add(p);
                }

                return true;
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }
            return false;
        }

        private void ClearMessages()
        {
            try
            {
                while (this.Messages.Count > GlobalManager.MaxMessagesInMemory)
                {
                    Message msg = this.Messages.First.Value;
                    this.Messages.RemoveFirst();

                    if (this._messagesLoadedFrom != null && this._messagesLoadedFrom.Value == msg) // If the removed message was displayed (when the scrollbar is not at bottom)
                    {
                        this._rtbDocument.Blocks.Remove(this._rtbDocument.Blocks.FirstBlock);
                        this.SetMessagesLoadedFrom();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }

        private void SetMessagesLoadedFrom()
        {
            this._messagesLoadedFrom = this.Messages.First;

            if (Properties.Settings.Default.ChatMode)
            {
                while (this._messagesLoadedFrom != null)
                {
                    var type = this._messagesLoadedFrom.Value.Style.Type;
                    if (type != Message.MessageTypes.Part && type != Message.MessageTypes.Join && type != Message.MessageTypes.Quit)
                    {
                        break;
                    }
                    this._messagesLoadedFrom = this._messagesLoadedFrom.Next;
                }
            }
        }

        private void InitConnectedLayout()
        {
            _connectedLayout = new ConnectedLayout(this);
            var sw = (ScrollViewer)((Border)((Grid)_connectedLayout.Child).Children[0]).Child;
            sw.ScrollChanged += MessageScrollChanged;
            _rtb = (RichTextBox)sw.Content;
            _rtbDocument = _rtb.Document;
        }

        private void InstantColorMenu(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!this._rtb.Selection.IsEmpty)
            {
                return;
            }

            if (_instantColorMenu == null)
            {
                _instantColorMenu = new ContextMenu();

                var def = new MenuItem()
                {
                    Header = Localizations.GSLocalization.Instance.DefaultText,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12
                };
                def.Click += RemoveInstantColor;
                _instantColorMenu.Items.Add(def);

                string[] goodcolors = { "Aquamarine", "Bisque", "BlueViolet", "BurlyWood", "CadetBlue", "Chocolate", "CornflowerBlue", "Gold", "GreenYellow", "LightCoral", "Pink", "Plum", "Red", "Sienna", "Violet", "White" };
                // populate colors drop down (will work with other kinds of list controls)
                Type colors = typeof(Colors);
                PropertyInfo[] colorInfo = colors.GetProperties(BindingFlags.Public | BindingFlags.Static);
                int found = 0;
                foreach (PropertyInfo info in colorInfo)
                {
                    for (int i = 0; i < goodcolors.Length; i++)
                    {
                        if (info.Name == goodcolors[i])
                        {
                            var color = new SolidColorBrush((Color)info.GetValue(null, null));
                            color.Freeze();
                            var item = new MenuItem()
                            {
                                Header = info.Name,
                                Foreground = color,
                                FontWeight = FontWeights.Bold,
                                FontSize = 12
                            };
                            item.Click += AddColorChoosed;
                            _instantColorMenu.Items.Add(item);

                            found++;
                            break;
                        }
                    }
                    if (found == goodcolors.Length)
                    {
                        break;
                    }
                }
            }

            var p = (Paragraph)sender;
            var msg = (Message)p.Tag;
            _instantColorMenu.Tag = msg;
            ((MenuItem)_instantColorMenu.Items[0]).Foreground = msg.Style.NickColor;
            p.ContextMenu = _instantColorMenu;
            p.ContextMenu.IsOpen = true;
            p.ContextMenu = null;
        }

        private void MessageScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            e.Handled = true;
            var obj = sender as ScrollViewer;
            // Keep scrolling
            if (obj.VerticalOffset == obj.ScrollableHeight)
            {
                if (this._rtbDocument.Blocks.Count > GlobalManager.MaxMessagesDisplayed)
                {
                    while (this._rtbDocument.Blocks.Count > GlobalManager.MaxMessagesDisplayed)
                    {
                        this._rtbDocument.Blocks.Remove(this._rtbDocument.Blocks.FirstBlock);
                        this._messagesLoadedFrom = this._messagesLoadedFrom.Next;

                        if (Properties.Settings.Default.ChatMode)
                        {
                            while (this._messagesLoadedFrom != null)
                            {
                                var type = this._messagesLoadedFrom.Value.Style.Type;
                                if (type != Message.MessageTypes.Part && type != Message.MessageTypes.Join && type != Message.MessageTypes.Quit)
                                {
                                    break;
                                }
                                this._messagesLoadedFrom = this._messagesLoadedFrom.Next;
                            }
                        }
                    }
                }

                obj.ScrollToEnd();
            }
            // Load older messages
            else if (obj.VerticalOffset == 0)
            {
                if (!_stopLoadingMessages)
                {
                    if (this._messagesLoadedFrom != null && this._messagesLoadedFrom != this.Messages.First)
                    {
                        _stopLoadingMessages = true;
                        int loaded = LoadMessages(GlobalManager.NumOfOldMessagesToBeLoaded);
                        Block first = this._rtbDocument.Blocks.FirstBlock;
                        double plus = first.Padding.Top + first.Padding.Bottom + first.Margin.Bottom + first.Margin.Top;
                        double sum = 0;
                        Block temp = this._rtbDocument.Blocks.FirstBlock;
                        for (int i = 0; i < loaded; i++)
                        {
                            double maxFontSize = 0;
                            // Get the biggest font size int the paragraph
                            Inline temp2 = ((Paragraph)temp).Inlines.FirstInline;
                            while (temp2 != null)
                            {
                                if (maxFontSize < temp2.FontSize)
                                {
                                    maxFontSize = temp.FontSize;
                                }
                                temp2 = temp2.NextInline;
                            }
                            sum += maxFontSize + plus;
                            temp = temp.NextBlock;
                            if (temp == null)
                            {
                                break;
                            }
                        }

                        obj.ScrollToVerticalOffset(sum);
                    }
                }
            }
            else
            {
                _stopLoadingMessages = false;
            }
        }

        private void MsgKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Return && MessageText.Length > 0)
            {
                e.Handled = true;
                string message = Server.VerifyString(MessageText);

                if (message.Length > 0)
                {
                    this._lastUserMessages.Add(message);
                    ProcessUserMessage(message);
                }

                MessageText = string.Empty;
                RaisePropertyChanged("MessageText");
            }
        }

        private void MsgPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                string text;
                if (this._lastUserMessages.TryGetPrevious(this.MessageText, out text))
                {
                    this.MessageText = text;
                    this.RaisePropertyChanged("MessageText");
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                string text;
                if (this._lastUserMessages.TryGetNext(this.MessageText, out text))
                {
                    this.MessageText = text;
                    this.RaisePropertyChanged("MessageText");
                }

                e.Handled = true;
            }
        }

        private void OpenLinkClick(object sender, RoutedEventArgs e)
        {
            var link = (Hyperlink)sender;
            try
            {
                Process.Start((string)link.CommandParameter);
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }

        private void ProcessUserMessage(string message)
        {
            // Command message
            if (message[0] == '/')
            {
                // Get the command
                int spacePos = message.IndexOf(' ');
                string command, text = string.Empty;
                if (spacePos != -1)
                {
                    command = message.Substring(1, spacePos - 1).Trim();
                    text = message.Substring(spacePos + 1).Trim();
                }
                else
                {
                    command = message.Substring(1).Trim();
                }

                // Process the command
                if (command.Equals("me", StringComparison.OrdinalIgnoreCase))
                {
                    if (text.Length > 0)
                    {
                        SendActionMessage(text);
                    }
                }
                else if (command.Equals("notice", StringComparison.OrdinalIgnoreCase))
                {
                    if (text.Length > 0)
                    {
                        SendNotice(text);
                    }
                }
                else if (command.Equals("nick", StringComparison.OrdinalIgnoreCase))
                {
                    if (text.Length > 0 && Server.HandleNickChange)
                    {
                        Server.NickChange(this, text);
                    }
                }
                else if (command.Equals("away", StringComparison.OrdinalIgnoreCase))
                {
                    this.MainViewModel.SetAway(text);
                }
                else if (command.Equals("back", StringComparison.OrdinalIgnoreCase))
                {
                    this.MainViewModel.SetBack();
                }
                else if (command.Equals("ctcp", StringComparison.OrdinalIgnoreCase))
                {
                    if (text.Length > 0)
                    {
                        string ctcpCommand = text;
                        string ctcpText = string.Empty;

                        spacePos = text.IndexOf(' ');
                        if (spacePos != -1)
                        {
                            ctcpCommand = text.Substring(0, spacePos).Trim();
                            ctcpText = text.Substring(spacePos + 1).Trim();
                        }

                        SendCTCPMessage(ctcpCommand, ctcpText);
                    }
                }
                else if (command.Equals("raw", StringComparison.OrdinalIgnoreCase) || command.Equals("irc", StringComparison.OrdinalIgnoreCase))
                {
                    if (text.Length > 0)
                    {
                        Server.Send(this, text);
                    }
                }
                else if (command.Equals("join", StringComparison.OrdinalIgnoreCase))
                {
                    if (Server.HandleJoinRequest && text.Length > 0 && (text.StartsWith("#") || text.StartsWith("&")))
                    {
                        string[] parts = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length <= 2)
                        {
                            AbstractChannelViewModel chvm;
                            if (Server.Channels.TryGetValue(parts[0], out chvm) == false)
                            {
                                if (parts.Length == 1)
                                {
                                    chvm = new ChannelViewModel(this.MainViewModel, this.Server, parts[0], string.Empty);
                                }
                                else
                                {
                                    chvm = new ChannelViewModel(this.MainViewModel, this.Server, parts[0], string.Empty, parts[1]);
                                }
                            }
                            this.MainViewModel.SelectChannel(chvm);
                        }
                    }
                }
                else if (command.Equals("pm", StringComparison.OrdinalIgnoreCase) ||
                         command.Equals("msg", StringComparison.OrdinalIgnoreCase) ||
                         command.Equals("chat", StringComparison.OrdinalIgnoreCase))
                {
                    if (text.Length > 0)
                    {
                        string username = text;
                        string msg = string.Empty;

                        spacePos = text.IndexOf(' ');
                        if (spacePos != -1)
                        {
                            username = text.Substring(0, spacePos).Trim();
                            msg = text.Substring(spacePos + 1).Trim();
                        }

                        AbstractChannelViewModel chvm;
                        if (Server.Channels.TryGetValue(username, out chvm) == false)
                        {
                            chvm = new PMChannelViewModel(this.MainViewModel, this.Server, username);
                        }
                        this.MainViewModel.SelectChannel(chvm);

                        if (msg.Length > 0)
                        {
                            chvm.SendMessage(msg);
                        }
                    }
                }
                else if (command.Equals("gs", StringComparison.OrdinalIgnoreCase))
                {
                    var gsUsers = Server.Users.Where(x => x.Value.UsingGreatSnooper).Select(x => x.Value.Name).ToList();
                    this.MainViewModel.DialogService.ShowDialog(Localizations.GSLocalization.Instance.GSCheckTitle, string.Format(Localizations.GSLocalization.Instance.GSCheckText, gsUsers.Count, string.Join(", ", gsUsers)));
                }
                else if (command.Equals("log", StringComparison.OrdinalIgnoreCase) || command.Equals("logs", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Process.Start(GlobalManager.SettingsPath);
                }
                else if (command.Equals("news", StringComparison.OrdinalIgnoreCase))
                {
                    MainViewModel.OpenNewsCommand.Execute(null);
                }
                else if (command.Equals("ignore", StringComparison.OrdinalIgnoreCase))
                {
                    this.MainViewModel.AddOrRemoveBanCommand.Execute(text);
                }
                else if (command.Equals("part", StringComparison.OrdinalIgnoreCase))
                {
                    var chvm = this as ChannelViewModel;
                    if (chvm != null)
                    {
                        chvm.LeaveChannelCommand.Execute(null);
                    }
                    else
                    {
                        this.MainViewModel.CloseChannelCommand.Execute(this);
                    }
                }
                else if (command.Equals("worms", StringComparison.OrdinalIgnoreCase))
                {
                    Properties.Settings.Default.ShowWormsChannel = !Properties.Settings.Default.ShowWormsChannel;
                    Properties.Settings.Default.Save();
                }
                else if (command.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    this.MainViewModel.CloseCommand.Execute(null);
                }
                else if (command.Equals("topic", StringComparison.OrdinalIgnoreCase))
                {
                    if (this is ChannelViewModel)
                    {
                        this.Server.Send(this, "TOPIC " + this.Name);
                    }
                }
                else if (command.Equals("get", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (text.Length > 0)
                        {
                            if (SettingsHelper.Exists(text))
                            {
                                this.AddMessage(GlobalManager.SystemUser, SettingsHelper.Load(text).ToString(), MessageSettings.SystemMessage);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Log(ex);
                    }
                }
                else if (command.Equals("set", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string settingName = text;
                        string value = string.Empty;
                        spacePos = text.IndexOf(' ');
                        if (spacePos != -1)
                        {
                            settingName = text.Substring(0, spacePos).Trim();
                            value = text.Substring(spacePos + 1).Trim();
                        }

                        if (SettingsHelper.Exists(settingName))
                        {
                            var type = SettingsHelper.Load(settingName).GetType();
                            SettingsHelper.Save(settingName, Convert.ChangeType(value, type));
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Log(ex);
                    }
                }
                else if (command.Equals("batman", StringComparison.OrdinalIgnoreCase))
                {
                    // hehehe
                    this.MainViewModel.BatLogo = !this.MainViewModel.BatLogo;
                }
                else if (command.Equals("debug", StringComparison.OrdinalIgnoreCase))
                {
                    GlobalManager.DebugMode = !GlobalManager.DebugMode;
                }
            }
            // Simple message
            else
            {
                message = message.Trim();
                if (message.Length > 0)
                {
                    SendMessage(message);
                }
            }
        }

        private void RemoveInstantColor(object sender, RoutedEventArgs e)
        {
            MenuItem obj = (MenuItem)sender;
            User u = ((Message)((ContextMenu)obj.Parent).Tag).Sender;

            this.MainViewModel.InstantColors.Remove(u.Name);
            ChangeMessageColorForUser(u, null);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._channelLogger != null)
                {
                    this._channelLogger.EndLogging();
                    this._channelLogger.Dispose();
                    this._channelLogger = null;
                }

                this.ClearUsers();
            }
        }
    }
}
