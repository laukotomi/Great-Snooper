using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using GreatSnooper.IRC;
using GreatSnooper.IRCTasks;
using GreatSnooper.Model;
using GreatSnooper.Services;
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

        protected AbstractChannelViewModel(MainViewModel mainViewModel, IRCCommunicator server)
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
                        this.ClearMessages(0);
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

        public IRCCommunicator Server
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
            var msg = new Message(this, sender, message, messageSetting, DateTime.Now);
            this.AddMessage(msg);
        }

        public void AddMessage(Message msg)
        {
            this._channelLogger.LogMessage(msg, this.Name);
            if (this.Messages.Count >= GlobalManager.MaxMessagesInMemory && this.MainViewModel.IsGameWindowOn() == false)
            {
                ClearMessages(GlobalManager.MaxMessagesInMemory);
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
                while (this._rtbDocument.Blocks.Count > 0)
                {
                    Message msg = this._rtbDocument.Blocks.FirstBlock.Tag as Message;
                    if (msg != null)
                    {
                        msg.NickRun = null;
                    }
                    this._rtbDocument.Blocks.Remove(this._rtbDocument.Blocks.FirstBlock);
                }
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
            if (count > 0)
            {
                while (this._messagesLoadedFrom != null)
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
            }

            return k;
        }

        public void LoadNewMessages()
        {
            try
            {
                while (true)
                {
                    if (this._lastMessageLoaded == null)
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

            InstantColors.Instance.Add(u, color);
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
                if (msg.HighlightParts == null)
                {
                    p.Inlines.Add(new Run(msg.Text));
                }
                else
                {
                    int idx = 0;
                    foreach (Message.MessageHighlight highlight in msg.HighlightParts)
                    {
                        if (highlight.StartCharPos != idx)
                        {
                            string part = msg.Text.Substring(idx, highlight.StartCharPos - idx);
                            p.Inlines.Add(new Run(part));
                            idx += part.Length;
                        }

                        string hword = msg.Text.Substring(idx, highlight.Length);
                        if (highlight.Type == Message.HightLightTypes.Highlight)
                        {
                            Run word = new Run(hword);
                            MessageSettings.LoadSettingsFor(word, MessageSettings.LeagueFoundMessage);
                            word.Foreground = MessageSettings.LeagueFoundMessage.NickColor;
                            p.Inlines.Add(word);
                        }
                        else if (highlight.Type == Message.HightLightTypes.URI)
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
                        idx += highlight.Length;
                    }

                    if (idx != msg.Text.Length)
                    {
                        string part = msg.Text.Substring(idx, msg.Text.Length - idx);
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

        private void ClearMessages(int maxMessages)
        {
            try
            {
                while (this.Messages.Count > maxMessages)
                {
                    Message msg = this.Messages.First.Value;
                    this.Messages.RemoveFirst();
                    msg.Sender.Messages.Remove(msg);

                    if (this._messagesLoadedFrom != null && this._messagesLoadedFrom.Value == msg) // If the removed message was displayed (when the scrollbar is not at bottom)
                    {
                        this._rtbDocument.Blocks.Remove(this._rtbDocument.Blocks.FirstBlock);
                        msg.NickRun = null;
                        this.SetMessagesLoadedFrom(this.Messages.First);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }

        private void SetMessagesLoadedFrom(LinkedListNode<Message> from)
        {
            this._messagesLoadedFrom = from;

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
                        this._messagesLoadedFrom.Value.NickRun = null;
                        SetMessagesLoadedFrom(this._messagesLoadedFrom.Next);
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
                UserCommandService.Instance.Run(this, command, text);
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

            InstantColors.Instance.Remove(u);
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
