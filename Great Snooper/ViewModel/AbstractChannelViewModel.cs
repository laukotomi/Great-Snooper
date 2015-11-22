using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.IRCTasks;
using GreatSnooper.Model;
using GreatSnooper.UserControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace GreatSnooper.ViewModel
{
    [DebuggerDisplay("{Name}")]
    public abstract class AbstractChannelViewModel : ViewModelBase, IComparable
    {
        #region Static
        private static Regex dateRegex = new Regex(@"[^0-9]");
        protected static Regex urlRegex = new Regex(@"\b(http|ftp)s?://\S+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        #endregion

        #region Members
        private bool _loading;
        private bool _isHighlighted;
        private bool _joined;
        private bool _isTBFocused;
        private bool _disabled;

        private LinkedList<string> lastMessages;
        private LinkedListNode<string> lastMessageIterator;
        private string tempMessage = string.Empty;
        private LinkedListNode<Message> messagesLoadedFrom;
        private LinkedListNode<Message> lastMessageLoaded;
        private bool stopLoadingMessages;

        protected TabItem tabitem;
        private Border _connectedLayout;
        protected RichTextBox rtb;
        protected FlowDocument rtbDocument;
        private ContextMenu instantColorMenu;
        #endregion

        #region Properties
        public string Name { get; protected set; }
        public SortedObservableCollection<User> Users { get; private set; }
        public LinkedList<Message> Messages { get; private set; }
        public AbstractCommunicator Server { get; private set; }
        public bool Loading
        {
            get { return _loading; }
            set
            {
                if (_loading != value)
                {
                    _loading = value;
                    RaisePropertyChanged("Loading");
                }
            }
        }
        public bool IsHighlighted
        {
            get { return _isHighlighted; }
            set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    RaisePropertyChanged("IsHightlighted");
                }
            }
        }
        public string MessageText { get; set; }
        public bool IsTBFocused
        {
            get { return _isTBFocused; }
            set
            {
                if (_isTBFocused != value)
                {
                    _isTBFocused = value;
                    RaisePropertyChanged("IsTBFocused");
                    _isTBFocused = false;
                    RaisePropertyChanged("IsTBFocused");
                }
            }
        }
        public bool Disabled
        {
            get { return _disabled; }
            set
            {
                if (_disabled != value)
                {
                    _disabled = value;
                    RaisePropertyChanged("Disabled");
                }
            }
        }
        public MainViewModel MainViewModel { get; private set; }
        protected Border ConnectedLayout
        {
            get
            {
                if (_connectedLayout == null)
                {
                    _connectedLayout = new ConnectedLayout(this);
                    var sw = (ScrollViewer)((Border)((Grid)_connectedLayout.Child).Children[0]).Child;
                    sw.ScrollChanged += MessageScrollChanged;
                    rtb = (RichTextBox)sw.Content;
                    rtbDocument = rtb.Document;
                }
                return _connectedLayout;
            }
        }
        public int NewMessagesCount { get; private set; }
        public bool Joined
        {
            get { return _joined; }
            protected set
            {
                if (_joined != value)
                {
                    _joined = value;

                    if (value == false)
                    {
                        // Reset everything to default value
                        this.Log(this.Messages.Count, true);
                        this.messagesLoadedFrom = null;
                        this.Disabled = false;
                        this.Loading = false;
                        this.IsHighlighted = false;
                        this.lastMessageIterator = null;
                        this.lastMessageLoaded = null;
                        this.MessageText = string.Empty;
                        this.NewMessagesCount = 0;
                        this.stopLoadingMessages = false;
                        this.tempMessage = string.Empty;
                        this.lastMessages.Clear();
                    }
                    this.JoinedChanged();
                    RaisePropertyChanged("Joined");
                }
            }
        }
        #endregion

        #region Abstract methods
        public abstract void ClearUsers();
        public abstract void SendMessage(string message, bool userMessage = false);
        public abstract void SendNotice(string message, bool userMessage = false);
        public abstract void SendActionMessage(string message, bool userMessage = false);
        public abstract void SendCTCPMessage(string ctcpCommand, string ctcpText, User except = null);
        public abstract void ProcessMessage(MessageTask msgTask);
        protected virtual void JoinedChanged() { }
        public abstract TabItem GetLayout();
        public abstract void SetLoading(bool loading = true);
        #endregion

        protected AbstractChannelViewModel(MainViewModel mainViewModel, AbstractCommunicator server)
        {
            this.MainViewModel = mainViewModel;
            this.Server = server;
            this.Messages = new LinkedList<Message>();
            this.Users = new SortedObservableCollection<User>();
            this.lastMessages = new LinkedList<string>();
            this.MessageText = string.Empty;
        }

        #region MsgKeyDownCommand
        public RelayCommand<KeyEventArgs> MsgKeyDownCommand
        {
            get { return new RelayCommand<KeyEventArgs>(MsgKeyDown); }
        }

        private void MsgKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Return && MessageText.Length > 0)
            {
                e.Handled = true;
                string message = Server.VerifyString(MessageText);

                if (message.Length > 0)
                {
                    SaveNewMessage(message);
                    ProcessUserMessage(message);
                }

                MessageText = string.Empty;
                RaisePropertyChanged("MessageText");
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
                    command = message.Substring(1).Trim();

                // Process the command
                if (command.Equals("me", StringComparison.OrdinalIgnoreCase))
                {
                    if (text.Length > 0)
                        SendActionMessage(text, true);
                }
                else if (command.Equals("notice", StringComparison.OrdinalIgnoreCase))
                {
                    if (text.Length > 0)
                        SendNotice(text, true);
                }
                else if (command.Equals("nick", StringComparison.OrdinalIgnoreCase))
                {
                    if (text.Length > 0 && Server.HandleNickChange)
                        Server.NickChange(this, text);
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
                        Server.Send(this, text);
                }
                else if (command == "e")
                {
                    this.MainViewModel.EnterEnergySaveMode();
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
                                    new ChannelViewModel(this.MainViewModel, this.Server, parts[0], "");
                                else
                                    new ChannelViewModel(this.MainViewModel, this.Server, parts[0], "", parts[1]);
                            }
                            else if (this.MainViewModel.SelectedChannel != chvm)
                                this.MainViewModel.SelectChannel(chvm);
                        }
                    }
                }
                else if (command.Equals("pm", StringComparison.OrdinalIgnoreCase))
                {
                    if (text.Length > 0)
                    {
                        AbstractChannelViewModel chvm;
                        if (Server.Channels.TryGetValue(text, out chvm) == false)
                            new PMChannelViewModel(this.MainViewModel, this.Server, text);
                        else if (this.MainViewModel.SelectedChannel != chvm)
                            this.MainViewModel.SelectChannel(chvm);
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
                else if (command.Equals("worms", StringComparison.OrdinalIgnoreCase))
                {
                    Properties.Settings.Default.ShowWormsChannel = !Properties.Settings.Default.ShowWormsChannel;
                    Properties.Settings.Default.Save();
                }
                else if (command.Equals("get", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (text.Length > 0)
                        {
                            if (SettingsHelper.Exists(text))
                                this.AddMessage(GlobalManager.SystemUser, SettingsHelper.Load(text).ToString(), MessageSettings.SystemMessage);
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
                    SendMessage(message, true);
            }
        }

        private void SaveNewMessage(string message)
        {
            if (lastMessages.Count == 0 || lastMessages.First.Value != message)
            {
                if (lastMessages.Count == GlobalManager.LastMessageCapacity)
                    lastMessages.RemoveLast();
                lastMessages.AddFirst(message);
            }
            lastMessageIterator = null;
        }
        #endregion

        #region MsgPreviewKeyDownCommand
        public RelayCommand<KeyEventArgs> MsgPreviewKeyDownCommand
        {
            get { return new RelayCommand<KeyEventArgs>(MsgPreviewKeyDown); }
        }

        private void MsgPreviewKeyDown(KeyEventArgs e)
        {
            if (lastMessages.Count > 0)
            {
                if (e.Key == Key.Up)
                {
                    if (lastMessageIterator == null)
                    {
                        lastMessageIterator = lastMessages.First;
                        tempMessage = MessageText;
                        MessageText = lastMessageIterator.Value;
                        RaisePropertyChanged("MessageText");
                    }
                    else
                    {
                        if (lastMessageIterator.Next != null)
                        {
                            lastMessageIterator = lastMessageIterator.Next;
                            MessageText = lastMessageIterator.Value;
                            RaisePropertyChanged("MessageText");
                        }
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.Down)
                {
                    if (lastMessageIterator != null)
                    {
                        lastMessageIterator = lastMessageIterator.Previous;
                        if (lastMessageIterator == null)
                            MessageText = tempMessage;
                        else
                            MessageText = lastMessageIterator.Value;
                        RaisePropertyChanged("MessageText");
                    }
                    e.Handled = true;
                }
            }
        }
        #endregion

        #region Add message
        public void AddMessage(User sender, string message, MessageSetting messageSetting, bool userMessage = false)
        {
            var msg = new Message(sender, message, messageSetting);

            if (userMessage || msg.Style.Type == Message.MessageTypes.Quit)
            {
                var matches = urlRegex.Matches(msg.Text);
                for (int i = 0; i < matches.Count; i++)
                {
                    var groups = matches[i].Groups;
                    Uri uri;
                    if (Uri.TryCreate(groups[0].Value, UriKind.RelativeOrAbsolute, out uri))
                        msg.AddHighlightWord(groups[0].Index, groups[0].Length, Message.HightLightTypes.URI);
                }
            }

            this.AddMessage(msg);
        }

        protected void AddMessage(Message msg)
        {
            if (this.MainViewModel.IsEnergySaveMode == false && this.Messages.Count >= GlobalManager.MaxMessagesInMemory)
                Log(this.Messages.Count - GlobalManager.MaxMessagesInMemory + GlobalManager.NumOfOldMessagesToBeLoaded);

            this.Messages.AddLast(msg);

            if (!msg.Sender.IsBanned || this.GetType() == typeof(ChannelViewModel) && Properties.Settings.Default.ShowBannedMessages)
            {
                if (this.MainViewModel.IsEnergySaveMode)
                    this.NewMessagesCount++;
                else
                {
                    this.lastMessageLoaded = this.Messages.Last;
                    if (this.messagesLoadedFrom == null)
                        this.messagesLoadedFrom = this.Messages.Last;
                    this.AddMessageToUI(msg);
                }
            }
        }

        private bool AddMessageToUI(Message msg, bool add = true)
        {
            if (Properties.Settings.Default.ChatMode && (
                msg.Style.Type == Message.MessageTypes.Part ||
                msg.Style.Type == Message.MessageTypes.Join ||
                msg.Style.Type == Message.MessageTypes.Quit)
            )
                return false;
            
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

                // Instant color
                SolidColorBrush b;
                if (this.MainViewModel.InstantColors.TryGetValue(msg.Sender.Name, out b))
                    nick.Foreground = b;
                // Group color
                else if (msg.Sender.Group.ID != UserGroups.SystemGroupID)
                {
                    nick.Foreground = msg.Sender.Group.TextColor;
                    nick.FontStyle = FontStyles.Italic;
                }
                else
                    nick.Foreground = msg.Style.NickColor;
                nick.FontWeight = FontWeights.Bold;
                p.Inlines.Add(nick);

                // Message content
                if (msg.Style.IsFixedText || msg.HighlightWords == null)
                    p.Inlines.Add(new Run(msg.Text));
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
                            word.Command = this.MainViewModel.OpenLinkCommand;
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
                if (add == false && this.rtbDocument.Blocks.Count > 0)
                    this.rtbDocument.Blocks.InsertBefore(this.rtbDocument.Blocks.FirstBlock, p);
                else
                    this.rtbDocument.Blocks.Add(p);

                return true;
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }
            return false;
        }
        #endregion

        #region Message scrolling, log and loading
        private void MessageScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            e.Handled = true;
            var obj = sender as ScrollViewer;
            // Keep scrolling
            if (obj.VerticalOffset == obj.ScrollableHeight)
            {
                if (this.rtbDocument.Blocks.Count > GlobalManager.MaxMessagesDisplayed)
                {
                    while (this.rtbDocument.Blocks.Count > GlobalManager.MaxMessagesDisplayed)
                    {
                        this.rtbDocument.Blocks.Remove(this.rtbDocument.Blocks.FirstBlock);
                        this.messagesLoadedFrom = this.messagesLoadedFrom.Next;

                        if (Properties.Settings.Default.ChatMode)
                        {
                            while (this.messagesLoadedFrom != null)
                            {
                                var type = this.messagesLoadedFrom.Value.Style.Type;
                                if (type != Message.MessageTypes.Part && type != Message.MessageTypes.Join && type != Message.MessageTypes.Quit)
                                    break;
                                this.messagesLoadedFrom = this.messagesLoadedFrom.Next;
                            }
                        }
                    }
                }

                obj.ScrollToEnd();
            }
            // Load older messages
            else if (obj.VerticalOffset == 0)
            {
                if (!stopLoadingMessages)
                {
                    if (this.messagesLoadedFrom != null && this.messagesLoadedFrom != this.Messages.First)
                    {
                        stopLoadingMessages = true;
                        int loaded = LoadMessages(GlobalManager.NumOfOldMessagesToBeLoaded);
                        Block first = this.rtbDocument.Blocks.FirstBlock;
                        double plus = first.Padding.Top + first.Padding.Bottom + first.Margin.Bottom + first.Margin.Top;
                        double sum = 0;
                        Block temp = this.rtbDocument.Blocks.FirstBlock;
                        for (int i = 0; i < loaded; i++)
                        {
                            double maxFontSize = 0;
                            // Get the biggest font size int the paragraph
                            Inline temp2 = ((Paragraph)temp).Inlines.FirstInline;
                            while (temp2 != null)
                            {
                                if (maxFontSize < temp2.FontSize)
                                    maxFontSize = temp.FontSize;
                                temp2 = temp2.NextInline;
                            }
                            sum += maxFontSize + plus;
                            temp = temp.NextBlock;
                            if (temp == null)
                                break;
                        }

                        obj.ScrollToVerticalOffset(sum);
                    }
                }
            }
            else
                stopLoadingMessages = false;
        }

        public int LoadMessages(int count, bool clear = false)
        {
            if (clear)
                this.rtbDocument.Blocks.Clear();

            // select the index from which the messages will be loaded
            if (clear)
            {
                this.messagesLoadedFrom = this.Messages.Last;
                this.lastMessageLoaded = this.Messages.Last;
            }
            else
                this.messagesLoadedFrom = this.messagesLoadedFrom.Previous;

            // load the message backwards
            int k = 0;
            while (true)
            {
                var msg = messagesLoadedFrom.Value;
                if (!msg.Sender.IsBanned || Properties.Settings.Default.ShowBannedMessages)
                {
                    if (AddMessageToUI(msg, false))
                    {
                        k++;
                        if (k == count)
                            break;
                    }
                }
                if (this.messagesLoadedFrom.Previous == null)
                    break;
                this.messagesLoadedFrom = this.messagesLoadedFrom.Previous;
            }

            return k;
        }

        public void LoadNewMessages()
        {
            for (int i = 0; i < this.NewMessagesCount; i++)
            {
                if (this.lastMessageLoaded == null)
                    this.lastMessageLoaded = this.Messages.First;
                else
                    this.lastMessageLoaded = this.lastMessageLoaded.Next;
                
                var msg = lastMessageLoaded.Value;
                if (!msg.Sender.IsBanned || Properties.Settings.Default.ShowBannedMessages)
                    AddMessageToUI(msg);
                if (this.lastMessageLoaded.Next == null)
                    break;
            }

            this.NewMessagesCount = 0;
        }

        public void Log(int count, bool makeEnd = false)
        {
            if (count == 0) return;

            try
            {
                string dirPath = GlobalManager.SettingsPath + @"\Logs\" + this.Name;
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                string logFile = dirPath + "\\" + dateRegex.Replace(DateTime.Now.ToString("d"), "-") + ".log";

                using (StreamWriter writer = new StreamWriter(logFile, true))
                {
                    for (int i = 0; i < count && this.Messages.Count > 0; i++)
                    {
                        var msg = this.Messages.First.Value;
                        writer.WriteLine("(" + msg.Style.Type.ToString() + ") " + msg.Time.ToString("G") + " " + msg.Sender.Name + ": " + msg.Text);
                        this.Messages.RemoveFirst();
                        if (this.messagesLoadedFrom != null && messagesLoadedFrom.Value == msg)
                        {
                            this.rtbDocument.Blocks.Remove(this.rtbDocument.Blocks.FirstBlock);
                            this.messagesLoadedFrom = this.Messages.First; // ok, since it was the first block which was removed and it is already checked that messagesLoadedFrom was the first message
                            if (Properties.Settings.Default.ChatMode)
                            {
                                while (this.messagesLoadedFrom != null)
                                {
                                    var type = this.messagesLoadedFrom.Value.Style.Type;
                                    if (type != Message.MessageTypes.Part && type != Message.MessageTypes.Join && type != Message.MessageTypes.Quit)
                                        break;
                                    this.messagesLoadedFrom = this.messagesLoadedFrom.Next;
                                }
                            }
                        }
                    }

                    if (makeEnd)
                    {
                        writer.WriteLine(DateTime.Now.ToString("G") + " - Channel closed.");
                        writer.WriteLine("-----------------------------------------------------------------------------------------");
                        writer.WriteLine(Environment.NewLine + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }
        #endregion

        public void Highlight()
        {
            if (this.MainViewModel.IsWindowActive == false || this.MainViewModel.SelectedChannel != this)
            {
                this.IsHighlighted = true;
                if (this is PMChannelViewModel)
                    ((PMChannelViewModel)this).GenerateHeader();
            }
        }

        #region Instant colors
        private void InstantColorMenu(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!this.rtb.Selection.IsEmpty)
                return;

            if (instantColorMenu == null)
            {
                instantColorMenu = new ContextMenu();

                var def = new MenuItem() { Header = Localizations.GSLocalization.Instance.DefaultText, FontWeight = FontWeights.Bold, FontSize = 12 };
                def.Click += RemoveInstantColor;
                instantColorMenu.Items.Add(def);

                string[] goodcolors = { "Aquamarine", "Bisque", "BlueViolet", "BurlyWood", "CadetBlue", "Chocolate", "CornflowerBlue", "Gold", "Pink", "Plum", "GreenYellow", "Sienna", "Violet" };
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
                            var item = new MenuItem() { Header = info.Name, Foreground = color, FontWeight = FontWeights.Bold, FontSize = 12 };
                            item.Click += AddColorChoosed;
                            instantColorMenu.Items.Add(item);

                            found++;
                            break;
                        }
                    }
                    if (found == goodcolors.Length)
                        break;
                }
            }

            var p = (Paragraph)sender;
            var msg = (Message)p.Tag;
            instantColorMenu.Tag = msg;
            ((MenuItem)instantColorMenu.Items[0]).Foreground = msg.Style.NickColor;
            p.ContextMenu = instantColorMenu;
            p.ContextMenu.IsOpen = true;
            p.ContextMenu = null;
        }

        private void AddColorChoosed(object sender, RoutedEventArgs e)
        {
            MenuItem obj = (MenuItem)sender;
            User u = ((Message)((ContextMenu)obj.Parent).Tag).Sender;
            SolidColorBrush color = (SolidColorBrush)obj.Foreground;

            this.MainViewModel.InstantColors[u.Name] = color;
            ChangeMessageColorForUser(u, color);
        }

        private void RemoveInstantColor(object sender, RoutedEventArgs e)
        {
            MenuItem obj = (MenuItem)sender;
            User u = ((Message)((ContextMenu)obj.Parent).Tag).Sender;

            this.MainViewModel.InstantColors.Remove(u.Name);
            ChangeMessageColorForUser(u, null);
        }

        public void ChangeMessageColorForUser(User u, SolidColorBrush color)
        {
            bool italic = u.Group.ID != UserGroups.SystemGroupID;

            foreach (var chvm in this.MainViewModel.Channels)
            {
                if (chvm.Joined && chvm.rtbDocument.Blocks.Count > 0)
                {
                    Paragraph p = (Paragraph)chvm.rtbDocument.Blocks.FirstBlock;
                    while (p != null)
                    {
                        var msg = (Message)p.Tag;
                        if (msg.Sender == u)
                        {
                            if (Properties.Settings.Default.MessageTime)
                            {
                                p.Inlines.FirstInline.NextInline.Foreground = (color != null) ? color : msg.Style.NickColor;
                                p.Inlines.FirstInline.NextInline.FontStyle = (italic) ? FontStyles.Italic : FontStyles.Normal;
                            }
                            else
                            {
                                p.Inlines.FirstInline.Foreground = (color != null) ? color : msg.Style.NickColor;
                                p.Inlines.FirstInline.FontStyle = (italic) ? FontStyles.Italic : FontStyles.Normal;
                            }
                        }
                        p = (Paragraph)p.NextBlock;
                    }
                }
            }
        }
        #endregion

        public int CompareTo(object obj)
        {
            var o = (AbstractChannelViewModel)obj;
            return this.Name.CompareTo(o.Name);
        }
    }
}
