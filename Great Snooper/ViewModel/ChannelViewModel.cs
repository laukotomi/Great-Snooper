namespace GreatSnooper.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using GalaSoft.MvvmLight.Command;

    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;
    using GreatSnooper.Model;
    using GreatSnooper.UserControls;
    using GreatSnooper.Windows;

    using MahApps.Metro.Controls.Dialogs;

    public class ChannelViewModel : AbstractChannelViewModel
    {
        private readonly LeagueSearcher leagueSearcher;
        private readonly Notificator notificator;

        private static Regex channelSchemeRegex = new Regex(@"^<SCHEME=([^>]+)>$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private bool isHighlightInRegex;
        private bool isLeagueSearcherInRegex;
        private Game joinedGame;
        private Regex messageRegex;
        private bool? _autoJoin;
        private bool _canHost;
        private Border _disconnectedLayout;
        private string _scheme = string.Empty;

        public ChannelViewModel(MainViewModel mainViewModel, AbstractCommunicator server, string channelName, string description, string password = null)
            : base(mainViewModel, server)
        {
            this.Name = channelName;
            this.Password = password;
            this.Description = description;
            this.leagueSearcher = LeagueSearcher.Instance;
            this.notificator = Notificator.Instance;
            this._autoJoin = GlobalManager.AutoJoinList.ContainsKey(channelName);
            this.GameListUpdatedTime = new DateTime(1999, 5, 31);
            this.RegenerateGroupsMenu = true;

            this.Games = new SortedObservableCollection<Game>();

            server.Channels.Add(this.Name, this);
            if (GlobalManager.HiddenChannels.Contains(channelName) == false)
            {
                mainViewModel.CreateChannel(this);
            }
        }

        public RelayCommand<User> AddUserToDefaultGroupCommand
        {
            get
            {
                return new RelayCommand<User>(AddUserToDefaultGroup);
            }
        }

        public RelayCommand<KeyValuePair<User, UserGroup>> AddUserToGroupCommand
        {
            get
            {
                return new RelayCommand<KeyValuePair<User, UserGroup>>(AddUserToGroup);
            }
        }

        public bool? AutoJoin
        {
            get
            {
                return this._autoJoin;
            }
            set
            {
                if (this._autoJoin != value)
                {
                    this._autoJoin = value;
                    if (value.HasValue && value.Value == false && GlobalManager.AutoJoinList.ContainsKey(this.Name))
                    {
                        GlobalManager.AutoJoinList.Remove(this.Name);
                        SettingsHelper.Save("AutoJoinChannels", GlobalManager.AutoJoinList);
                    }
                }
            }
        }

        public bool CanHost
        {
            get
            {
                return this._canHost;
            }
            private set
            {
                if (this._canHost != value)
                {
                    this._canHost = value;
                    RaisePropertyChanged("CanHost");
                    RaisePropertyChanged("CanHostJoined");
                }
            }
        }

        public bool CanHostJoined
        {
            get
            {
                return CanHost && Joined;
            }
        }

        public Task ChannelSchemeTask
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        public Grid GameListGrid
        {
            get;
            private set;
        }

        public DateTime GameListUpdatedTime
        {
            get;
            set;
        }

        public SortedObservableCollection<Game> Games
        {
            get;
            private set;
        }

        public ICommand HostGameCommand
        {
            get
            {
                return new RelayCommand(HostGame);
            }
        }

        public RelayCommand<string> JoinCloseCommand
        {
            get
            {
                return new RelayCommand<string>(JoinClose);
            }
        }

        public ICommand JoinCommand
        {
            get
            {
                return new RelayCommand(DoJoin);
            }
        }

        public RelayCommand<string> JoinGameCommand
        {
            get
            {
                return new RelayCommand<string>(JoinGame);
            }
        }

        public RelayCommand<string> LeaveChannelCommand
        {
            get
            {
                return new RelayCommand<string>(LeaveChannel);
            }
        }

        public RelayCommand<User> OpenChatCommand
        {
            get
            {
                return new RelayCommand<User>(OpenChat);
            }
        }

        public ICommand OpenChatDClickCommand
        {
            get
            {
                return new RelayCommand(OpenChatDClick);
            }
        }

        public string Password
        {
            get;
            set;
        }

        public bool RegenerateGroupsMenu
        {
            get;
            set;
        }

        public string Scheme
        {
            get
            {
                return _scheme;
            }
            private set
            {
                if (_scheme != value)
                {
                    _scheme = value;
                    CanHost = !value.Contains("Tf");
                }
            }
        }

        public Game SelectedGame
        {
            get;
            set;
        }

        public RelayCommand<string> SilentJoinCloseCommand
        {
            get
            {
                return new RelayCommand<string>(SilentJoinClose);
            }
        }

        public RelayCommand<string> SilentJoinCommand
        {
            get
            {
                return new RelayCommand<string>(SilentJoin);
            }
        }

        public UserListGrid UserListDG
        {
            get;
            private set;
        }

        private Border DisconnectedLayout
        {
            get
            {
                if (_disconnectedLayout == null)
                {
                    _disconnectedLayout = new DisconnectedLayout(this);
                }

                return _disconnectedLayout;
            }
        }

        public void AddUser(User u)
        {
            this.Users.Add(u);
            u.Channels.Add(this);
        }

        public override void ClearUsers()
        {
            var temp = new HashSet<User>(this.Users);
            foreach (User u in temp)
            {
                this.RemoveUser(u);

                if (u.Channels.Count == 0)
                {
                    if (u.PMChannels.Count > 0)
                    {
                        u.OnlineStatus = User.Status.Unknown;
                    }
                    else
                    {
                        GreatSnooper.Helpers.UserHelper.FinalizeUser(this.Server, u);
                    }
                }
            }
        }

        public void FinishJoin()
        {
            this.Joined = true;
            this.AddMessage(this.Server.User, Localizations.GSLocalization.Instance.JoinMessage, MessageSettings.JoinMessage);
            if (Properties.Settings.Default.UseWhoMessages)
            {
                this.Server.GetChannelClients(this, this.Name);    // get the users in the channel
            }
        }

        public TabItem GetGameListLayout()
        {
            GameListGrid = new GameListLayout();
            GameListGrid.DataContext = this;
            return new TabItem()
            {
                Content = GameListGrid,
                Visibility = Visibility.Collapsed
            };
        }

        public override TabItem GetLayout()
        {
            if (_tabitem == null)
            {
                var mainWindow = (MainWindow)this.MainViewModel.DialogService.GetView();
                _tabitem = new TabItem();
                _tabitem.DataContext = this;
                _tabitem.Style = (Style)mainWindow.FindResource("channelTabItem");
                _tabitem.Content = (this.Joined) ? ConnectedLayout : DisconnectedLayout;

            }
            return _tabitem;
        }

        public TabItem GetUserListLayout()
        {
            UserListDG = new UserListGrid(this);
            UserListDG.ContextMenuOpening += userListDG_ContextMenuOpening;
            return new TabItem()
            {
                Content = UserListDG,
                Visibility = Visibility.Collapsed
            };
        }

        public override void ProcessMessage(IRCTasks.MessageTask msgTask)
        {
            var msg = new Message(msgTask.User, msgTask.Message, msgTask.Setting, DateTime.Now);
            bool canDisplay = !msg.Sender.IsBanned || this.GetType() == typeof(ChannelViewModel) && Properties.Settings.Default.ShowBannedMessages;

            // Search for league or hightlight or notification
            if (msgTask.Setting.Type == Message.MessageTypes.Channel)
            {
                if (canDisplay && this.notificator.SearchInSenderNamesEnabled &&
                    this.notificator.SenderNames.Any(r => r.IsMatch(msg.Sender.Name, msg.Sender.Name, this.Name)))
                {
                    msg.AddHighlightWord(0, msg.Text.Length, Message.HightLightTypes.NotificatorFound);
                    this.MainViewModel.NotificatorFound(msg, this);
                }

                if (messageRegex == null)
                {
                    GenerateMessageRegex();
                }
                if (isHighlightInRegex || isLeagueSearcherInRegex)
                {
                    MatchCollection matches = messageRegex.Matches(msg.Text);
                    for (int i = 0; i < matches.Count; i++)
                    {
                        GroupCollection groups = matches[i].Groups;
                        Group hGroup = groups["hbeep"];
                        if (canDisplay && isHighlightInRegex && hGroup.Length > 0)
                        {
                            if (hGroup.Value == this.Server.User.Name) // Check case sensitive
                            {
                                msg.AddHighlightWord(hGroup.Index, hGroup.Length, Message.HightLightTypes.Highlight);
                                this.Highlight();
                                this.MainViewModel.FlashWindow();
                                if (Properties.Settings.Default.TrayNotifications)
                                {
                                    this.MainViewModel.ShowTrayMessage(string.Format(Localizations.GSLocalization.Instance.HightLightMessage, this.Name), this);
                                }
                                if (Properties.Settings.Default.HBeepEnabled)
                                {
                                    Sounds.PlaySoundByName("HBeep");
                                }
                            }
                            continue;
                        }

                        Group leagueGroup = groups["league"];
                        if (canDisplay && isLeagueSearcherInRegex && leagueGroup.Length > 0 && this.leagueSearcher.HandleMatch(leagueGroup, msg))
                        {
                            this.MainViewModel.FlashWindow();

                            if (Properties.Settings.Default.TrayNotifications)
                            {
                                this.MainViewModel.ShowTrayMessage(msg.Sender.Name + ": " + msg.Text, this);
                            }
                            if (Properties.Settings.Default.LeagueFoundBeepEnabled)
                            {
                                Sounds.PlaySoundByName("LeagueFoundBeep");
                            }
                        }
                    }
                }

                if (canDisplay && this.notificator.SearchInMessagesEnabled)
                {
                    foreach (NotificatorEntry entry in this.notificator.InMessages)
                    {
                        var nmatches = entry.Matches(msg.Text, msg.Sender.Name, this.Name);
                        if (nmatches != null)
                        {
                            for (int i = 0; i < nmatches.Count; i++)
                            {
                                var groups = nmatches[i].Groups;
                                if (groups[0].Length > 0)
                                {
                                    msg.AddHighlightWord(groups[0].Index, groups[0].Length, Message.HightLightTypes.NotificatorFound);
                                }
                            }
                            if (nmatches.Count > 0)
                            {
                                this.MainViewModel.NotificatorFound(msg, this);
                            }
                        }
                    }
                }
            }

            // Add message logic
            this.AddMessage(msg);
        }

        public void RemoveUser(User u)
        {
            this.Users.Remove(u);
            u.Channels.Remove(this);
        }

        public override void SendActionMessage(string message)
        {
            Server.SendCTCPMessage(this, this.Name, "ACTION", message);
            AddMessage(Server.User, message, MessageSettings.ActionMessage);
        }

        public override void SendCTCPMessage(string ctcpCommand, string ctcpText, User except = null)
        {
            Server.SendCTCPMessage(this, this.Name, ctcpCommand, ctcpText);
        }

        public override void SendMessage(string message)
        {
            Server.SendMessage(this, this.Name, message);
            AddMessage(Server.User, message, MessageSettings.UserMessage);
        }

        public override void SendNotice(string message)
        {
            Server.SendNotice(this, this.Name, message);
            AddMessage(Server.User, message, MessageSettings.NoticeMessage);
        }

        public override void SetLoading(bool loading = true)
        {
            this.Loading = loading;
            this.Disabled = loading;
            if (loading && this.Joined)
            {
                ClearUsers();
            }
        }

        public void TryGetChannelScheme(Action onSuccess = null)
        {
            // Can not dispose channelSchemeTask because there can be parallel requests if user selects at least 2 channels to autojoin
            this.ChannelSchemeTask = Task.Factory.StartNew<string>(() =>
            {
                try
                {
                    HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + this.Server.ServerAddress + "/wormageddonweb/RequestChannelScheme.asp?Channel=" + this.Name.Substring(1));
                    myHttpWebRequest.UserAgent = "T17Client/1.2";
                    myHttpWebRequest.Proxy = null;
                    myHttpWebRequest.AllowAutoRedirect = false;
                    myHttpWebRequest.Timeout = GlobalManager.WebRequestTimeout;
                    using (WebResponse myHttpWebResponse = myHttpWebRequest.GetResponse())
                    using (Stream stream = myHttpWebResponse.GetResponseStream())
                    {
                        int bytes;
                        var sb = new StringBuilder();
                        byte[] schemeRecvBuffer = new byte[100];
                        while ((bytes = stream.Read(schemeRecvBuffer, 0, schemeRecvBuffer.Length)) > 0)
                        {
                            for (int j = 0; j < bytes; j++)
                            {
                                sb.Append(WormNetCharTable.Decode[schemeRecvBuffer[j]]);
                            }
                        }

                        // <SCHEME=Pf,Be>
                        Match m = channelSchemeRegex.Match(sb.ToString());
                        if (m.Success)
                        {
                            return m.Groups[1].Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }

                return string.Empty;
            })
            .ContinueWith(
                (t) =>
                {
                    if (this.MainViewModel.closing)
                    {
                        this.MainViewModel.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.MainViewModel.CloseCommand.Execute(null);
                        }));
                        return;
                    }

                    if (t.Result.Length > 0)
                        this.Scheme = t.Result;

                    if (onSuccess != null)
                        onSuccess();
                },
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        protected override void JoinedChanged()
        {
            RaisePropertyChanged("CanHostJoined");
            if (this.Joined)
            {
                this.leagueSearcher.MessageRegexChange += GenerateMessageRegex;
                this.Loading = false;
            }
            else // Part
            {
                this.leagueSearcher.MessageRegexChange -= GenerateMessageRegex;

                ClearUsers();
                this.Games.Clear();
                if (this.Server is GameSurgeCommunicator && this.Server.Users.Count == 0)
                {
                    this.Server.CancelAsync();
                }
            }

            if (_tabitem != null)
            {
                _tabitem.Content = (this.Joined) ? ConnectedLayout : DisconnectedLayout;
            }
        }

        private void AddUserToDefaultGroup(User u)
        {
            UserGroups.AddOrRemoveUser(u, null);
        }

        private void AddUserToGroup(KeyValuePair<User, UserGroup> param)
        {
            UserGroups.AddOrRemoveUser(param.Key, param.Value);
        }

        private bool CheckWAExe()
        {
            if (this.MainViewModel.ShowWAExe1 == false && this.MainViewModel.ShowWAExe2 == false)
            {
                this.MainViewModel.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.WAExeMissingText);
                return false;
            }

            if (!File.Exists(Properties.Settings.Default.WaExe) && !File.Exists(Properties.Settings.Default.WaExe2))
            {
                this.MainViewModel.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.WAExeNotExistsText);
                return false;
            }

            return true;
        }

        private void DoJoin()
        {
            this.Loading = true;

            if (AutoJoin.HasValue && AutoJoin.Value && GlobalManager.AutoJoinList.ContainsKey(this.Name) == false)
            {
                GlobalManager.AutoJoinList.Add(this.Name, this.Password);
                SettingsHelper.Save("AutoJoinChannels", GlobalManager.AutoJoinList);
            }

            if (this.Server is WormNetCommunicator || Server.State == AbstractCommunicator.ConnectionStates.Connected)
            {
                this.Server.JoinChannel(this, this.Name, this.Password);
            }
            else
            {
                var gameSurge = (GameSurgeCommunicator)this.Server;
                if (this.Server.State == AbstractCommunicator.ConnectionStates.Connected)
                {
                    gameSurge.JoinChannel(this, this.Name, this.Password);
                }
                else
                {
                    gameSurge.JoinChannelList.Add(this.Name);
                    this.Server.Connect();
                }
            }
        }

        private void DoJoinGame(string wa, bool silent, bool exit)
        {
            this.MainViewModel.ExitSnooperAfterGameStart = exit;
            this.MainViewModel.GameProcess = new Process();
            this.MainViewModel.GameProcess.StartInfo.UseShellExecute = false;
            this.MainViewModel.GameProcess.StartInfo.FileName = (wa == "1") ? Properties.Settings.Default.WaExe : Properties.Settings.Default.WaExe2;
            this.MainViewModel.GameProcess.StartInfo.Arguments = "wa://" + this.joinedGame.Address + "?gameid=" + this.joinedGame.ID + "&scheme=" + this.Scheme;
            if (this.MainViewModel.GameProcess.Start())
            {
                if (Properties.Settings.Default.WAHighPriority)
                {
                    this.MainViewModel.GameProcess.PriorityClass = ProcessPriorityClass.High;
                }
                if (Properties.Settings.Default.MessageJoinedGame && !silent)
                {
                    this.SendActionMessage("is joining a game: " + this.joinedGame.Name);
                }
                if (Properties.Settings.Default.MarkAway)
                {
                    this.MainViewModel.SetAway();
                }
            }
            else
            {
                this.MainViewModel.FreeGameProcess();
            }
        }

        private void GenerateMessageRegex()
        {
            var helper = new HashSet<string>(GlobalManager.CIStringComparer);
            var sb = new StringBuilder();

            this.isHighlightInRegex = Properties.Settings.Default.HBeepEnabled;
            if (this.isHighlightInRegex)
            {
                helper.Add(this.Server.User.Name);
                sb.Append(@"(?<hbeep>\b" + Regex.Escape(this.Server.User.Name) + @"\b)");
            }

            this.isLeagueSearcherInRegex = this.leagueSearcher.ChannelToSearch == this;
            if (this.isLeagueSearcherInRegex)
            {
                List<string> words = new List<string>(this.leagueSearcher.SearchData.Count);
                foreach (string word in this.leagueSearcher.SearchData.Keys)
                {
                    if (helper.Contains(word) == false)
                    {
                        helper.Add(word);
                        words.Add(Regex.Escape(word));
                    }
                }
                if (words.Count != 0)
                {
                    sb.Append(@"|(?<league>\b(");
                    sb.Append(string.Join(")|(", words));
                    sb.Append(@")\b)");
                }
            }

            this.messageRegex = new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        private void GenerateMessageRegex(object sender)
        {
            if (sender is LeagueSearcher && isLeagueSearcherInRegex == false && leagueSearcher.ChannelToSearch != this)
            {
                return;
            }
            GenerateMessageRegex();
        }

        private void HostGame()
        {
            if (!File.Exists(Path.GetFullPath("Hoster.exe")))
            {
                this.MainViewModel.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.HosterExeText);
                return;
            }

            if (!CheckWAExe())
            {
                return;
            }

            if (this.MainViewModel.GameProcess != null)
            {
                if (this.MainViewModel.StartedGameType == ViewModel.MainViewModel.StartedGameTypes.Join)
                {
                    this.MainViewModel.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.WAIsRunningText);
                    return;
                }
                else
                {
                    // Because of wormkit rehost module, this should be allowed
                    this.MainViewModel.FreeGameProcess();
                }
            }

            if (this.leagueSearcher.IsEnabled && Properties.Settings.Default.AskLeagueSearcherOff)
            {
                this.MainViewModel.DialogService.ShowDialog(
                    Localizations.GSLocalization.Instance.QuestionText,
                    Localizations.GSLocalization.Instance.LeagueSearcherRunningText,
                    MessageDialogStyle.AffirmativeAndNegative,
                    GlobalManager.YesNoDialogSetting,
                    (tt) =>
                    {
                        if (tt.Result == MessageDialogResult.Affirmative)
                            LeagueSearcher.Instance.ChangeSearching(null);

                        if (this.notificator.IsEnabled && Properties.Settings.Default.AskNotificatorOff)
                        {
                            this.MainViewModel.DialogService.ShowDialog(
                                Localizations.GSLocalization.Instance.QuestionText,
                                Localizations.GSLocalization.Instance.NotificatorRunningText,
                                MessageDialogStyle.AffirmativeAndNegative,
                                GlobalManager.YesNoDialogSetting,
                                (ttt) =>
                                {
                                    if (ttt.Result == MessageDialogResult.Affirmative)
                                        this.notificator.IsEnabled = false;

                                    OpenHostingWindow();
                                });
                        }
                        else
                        { OpenHostingWindow(); }
                    });
            }
            else
            {
                OpenHostingWindow();
            }
        }

        private void JoinClose(string wa)
        {
            this.JoinGame(wa, false, true);
        }

        private void JoinGame(string wa)
        {
            this.JoinGame(wa, false, false);
        }

        private void JoinGame(string wa, bool silent, bool exit)
        {
            if (this.SelectedGame == null)
            {
                return;
            }

            if (this.MainViewModel.GameProcess != null)
            {
                this.MainViewModel.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.WAIsRunningText);
                return;
            }

            if (!CheckWAExe())
            {
                return;
            }

            this.joinedGame = this.SelectedGame;

            if (this.leagueSearcher.IsEnabled && Properties.Settings.Default.AskLeagueSearcherOff)
            {
                this.MainViewModel.DialogService.ShowDialog(
                    Localizations.GSLocalization.Instance.QuestionText,
                    Localizations.GSLocalization.Instance.LeagueSearcherRunningText,
                    MessageDialogStyle.AffirmativeAndNegative,
                    GlobalManager.YesNoDialogSetting,
                    (tt) =>
                    {
                        if (tt.Result == MessageDialogResult.Affirmative)
                            LeagueSearcher.Instance.ChangeSearching(null);

                        if (this.notificator.IsEnabled && Properties.Settings.Default.AskNotificatorOff)
                        {
                            this.MainViewModel.DialogService.ShowDialog(
                                Localizations.GSLocalization.Instance.QuestionText,
                                Localizations.GSLocalization.Instance.NotificatorRunningText,
                                MessageDialogStyle.AffirmativeAndNegative,
                                GlobalManager.YesNoDialogSetting,
                                (ttt) =>
                                {
                                    if (ttt.Result == MessageDialogResult.Affirmative)
                                        this.notificator.IsEnabled = false;

                                    DoJoinGame(wa, silent, exit);
                                });
                        }
                        else
                        { DoJoinGame(wa, silent, exit); }
                    });
            }
            else
            {
                DoJoinGame(wa, silent, exit);
            }
        }

        private void LeaveChannel(string message = null)
        {
            if (this.Joined)
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = Localizations.GSLocalization.Instance.PartMessage;
                }
                this.AddMessage(this.Server.User, message, MessageSettings.PartMessage);
                this.Server.LeaveChannel(this, this.Name);
                this.Joined = false;
            }
        }

        private void OpenChat(User u)
        {
            if (u.IsBanned || u.Name == this.Server.User.Name)
            {
                return;
            }

            // Test if we already have an opened chat with the user
            var chvm = this.MainViewModel.AllChannels.FirstOrDefault(x => x.Name == u.Name && x.Server == this.Server);
            if (chvm != null)
            {
                if (this.MainViewModel.SelectedChannel != chvm)
                {
                    this.MainViewModel.SelectChannel(chvm);
                }
                else
                {
                    this.MainViewModel.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.MainViewModel.DialogService.GetView().UpdateLayout();
                        chvm.IsTBFocused = true;
                    }));
                }
                return;
            }

            // Make new channel
            var newchvm = new PMChannelViewModel(this.MainViewModel, this.Server, u.Name);
            this.MainViewModel.SelectChannel(newchvm);
        }

        private void OpenChatDClick()
        {
            if (UserListDG.SelectedItem == null)
            {
                return;
            }

            this.OpenChatCommand.Execute(UserListDG.SelectedItem);
        }

        private void OpenHostingWindow()
        {
            DI _di;

            HostingWindow window = _di.Resolve<HostingWindow>();
            window.Init(this);
            window.Owner = this.MainViewModel.DialogService.GetView();
            window.ShowDialog();
        }

        private void SilentJoin(string wa)
        {
            this.JoinGame(wa, true, false);
        }

        private void SilentJoinClose(string wa)
        {
            this.JoinGame(wa, true, true);
        }

        void userListDG_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                var obj = sender as DataGrid;

                if (obj.SelectedItem == null)
                {
                    e.Handled = true;
                    return;
                }

                var u = (User)obj.SelectedItem;

                var chat = (MenuItem)obj.ContextMenu.Items[0];
                if (u.Name.Equals(this.Server.User.Name, StringComparison.OrdinalIgnoreCase) == false)
                {
                    chat.CommandParameter = u;
                    chat.IsEnabled = true;
                }
                else
                {
                    chat.IsEnabled = false;
                }

                var conversation = (MenuItem)obj.ContextMenu.Items[1];
                conversation.Header = Localizations.GSLocalization.Instance.AddToConvText;
                if (u.CanConversation && u.Name.Equals(this.Server.User.Name, StringComparison.OrdinalIgnoreCase) == false && this.MainViewModel.SelectedChannel != null && this.MainViewModel.SelectedChannel is PMChannelViewModel)
                {
                    var chvm = (PMChannelViewModel)this.MainViewModel.SelectedChannel;
                    if (chvm.Users.Count > 0 && chvm.Users[0].CanConversation)
                    {
                        conversation.CommandParameter = u;
                        if (chvm.IsUserInConversation(u))
                        {
                            conversation.Header = Localizations.GSLocalization.Instance.RemoveFromConvText;
                            if (chvm.Users.Count == 1)
                            {
                                conversation.IsEnabled = false;
                            }
                            else
                            {
                                conversation.IsEnabled = true;
                            }
                        }
                        else
                        {
                            conversation.IsEnabled = true;
                        }
                    }
                    else
                    {
                        conversation.IsEnabled = false;
                    }
                }
                else
                {
                    conversation.IsEnabled = false;
                }

                var group = (MenuItem)obj.ContextMenu.Items[2];
                if (this.RegenerateGroupsMenu)
                {
                    group.Items.Clear();
                    var defItem = new MenuItem()
                    {
                        Header = Localizations.GSLocalization.Instance.NoGroupText
                    };
                    defItem.Command = AddUserToDefaultGroupCommand;
                    group.Items.Add(defItem);

                    foreach (var item in UserGroups.Groups)
                    {
                        var menuItem = new MenuItem()
                        {
                            Header = item.Value.Name,
                            Foreground = item.Value.TextColor,
                            Tag = item.Value
                        };
                        menuItem.Command = AddUserToGroupCommand;
                        group.Items.Add(menuItem);
                    }
                }

                bool first = true;
                foreach (MenuItem item in group.Items)
                {
                    if (first)
                    {
                        first = false;
                        item.CommandParameter = u;
                    }
                    else
                    {
                        item.CommandParameter = new KeyValuePair<User, UserGroup>(u, (UserGroup)item.Tag);
                    }
                    item.FontWeight = FontWeights.Normal;
                    item.FontStyle = FontStyles.Normal;
                }

                if (u.Group.ID == UserGroups.SystemGroupID)
                {
                    ((MenuItem)group.Items[0]).FontWeight = FontWeights.Bold;
                    ((MenuItem)group.Items[0]).FontStyle = FontStyles.Italic;
                }
                else if (group.Items.Count > u.Group.ID + 1)
                {
                    ((MenuItem)group.Items[u.Group.ID + 1]).FontWeight = FontWeights.Bold;
                    ((MenuItem)group.Items[u.Group.ID + 1]).FontStyle = FontStyles.Italic;
                }

                var ignore = (MenuItem)obj.ContextMenu.Items[3];
                ignore.CommandParameter = u.Name;
                if (u.IsBanned)
                {
                    ignore.Header = Localizations.GSLocalization.Instance.RemoveIgnoreText;
                }
                else
                {
                    ignore.Header = Localizations.GSLocalization.Instance.AddIgnoreText;
                }

                var history = (MenuItem)obj.ContextMenu.Items[4];
                history.CommandParameter = u;

                var tusInfo = (MenuItem)obj.ContextMenu.Items[5];
                if (u.TusAccount != null)
                {
                    tusInfo.CommandParameter = u.TusAccount.TusLink;
                    tusInfo.Header = string.Format(Localizations.GSLocalization.Instance.ViewTusText, u.TusAccount.TusNick);
                    tusInfo.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    tusInfo.Visibility = System.Windows.Visibility.Collapsed;
                }

                var tusClanInfo = (MenuItem)obj.ContextMenu.Items[6];
                if (u.TusAccount != null && string.IsNullOrWhiteSpace(u.TusAccount.Clan) == false)
                {
                    tusClanInfo.CommandParameter = "http://www.tus-wa.com/groups/" + u.TusAccount.Clan + "/";
                    tusClanInfo.Header = string.Format(Localizations.GSLocalization.Instance.ViewClanProfileText, u.TusAccount.Clan);
                    tusClanInfo.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    tusClanInfo.Visibility = System.Windows.Visibility.Collapsed;
                }

                var appinfo = (MenuItem)obj.ContextMenu.Items[7];
                if (string.IsNullOrWhiteSpace(u.ClientName) == false)
                {
                    appinfo.Header = string.Format(Localizations.GSLocalization.Instance.InfoText, u.ClientName);
                    appinfo.Visibility = Visibility.Visible;
                }
                else
                {
                    appinfo.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
                e.Handled = true;
            }
        }
    }
}