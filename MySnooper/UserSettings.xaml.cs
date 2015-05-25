using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;

namespace MySnooper
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>

    public enum SettingChangedType { NoType, Sound, Style, UserGroup, UserGroupPlayer, UserGroupColor };
    public delegate void SettingChangedDelegate(object sender, SettingChangedEventArgs e);

    public partial class UserSettings : MetroWindow
    {
        public event SettingChangedDelegate SettingChanged;

        public UserSettings()
        {
            InitializeComponent();

            // General settings
            AddBoolSetting(GeneralSettingsGrid, "AutoLogIn", "Auto login at startup:");
            AddTextListSetting(GeneralSettingsGrid, "AutoJoinChannels", "Join these channels on startup:", "Channel list");
            AddBoolSetting(GeneralSettingsGrid, "MessageJoinedGame", "Send a message to the channel if I join a game:");
            AddBoolSetting(GeneralSettingsGrid, "MarkAway", "Mark me away when I host or join a game:");
            AddTextSetting(GeneralSettingsGrid, "AwayText", "Away message:", new IRCTextValidator());
            AddBoolSetting(GeneralSettingsGrid, "SendBack", "Send back message in private chats if I am back:");
            AddTextSetting(GeneralSettingsGrid, "BackText", "Back message:", new IRCTextValidator());
            AddBoolSetting(GeneralSettingsGrid, "DeleteLogs", "Delete channel logs older than 30 days at startup:");
            AddTextSetting(GeneralSettingsGrid, "QuitMessagee", "Quit message:", new GSVersionValidator());
            WAExeText.Text = Properties.Settings.Default.WaExe;

            // User groups
            foreach (var item in UserGroups.Groups)
            {
                AddUserGroupSetting(UserGroupsGrid, item.Value, new NotEmptyValidator());
            }

            // Window
            AddBoolSetting(WindowGrid, "ShowBannedUsers", "Show banned users in user list:");
            AddBoolSetting(WindowGrid, "ShowBannedMessages", "Show messages of banned users in the channels:");
            AddBoolSetting(WindowGrid, "ShowInfoColumn", "Show information column in user list:");
            AddBoolSetting(WindowGrid, "CloseToTray", "Exit button minimizes the snooper to tray:");
            AddBoolSetting(WindowGrid, "EnergySaveModeGame", "Energy save mode while playing:");
            AddBoolSetting(WindowGrid, "EnergySaveModeWin", "Energy save mode when snooper is hidden:");
            AddBoolSetting(WindowGrid, "WAHighPriority", "Set W:A task priority to high:");

            // Notifications
            AddBoolSetting(NotificationsGrid, "AskNotificatorOff", "Ask if I would like to turn off notificator when I host or join a game:");
            AddBoolSetting(NotificationsGrid, "AskLeagueSearcherOff", "Ask if I would like to turn off league searcher when I host or join a game:");
            AddBoolSetting(NotificationsGrid, "TrayNotifications", "Enable tray balloon messages:");
            AddBoolSetting(NotificationsGrid, "TrayFlashing", "Enable tray flashing:");

            // #worms
            AddBoolSetting(WormsGrid, "ShowWormsChannel", "Show #worms channel:");
            AddTextSetting(WormsGrid, "WormsNick", "#worms channel nickname:", new NickNameValidator());
            //AddTextSetting(GeneralSettingsGrid, "WormsPassword", "#worms channel password:");
            AddBoolSetting(WormsGrid, "ChangeWormsNick", "Update #worms nick to WormNet nick at startup:");

            // Message styles
            AddBoolSetting(MessagesGrid, "MessageTime", "Show the time when the message arrived:");
            AddBoolSetting(MessagesGrid, "ActionMessageWithGT", "Action message using '>' character:");
            AddStyleSetting(MessageStylesGrid, "UserMessageStyle", "Style of your message:", MessageSettings.UserMessage);
            AddStyleSetting(MessageStylesGrid, "ChannelMessageStyle", "Style of other users' message:", MessageSettings.ChannelMessage);
            AddStyleSetting(MessageStylesGrid, "JoinMessageStyle", "Join message style:", MessageSettings.JoinMessage);
            AddStyleSetting(MessageStylesGrid, "PartMessageStyle", "Part message style:", MessageSettings.PartMessage);
            AddStyleSetting(MessageStylesGrid, "QuitMessageStyle", "Quit message style:", MessageSettings.QuitMessage);
            AddStyleSetting(MessageStylesGrid, "ActionMessageStyle", "Action message style:", MessageSettings.ActionMessage);
            AddStyleSetting(MessageStylesGrid, "NoticeMessageStyle", "Notice message style:", MessageSettings.NoticeMessage);
            AddStyleSetting(MessageStylesGrid, "OfflineMessageStyle", "Offline user message style:", MessageSettings.OfflineMessage);
            AddStyleSetting(MessageStylesGrid, "MessageTimeStyle", "Style of message arrived time:", MessageSettings.MessageTimeStyle);
            AddStyleSetting(MessageStylesGrid, "HyperLinkStyle", "Style of hyperlinks:", MessageSettings.HyperLinkStyle);
            AddStyleSetting(MessageStylesGrid, "LeagueFoundMessageStyle", "Found text style:", MessageSettings.LeagueFoundMessage);

            // Sounds
            AddSoundSetting(SoundsGrid, "PMBeep", "PMBeepEnabled", "Private message arrived:");
            AddSoundSetting(SoundsGrid, "HBeep", "HBeepEnabled", "When your name appears in chat:");
            AddSoundSetting(SoundsGrid, "LeagueFoundBeep", "LeagueFoundBeepEnabled", "When the snooper finds a league game:");
            AddSoundSetting(SoundsGrid, "LeagueFailBeep", "LeagueFailBeepEnabled", "When the snooper stops searching league game:");
            AddSoundSetting(SoundsGrid, "NotificatorSound", "NotificatorSoundEnabled", "When the notificator finds something:");

            GenerateGroupSounds();

            // About
            Version.Text = "Version: " + App.GetVersion();

            this.UpdateLayout();
        }

        private void GenerateGroupSounds()
        {
            GroupSoundsGrid.Children.Clear();
            GroupSoundsGrid.RowDefinitions.Clear();
            foreach (var item in UserGroups.Groups)
            {
                AddSoundSetting(GroupSoundsGrid, item.Value.SettingName + "Sound", item.Value.SettingName + "SoundEnabled", "A player from " + item.Value.Name + " comes online:");
            }
        }

        private void AddBoolSetting(Grid grid, string name, string text)
        {
            int row = grid.RowDefinitions.Count;
            bool value = (bool)(Properties.Settings.Default.GetType().GetProperty(name).GetValue(Properties.Settings.Default, null));

            grid.RowDefinitions.Add(new RowDefinition() { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });

            // <Label Grid.Column="0" Grid.Row="1" Content="Auto login at startup:"></Label>
            TextBlock tb = new TextBlock();
            tb.Text = text;
            if (name == "buddiesGroup")
                tb.IsEnabled = false;
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 0);
            grid.Children.Add(tb);

            // <CheckBox Name="AutoLogin" Grid.Column="1" Grid.Row="1" HorizontalAlignment="Left" IsEnabled="False" Click="ShowLoginScreenChanged"></CheckBox>
            CheckBox cb = new CheckBox();
            cb.Focusable = false;
            cb.Tag = name;
            cb.IsChecked = value;
            cb.Click += BoolHandler;
            Grid.SetRow(cb, row);
            Grid.SetColumn(cb, 1);
            grid.Children.Add(cb);
        }

        private void BoolHandler(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (cb.IsChecked.HasValue)
            {
                string name = (string)cb.Tag;
                Properties.Settings.Default.GetType().GetProperty(name).SetValue(Properties.Settings.Default, cb.IsChecked.Value, null);
                Properties.Settings.Default.Save();

                if (SettingChanged != null)
                    SettingChanged(this, new SettingChangedEventArgs(name, SettingChangedType.NoType));
            }
        }

        private void AddTextSetting(Grid grid, string name, string text, MyValidator validator)
        {
            int row = grid.RowDefinitions.Count;
            string value = (string)(Properties.Settings.Default.GetType().GetProperty(name).GetValue(Properties.Settings.Default, null));

            grid.RowDefinitions.Add(new RowDefinition() { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });

            // <Label Grid.Column="0" Grid.Row="1" Content="Auto login at startup:"></Label>
            TextBlock tb = new TextBlock();
            tb.Text = text;
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 0);
            grid.Children.Add(tb);

            // <TextBox Name="BackText" Grid.Column="1" Grid.Row="6" LostKeyboardFocus="BackTextChanged"></TextBox>
            TextBox tb2 = new TextBox();
            tb2.Tag = new object[] { name, validator };
            tb2.Text = value;
            tb2.LostKeyboardFocus += TextHandler;
            Grid.SetRow(tb2, row);
            Grid.SetColumn(tb2, 1);
            grid.Children.Add(tb2);
        }

        private void TextHandler(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            object[] tag = (object[])tb.Tag;
            string name = (string)tag[0];
            MyValidator validator = (MyValidator)tag[1];
            string text = tb.Text;

            string error = validator.Validate(ref text);
            if (error != string.Empty)
            {
                System.Windows.MessageBox.Show(error, "Invalid value", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            Properties.Settings.Default.GetType().GetProperty(name).SetValue(Properties.Settings.Default, text, null);
            Properties.Settings.Default.Save();

            if (SettingChanged != null)
                SettingChanged(this, new SettingChangedEventArgs(name, SettingChangedType.NoType));
        }

        private void AddStyleSetting(Grid grid, string name, string text, MessageSetting setting)
        {
            int row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition() { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });

            // <Label Grid.Column="0" Grid.Row="1" Content="Auto login at startup:"></Label>
            TextBlock tb = new TextBlock();
            tb.Text = text;
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 0);
            grid.Children.Add(tb);

            Run run, run2 = null;
            TextBlock tb2 = new TextBlock();
            tb2.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            if (setting.OneColorOnly)
            {
                run = new Run("Example");
                MessageSettings.LoadSettingsFor(run, setting);
                run.Foreground = setting.NickColor;
                tb2.Inlines.Add(run);
            }
            else
            {
                run = new Run("Player: ");
                MessageSettings.LoadSettingsFor(run, setting);
                run.FontWeight = FontWeights.Bold;
                run.Foreground = setting.NickColor;
                tb2.Inlines.Add(run);
                run2 = new Run("message");
                MessageSettings.LoadSettingsFor(run2, setting);
                run2.Foreground = setting.MessageColor;
                tb2.Inlines.Add(run2);
            }
            Grid.SetRow(tb2, row);
            Grid.SetColumn(tb2, 1);
            grid.Children.Add(tb2);

            // <Button Grid.Row="1" Grid.Column="1" Content="Change" Click="FontChange" Name="UserMessage"></Button>
            Button b = new Button();
            b.Focusable = false;
            b.Tag = new object[] { name, text, setting, run, run2 };
            b.Content = "Change";
            b.Click += StyleHandler;
            Grid.SetRow(b, row);
            Grid.SetColumn(b, 2);
            grid.Children.Add(b);
        }

        private Button helper;
        private void StyleHandler(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            helper = b;
            object[] tag = (object[])b.Tag;
            var window = new FontChooser((string)tag[0], (string)tag[1], (MessageSetting)tag[2]);
            window.SaveSetting += window_SaveSetting;
            window.Owner = this;
            window.ShowDialog();
        }

        void window_SaveSetting(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                object[] tag = (object[])helper.Tag;
                MessageSetting setting = (MessageSetting)tag[2];
                Run run = (Run)tag[3];
                MessageSettings.LoadSettingsFor(run, setting);
                run.Foreground = setting.NickColor;

                if (setting.OneColorOnly == false)
                {
                    Run run2 = (Run)tag[4];
                    MessageSettings.LoadSettingsFor(run2, setting);
                    run2.Foreground = setting.MessageColor;
                }

                if (SettingChanged != null)
                    SettingChanged(this, new SettingChangedEventArgs((string)tag[0], SettingChangedType.Style));
            }
            ));
        }

        private void AddSoundSetting(Grid grid, string name, string name2, string text)
        {
            int row = grid.RowDefinitions.Count;
            string value = (string)(Properties.Settings.Default.GetType().GetProperty(name).GetValue(Properties.Settings.Default, null));
            bool value2 = (bool)(Properties.Settings.Default.GetType().GetProperty(name2).GetValue(Properties.Settings.Default, null));

            grid.RowDefinitions.Add(new RowDefinition() { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });

            /*
                <Label Grid.Column="0" Grid.Row="5" Content="When a buddy comes online:"></Label>
                <StackPanel Grid.Column="1" Grid.Row="5" Orientation="Horizontal">
                    <TextBox Name="BJBeep" Width="210" IsReadOnly="True"></TextBox>
                    <Button Name="BJBeepChange" Content="Browse" Width="75" Click="SoundChange"></Button>
                </StackPanel>
                <CheckBox Name="BJBeepEnabled" Click="SoundEnabledChange" Grid.Column="2" Grid.Row="5" Content="Enable"></CheckBox>
            */

            // <Label Grid.Column="0" Grid.Row="1" Content="Auto login at startup:"></Label>
            TextBlock tb = new TextBlock();
            tb.Text = text;
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 0);
            grid.Children.Add(tb);

            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            Grid.SetRow(sp, row);
            Grid.SetColumn(sp, 1);

            TextBox tb2 = new TextBox();
            tb2.IsReadOnly = true;
            tb2.Width = 210;
            tb2.Text = value;
            sp.Children.Add(tb2);

            Button b = new Button();
            b.Focusable = false;
            b.Content = "Browse";
            b.Width = 75;
            b.Click += SoundChange;
            b.Tag = new object[] { tb2, name };
            sp.Children.Add(b);

            grid.Children.Add(sp);

            CheckBox cb = new CheckBox();
            cb.Focusable = false;
            cb.Content = "Enabled";
            cb.IsChecked = value2;
            cb.Click += BoolHandler;
            cb.Tag = name2;
            Grid.SetRow(cb, row);
            Grid.SetColumn(cb, 2);
            grid.Children.Add(cb);
        }


        private void SoundChange(object sender, RoutedEventArgs e)
        {
            object[] data = (object[])((Button)sender).Tag;
            TextBox tb = (TextBox)data[0];

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "WAV Files|*.wav";
            if (File.Exists(tb.Text))
                dlg.InitialDirectory = new FileInfo(tb.Text).Directory.FullName;

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result.HasValue && result.Value == true)
            {
                string filename = dlg.FileName;
                string name = (string)data[1];
                tb.Text = filename;
                Properties.Settings.Default.GetType().GetProperty(name).SetValue(Properties.Settings.Default, filename, null);
                Properties.Settings.Default.Save();

                if (SettingChanged != null)
                    SettingChanged(this, new SettingChangedEventArgs(name, SettingChangedType.Sound));
            }
        }

        private void AddTextListSetting(Grid grid, string name, string text, string editortext)
        {
            int row = grid.RowDefinitions.Count;

            grid.RowDefinitions.Add(new RowDefinition() { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });

            // <Label Grid.Column="0" Grid.Row="1" Content="Auto login at startup:"></Label>
            TextBlock tb = new TextBlock();
            tb.Text = text;
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 0);
            grid.Children.Add(tb);

            // <TextBox Name="BackText" Grid.Column="1" Grid.Row="6" LostKeyboardFocus="BackTextChanged"></TextBox>
            Button bt = new Button();
            bt.Focusable = false;
            bt.Content = "Edit";
            bt.Tag = new string[] { name, editortext };
            bt.Click += TextListHandler;
            Grid.SetRow(bt, row);
            Grid.SetColumn(bt, 1);
            grid.Children.Add(bt);
        }

        private void TextListHandler(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            string[] data = (string[])((Button)sender).Tag;
            ListEditor window = new ListEditor(data[0], data[1]);
            window.Owner = this;
            window.ShowDialog();
        }

        private void AddUserGroupSetting(Grid grid, UserGroup ug, MyValidator validator)
        {
            int row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition() { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });

            // <TextBox Name="BackText" Grid.Column="1" Grid.Row="6" LostKeyboardFocus="BackTextChanged"></TextBox>
            TextBox tb2 = new TextBox();
            tb2.Tag = new object[] { ug, validator };
            tb2.Text = ug.Name;
            tb2.LostKeyboardFocus += GroupTextHandler;
            if (ug.ID == UserGroups.BuddiesGroupID)
                tb2.IsEnabled = false;
            Grid.SetRow(tb2, row);
            Grid.SetColumn(tb2, 0);
            grid.Children.Add(tb2);

            var cp = new ColorPicker();
            cp.Focusable = false;
            cp.SelectedColor = ug.GroupColor.Color;
            cp.SelectedColorChanged += GroupColorChanged;
            cp.Tag = ug;
            Grid.SetRow(cp, row);
            Grid.SetColumn(cp, 2);
            grid.Children.Add(cp);

            Button bt = new Button();
            bt.Focusable = false;
            bt.Content = "Players";
            bt.Tag = ug;
            bt.Click += GroupTextListHandler;
            Grid.SetRow(bt, row);
            Grid.SetColumn(bt, 4);
            grid.Children.Add(bt);
        }

        UserGroup groupColorHelper = null;

        private void GroupColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            var obj = (ColorPicker)sender;
            var group = (UserGroup)obj.Tag;
            group.GroupColor = new SolidColorBrush(obj.SelectedColor);
            if (groupColorHelper == null)
                groupColorHelper = group;
            else if (groupColorHelper.ID != group.ID)
            {
                groupColorHelper.SaveSettings(); // explicit save
                if (SettingChanged != null)
                    SettingChanged(this, new SettingChangedEventArgs(groupColorHelper.SettingName, SettingChangedType.UserGroupColor, groupColorHelper));
                groupColorHelper = group;
            }

            if (SettingChanged != null)
                SettingChanged(this, new SettingChangedEventArgs(group.SettingName, SettingChangedType.UserGroup));
        }

        private void GroupTextHandler(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            object[] tag = (object[])tb.Tag;
            var group = (UserGroup)tag[0];
            MyValidator validator = (MyValidator)tag[1];
            string text = tb.Text.Trim();

            string error = validator.Validate(ref text);
            if (error != string.Empty)
            {
                System.Windows.MessageBox.Show(error, "Invalid value", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            group.Name = text;
            group.SaveSettings();
            GenerateGroupSounds();

            if (SettingChanged != null)
                SettingChanged(this, new SettingChangedEventArgs(group.SettingName, SettingChangedType.UserGroup));
        }

        UserGroup openedUG = null;
        private void GroupTextListHandler(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            UserGroup ug = (UserGroup)((Button)sender).Tag;
            openedUG = ug;
            ListEditor window = new ListEditor(ug.SettingName + "List", ug.Name);
            window.ItemRemoved += window_ItemRemoved;
            window.ItemAdded += window_ItemAdded;
            window.Owner = this;
            window.ShowDialog();
        }

        void window_ItemAdded(object sender, StringEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                string lowerName = e.Argument.ToLower();
                openedUG.Users.Add(lowerName, e.Argument);
                if (!UserGroups.Users.ContainsKey(lowerName))
                {
                    UserGroups.Users.Add(lowerName, openedUG);
                }
                else if (UserGroups.Users[lowerName].ID > openedUG.ID)
                {
                    UserGroups.Users[lowerName] = openedUG;
                }

                if (SettingChanged != null)
                    SettingChanged(this, new SettingChangedEventArgs(e.Argument, SettingChangedType.UserGroupPlayer, openedUG));
            }
            ));
        }

        void window_ItemRemoved(object sender, StringEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                string lowerName = e.Argument.ToLower();
                openedUG.Users.Remove(lowerName);
                UserGroups.Users.Remove(lowerName);

                foreach (var item in UserGroups.Groups)
                {
                    if (item.Value.Users.ContainsKey(lowerName))
                    {
                        UserGroups.Users.Add(lowerName, item.Value);
                        break;
                    }
                }

                if (SettingChanged != null)
                    SettingChanged(this, new SettingChangedEventArgs(e.Argument, SettingChangedType.UserGroupPlayer, openedUG));
            }
            ));
        }

        private void WAExeChange(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Worms Armageddon Exe|*.exe";
            if (File.Exists(WAExeText.Text))
                dlg.InitialDirectory = new FileInfo(WAExeText.Text).Directory.FullName;


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result.HasValue && result.Value == true)
            {
                WAExeText.Text = dlg.FileName;
                Properties.Settings.Default.WaExe = dlg.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (groupColorHelper != null)
            {
                groupColorHelper.SaveSettings();
                if (SettingChanged != null)
                    SettingChanged.BeginInvoke(this, new SettingChangedEventArgs(groupColorHelper.Name, SettingChangedType.UserGroupColor, groupColorHelper), null, null);
            }
        }
    }
}
