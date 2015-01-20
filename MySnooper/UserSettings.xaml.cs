using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Documents;

namespace MySnooper
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    
    public delegate void MessageSettingChangedDelegate();
    public delegate void SoundChangedDelegate(string name);
    public delegate void SoundEnabledChangeDelegate(string name, bool enabled);

    public partial class UserSettings : MetroWindow
    {
        public event MessageSettingChangedDelegate MessageSettingsEvent;
        public event SoundChangedDelegate SoundChanged;
        public event SoundEnabledChangeDelegate SoundEnabledChanged;

        public UserSettings()
        {
            InitializeComponent();

            AddBoolSetting(GeneralSettingsGrid, "AutoLogIn", "Auto login at startup:");
            AddBoolSetting(GeneralSettingsGrid, "MessageJoinedGame", "Send message to the channel if I join a game:");
            AddBoolSetting(GeneralSettingsGrid, "MarkAway", "Mark me away when I host or join a game:");
            AddTextSetting(GeneralSettingsGrid, "AwayText", "Away message:");
            AddBoolSetting(GeneralSettingsGrid, "SendBack", "Send back message in private chats if I am back:");
            AddTextSetting(GeneralSettingsGrid, "BackText", "Back message:");
            AddBoolSetting(GeneralSettingsGrid, "MessageTime", "Show the time when the message arrived:");
            AddBoolSetting(GeneralSettingsGrid, "DeleteLogs", "Delete channel logs older than 30 days at startup:");

            // General settings
            //AutoLogin.IsChecked = Properties.Settings.Default.AutoLogIn;
            //MessageEnabled.IsChecked = Properties.Settings.Default.MessageJoinedGame;
            //MarkAway.IsChecked = Properties.Settings.Default.MarkAway;
            //AwayText.Text = Properties.Settings.Default.AwayText;
            //SendBack.IsChecked = Properties.Settings.Default.SendBack;
            //BackText.Text = Properties.Settings.Default.BackText;
            //MessageTime.IsChecked = Properties.Settings.Default.MessageTime;
            WAExeText.Text = Properties.Settings.Default.WaExe;
            //AutoJoinAnythingGoes.IsChecked = Properties.Settings.Default.AutoJoinAnythingGoes;
            //DeleteOldLogs.IsChecked = Properties.Settings.Default.DeleteLogs;

            // Message styles
            AddStyleSetting(MessageStylesGrid, "UserMessage", "Style of your message:", MessageSettings.UserMessage);
            AddStyleSetting(MessageStylesGrid, "ChannelMessage", "Style of other users' message:", MessageSettings.ChannelMessage);
            AddStyleSetting(MessageStylesGrid, "JoinMessage", "Join message style:", MessageSettings.JoinMessage);
            AddStyleSetting(MessageStylesGrid, "PartMessage", "Part message style:", MessageSettings.PartMessage);
            AddStyleSetting(MessageStylesGrid, "QuitMessage", "Quit message style:", MessageSettings.QuitMessage);
            AddStyleSetting(MessageStylesGrid, "ActionMessage", "Action message style:", MessageSettings.ActionMessage);
            AddStyleSetting(MessageStylesGrid, "NoticeMessage", "Notice message style:", MessageSettings.NoticeMessage);
            AddStyleSetting(MessageStylesGrid, "OfflineMessage", "Offline user message style:", MessageSettings.OfflineMessage);
            AddStyleSetting(MessageStylesGrid, "BuddyJoinedMessage", "Buddy joined message style:", MessageSettings.BuddyJoinedMessage);
            AddStyleSetting(MessageStylesGrid, "MessageTimeStyle", "Style of message arrived time:", MessageSettings.MessageTimeStyle);
            AddStyleSetting(MessageStylesGrid, "HyperLinkStyle", "Style of hyperlinks:", MessageSettings.HyperLinkStyle);
            AddStyleSetting(MessageStylesGrid, "LeagueFoundMessage", "Found league text style:", MessageSettings.LeagueFoundMessage);
            
            // Sounds
            PMBeep.Text = Properties.Settings.Default.PMBeep;
            PMBeepEnabled.IsChecked = Properties.Settings.Default.PMBeepEnabled;

            HBeep.Text = Properties.Settings.Default.HBeep;
            HBeepEnabled.IsChecked = Properties.Settings.Default.HBeepEnabled;

            BJBeep.Text = Properties.Settings.Default.BJBeep;
            BJBeepEnabled.IsChecked = Properties.Settings.Default.BJBeepEnabled;

            LeagueFoundBeep.Text = Properties.Settings.Default.LeagueFoundBeep;
            LeagueFoundBeepEnabled.IsChecked = Properties.Settings.Default.LeagueFoundBeepEnabled;

            LeagueFailBeep.Text = Properties.Settings.Default.LeagueFailBeep;
            LeagueFailBeepEnabled.IsChecked = Properties.Settings.Default.LeagueFailBeepEnabled;

            // About
            Version.Text = "Version: " + App.getVersion();
        }

        private void AddBoolSetting(Grid grid, string name, string text)
        {
            int row = grid.RowDefinitions.Count;
            bool value = (bool)(Properties.Settings.Default.GetType().GetProperty(name).GetValue(Properties.Settings.Default));

            grid.RowDefinitions.Add(new RowDefinition() { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5) });

            // <Label Grid.Column="0" Grid.Row="1" Content="Auto login at startup:"></Label>
            Label label = new Label();
            label.Content = text;
            Grid.SetRow(label, row);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            // <CheckBox Name="AutoLogin" Grid.Column="1" Grid.Row="1" HorizontalAlignment="Left" IsEnabled="False" Click="ShowLoginScreenChanged"></CheckBox>
            CheckBox cb = new CheckBox();
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
                Properties.Settings.Default.GetType().GetProperty(name).SetValue(Properties.Settings.Default, cb.IsChecked.Value);
                Properties.Settings.Default.Save();
            }
        }

        private void AddTextSetting(Grid grid, string name, string text)
        {
            int row = grid.RowDefinitions.Count;
            string value = (string)(Properties.Settings.Default.GetType().GetProperty(name).GetValue(Properties.Settings.Default));

            grid.RowDefinitions.Add(new RowDefinition() { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5) });

            // <Label Grid.Column="0" Grid.Row="1" Content="Auto login at startup:"></Label>
            Label label = new Label();
            label.Content = text;
            Grid.SetRow(label, row);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            // <TextBox Name="BackText" Grid.Column="1" Grid.Row="6" LostKeyboardFocus="BackTextChanged"></TextBox>
            TextBox tb = new TextBox();
            tb.Tag = name;
            tb.Text = value;
            tb.LostKeyboardFocus += TextHandler;
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 1);
            grid.Children.Add(tb);
        }

        private void TextHandler(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            string name = (string)tb.Tag;
            Properties.Settings.Default.GetType().GetProperty(name).SetValue(Properties.Settings.Default, tb.Text);
            Properties.Settings.Default.Save();
        }

        private void AddStyleSetting(Grid grid, string name, string text, MessageSetting setting)
        {
            int row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition() { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5) });

            // <Label Grid.Column="0" Grid.Row="1" Content="Auto login at startup:"></Label>
            Label label = new Label();
            label.Content = text;
            Grid.SetRow(label, row);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            TextBlock tb = new TextBlock();
            tb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            Run run = new Run("Example");
            MessageSettings.LoadSettingsFor(run, setting);
            tb.Inlines.Add(run);
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 1);
            grid.Children.Add(tb);

            // <Button Grid.Row="1" Grid.Column="1" Content="Change" Click="FontChange" Name="UserMessage"></Button>
            Button b = new Button();
            b.Tag = new object[] { name, text, setting, run };
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

        void window_SaveSetting()
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                object[] tag = (object[])helper.Tag;
                Run run = (Run)tag[3];
                MessageSetting setting = (MessageSetting)tag[2];
                MessageSettings.LoadSettingsFor(run, setting);
            }
            ));
        }

        private void SoundChange(object sender, RoutedEventArgs e)
        {
            var obj = sender as Button;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "WAV Files|*.wav";
            switch (obj.Name)
            {
                case "PMBeepChange":
                    if (File.Exists(PMBeep.Text))
                        dlg.InitialDirectory = new FileInfo(PMBeep.Text).Directory.FullName;
                    break;
                case "HbeepChange":
                    if (File.Exists(HBeep.Text))
                        dlg.InitialDirectory = new FileInfo(HBeep.Text).Directory.FullName;
                    break;
                case "BJBeepChange":
                    if (File.Exists(BJBeep.Text))
                        dlg.InitialDirectory = new FileInfo(BJBeep.Text).Directory.FullName;
                    break;
                case "LeagueFoundBeepChange":
                    if (File.Exists(LeagueFoundBeep.Text))
                        dlg.InitialDirectory = new FileInfo(LeagueFoundBeep.Text).Directory.FullName;
                    break;
                case "LeagueFailBeepChange":
                    if (File.Exists(LeagueFailBeep.Text))
                        dlg.InitialDirectory = new FileInfo(LeagueFailBeep.Text).Directory.FullName;
                    break;
            }

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;

                switch (obj.Name)
                {
                    case "PMBeepChange":
                        PMBeep.Text = filename;
                        Properties.Settings.Default.PMBeep = filename;
                        break;
                    case "HbeepChange":
                        HBeep.Text = filename;
                        Properties.Settings.Default.HBeep = filename;
                        break;
                    case "BJBeppChange":
                        BJBeep.Text = filename;
                        Properties.Settings.Default.BJBeep = filename;
                        break;
                    case "LeagueFoundBeepChange":
                        LeagueFoundBeep.Text = filename;
                        Properties.Settings.Default.LeagueFoundBeep = filename;
                        break;
                    case "LeagueFailBeepChange":
                        LeagueFailBeep.Text = filename;
                        Properties.Settings.Default.LeagueFailBeep = filename;
                        break;
                }
                Properties.Settings.Default.Save();
                if (SoundChanged != null)
                    SoundChanged(obj.Name);
            }
        }

        private void SoundEnabledChange(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            if (SoundEnabledChanged != null)
                SoundEnabledChanged(obj.Name, obj.IsChecked.Value);
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
                // Open document 
                WAExeText.Text = dlg.FileName;
                Properties.Settings.Default.WaExe = dlg.FileName;
                Properties.Settings.Default.Save();
            }
        }
    }
}
