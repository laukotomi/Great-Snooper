using GalaSoft.MvvmLight.Command;
using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.Windows;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace GreatSnooper.ViewModel
{
    public class PMChannelViewModel : AbstractChannelViewModel
    {
        #region Members
        private TextBlock headerTB;
        #endregion

        #region Properties
        public bool AwayMsgSent { get; set; }
        #endregion

        public PMChannelViewModel(MainViewModel mainViewModel, AbstractCommunicator server, string channelName)
            : base(mainViewModel, server)
        {
            this.Joined = true;

            var mainWindow = (MainWindow)mainViewModel.DialogService.GetView();
            tabitem = new TabItem();
            tabitem.DataContext = this;
            tabitem.Style = (Style)mainWindow.ChannelsTabControl.FindResource("pmChannelTabItem");
            tabitem.ApplyTemplate();
            this.headerTB = (TextBlock)tabitem.Template.FindName("ContentSite", tabitem);
            tabitem.Content = ConnectedLayout;

            string[] users = channelName.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string userName in users)
            {
                User u = null;
                if (!this.Server.Users.TryGetValue(userName, out u))
                    u = GreatSnooper.Helpers.Users.CreateUser(this.Server, userName);

                this.Users.Add(u);
                u.PMChannels.Add(this);
                u.PropertyChanged += UserPropertyChanged;
            }

            this.Name = string.Join(",", this.Users);
            server.Channels.Add(this.Name, this);

            if (this.Users[0].IsBanned == false)
            {
                this.GenerateHeader();
                mainViewModel.Channels.Add(this);
            }
        }

        public override void SendMessage(string message, bool userMessage = false)
        {
            if (this.Users.Count > 1)
            {
                // Broadcast
                foreach (User user in this.Users)
                {
                    if (user.OnlineStatus != User.Status.Offline && user.IsBanned == false)
                        Server.SendCTCPMessage(this, user.Name, "CMESSAGE", this.Name + "|" + message);
                }
            }
            else if (this.Users[0].OnlineStatus != User.Status.Offline)
                Server.SendMessage(this, this.Users[0].Name, message);

            AddMessage(Server.User, message, MessageSettings.UserMessage, userMessage);
        }

        public override void SendNotice(string message, bool userMessage = false)
        {
            if (this.Users.Count > 1)
            {
                // Broadcast
                foreach (User user in this.Users)
                {
                    if (user.OnlineStatus != User.Status.Offline && user.IsBanned == false)
                        Server.SendCTCPMessage(this, user.Name, "CNOTICE", this.Name + "|" + message);
                }
            }
            else if (this.Users[0].OnlineStatus != User.Status.Offline)
                Server.SendNotice(this, this.Users[0].Name, message);

            AddMessage(Server.User, message, MessageSettings.NoticeMessage, userMessage);
        }

        public override void SendActionMessage(string message, bool userMessage = false)
        {
            if (this.Users.Count > 1)
            {
                // Broadcast
                foreach (User user in this.Users)
                {
                    if (user.OnlineStatus != User.Status.Offline && user.IsBanned == false)
                        Server.SendCTCPMessage(this, user.Name, "CACTION", this.Name + "|" + message);
                }
            }
            else if (this.Users[0].OnlineStatus != User.Status.Offline)
                Server.SendCTCPMessage(this, this.Users[0].Name, "ACTION", message);

            AddMessage(Server.User, message, MessageSettings.ActionMessage, userMessage);
        }

        public override void SendCTCPMessage(string ctcpCommand, string ctcpText, User except = null)
        {
            // Broadcast
            foreach (User user in this.Users)
            {
                if (user.OnlineStatus != User.Status.Offline && user.IsBanned == false && (except == null || user != except))
                    Server.SendCTCPMessage(this, user.Name, ctcpCommand, ctcpText);
            }
        }

        public override void ProcessMessage(IRCTasks.MessageTask msgTask)
        {
            // If user was removed from conversation and then added to it again but the channel tab remaint open
            if (this.Disabled)
                this.Disabled = false;

            var msg = new Message(msgTask.User, msgTask.Message, msgTask.Setting);
            if (msgTask.Setting.Type == Message.MessageTypes.Channel || msgTask.Setting.Type == Message.MessageTypes.Quit || msgTask.Setting.Type == Message.MessageTypes.Action || msgTask.Setting.Type == Message.MessageTypes.Notice)
            {
                var matches = urlRegex.Matches(msgTask.Message);
                for (int i = 0; i < matches.Count; i++)
                {
                    this.HandleUriMatch(matches[i].Groups[0], msg);
                }
            }

            // This way away message will be added to the channel later than the arrived message
            this.AddMessage(msg);

            if (!msgTask.User.IsBanned)
            {
                this.Highlight();
                this.MainViewModel.FlashWindow();
                if (Properties.Settings.Default.TrayNotifications && (this.MainViewModel.SelectedChannel != this || this.MainViewModel.IsWindowActive == false))
                    this.MainViewModel.ShowTrayMessage(msgTask.User.Name + ": " + msgTask.Message);
                if (Properties.Settings.Default.PMBeepEnabled)
                    Sounds.PlaySoundByName("PMBeep");

                // Send away message if needed
                if (msgTask.Setting.Type != Message.MessageTypes.Notice && this.MainViewModel.IsAway && this.AwayMsgSent == false)
                {
                    this.AwayMsgSent = true;
                    if (msgTask.User.UsingGreatSnooper2)
                        this.SendCTCPMessage("AWAY", this.MainViewModel.AwayText);
                    else
                        this.SendMessage(this.MainViewModel.AwayText + " (Away message)");
                }
            }
        }

        private void UserPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "OnlineStatus":
                case "Name":
                    GenerateHeader();
                    break;

                case "IsBanned":
                    if (this.Users.Count == 1)
                    {
                        var u = (User)sender;
                        if (u.IsBanned)
                            this.MainViewModel.CloseChannelTab(this);
                        else
                            this.MainViewModel.Channels.Add(this);
                    }
                    else
                        GenerateHeader();
                    break;
            }

        }

        public override TabItem GetLayout()
        {
            return tabitem;
        }

        #region MouseEnteredHeaderCommand
        public ICommand MouseEnteredHeaderCommand
        {
            get { return new RelayCommand(MouseEnteredHeader); }
        }

        private void MouseEnteredHeader()
        {
            this.GenerateHeader(true);
        }
        #endregion

        #region MouseLeftHeaderCommand
        public ICommand MouseLeftHeaderCommand
        {
            get { return new RelayCommand(MouseLeftHeader); }
        }

        private void MouseLeftHeader()
        {
            this.GenerateHeader();
        }
        #endregion

        #region MouseLeftHeaderCommand
        public RelayCommand<MouseButtonEventArgs> CloseChannelMBCommand
        {
            get { return new RelayCommand<MouseButtonEventArgs>(CloseChannelMB); }
        }

        private void CloseChannelMB(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
            {
                e.Handled = true;
                this.MainViewModel.CloseChannelCommand.Execute(this);
            }
        }
        #endregion

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
            if (this.headerTB.Inlines.Count != this.Users.Count * 2 - 1)
            {
                this.headerTB.Inlines.Clear();
                foreach (User u in this.Users)
                {
                    this.headerTB.Inlines.Add(new Run(u.Name));
                    if (i + 1 < this.Users.Count)
                        this.headerTB.Inlines.Add(new Run(" | "));
                    i++;
                }
            }

            i = 0;
            int j = 0;
            foreach (Inline inline in this.headerTB.Inlines)
            {
                if (i % 2 == 1)
                {
                    i++;
                    continue;
                }

                if (this.IsHighlighted)
                    inline.FontWeight = FontWeights.Bold;
                else
                    inline.FontWeight = FontWeights.Normal;

                switch (this.Users[j].OnlineStatus)
                {
                    case User.Status.Offline:
                        if (this.MainViewModel.SelectedChannel == this)
                            inline.Foreground = Brushes.Red;
                        else if (isMouseOver)
                            inline.Foreground = Brushes.Firebrick;
                        else
                            inline.Foreground = Brushes.DarkRed;
                        break;
                    case User.Status.Online:
                        if (this.MainViewModel.SelectedChannel == this)
                            inline.Foreground = Brushes.GreenYellow;
                        else if (isMouseOver)
                            inline.Foreground = Brushes.YellowGreen;
                        else
                            inline.Foreground = Brushes.Green;
                        break;
                    case User.Status.Unknown:
                        if (this.MainViewModel.SelectedChannel == this)
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

        internal void AddUserToConversation(User u, bool broadcast = true, bool canModifyChannel = true)
        {
            if (u.CanConversation == false)
                return;

            this.Users.Add(u);
            string newName = string.Join(",", this.Users);

            if (canModifyChannel)
            {
                // Test if we already have an opened chat with the users
                var chvm = this.MainViewModel.Channels.FirstOrDefault(x => x.Name == newName && x.Server == this.Server);
                if (chvm != null)
                {
                    if (this.MainViewModel.SelectedChannel != chvm)
                        this.MainViewModel.SelectChannel(chvm);
                    else
                    {
                        this.MainViewModel.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.MainViewModel.DialogService.GetView().UpdateLayout();
                            chvm.IsTBFocused = true;
                        }));
                    }
                    this.Users.Remove(u); // Undo modifications
                    return;
                }
                else
                {
                    u.PMChannels.Add(this);
                    u.PropertyChanged += UserPropertyChanged;
                }
            }

            if (broadcast && this.Messages.Count > 0)
                this.SendCTCPMessage("CLIENTADD", this.Name + "|" + u.Name, u);

            // update hashname
            this.Server.Channels.Remove(this.Name);
            this.Name = newName;
            this.Server.Channels.Add(this.Name, this);
            this.GenerateHeader();
        }

        internal void RemoveUserFromConversation(User u, bool broadcast = true, bool canModifyChannel = true)
        {
            if (!this.IsUserInConversation(u))
                return;

            this.Users.Remove(u);
            string newName = string.Join(",", this.Users);

            if (canModifyChannel)
            {
                // Test if we already have an opened chat with the user(s)
                var chvm = this.MainViewModel.Channels.FirstOrDefault(x => x.Name == newName && x.Server == this.Server);
                if (chvm != null)
                {
                    if (this.MainViewModel.SelectedChannel != chvm)
                        this.MainViewModel.SelectChannel(chvm);
                    else
                    {
                        this.MainViewModel.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.MainViewModel.DialogService.GetView().UpdateLayout();
                            chvm.IsTBFocused = true;
                        }));
                    }
                    this.Users.Add(u); // Undo
                    return;
                }
                else
                {
                    u.PMChannels.Remove(this);
                    u.PropertyChanged -= UserPropertyChanged;
                }

                if (u.Channels.Count == 0 && u.PMChannels.Count == 0)
                    GreatSnooper.Helpers.Users.FinalizeUser(this.Server, u);
            }

            if (broadcast && this.Messages.Count > 0)
            {
                this.SendCTCPMessage("CLIENTREM", this.Name + "|" + u.Name);
                if (u.OnlineStatus != User.Status.Offline)
                    this.Server.SendCTCPMessage(this, u.Name, "CLIENTREM", this.Name + "|" + u.Name);
            }

            // update hashname
            this.Server.Channels.Remove(this.Name);
            this.Name = newName;
            this.Server.Channels.Add(this.Name, this);
            this.GenerateHeader();
        }

        public bool IsUserInConversation(User u)
        {
            foreach (var user in this.Users)
            {
                if (u == user)
                    return true;
            }
            return false;
        }

        public override void SetLoading(bool loading = true)
        {
            this.Loading = loading;
            this.Disabled = loading;
        }

        public override void ClearUsers()
        {
            foreach (User u in this.Users)
            {
                u.PMChannels.Remove(this);
                u.PropertyChanged -= UserPropertyChanged;
            }
            this.Users.Clear();
        }
    }
}
