using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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

            // General settings
            ShowLoginScreen.IsChecked = Properties.Settings.Default.ShowLoginScreen;
            if (Properties.Settings.Default.AutoLogIn)
                ShowLoginScreen.IsEnabled = true;
            MessageEnabled.IsChecked = Properties.Settings.Default.MessageJoinedGame;
            MarkAway.IsChecked = Properties.Settings.Default.MarkAway;
            AwayText.Text = Properties.Settings.Default.AwayText;
            SendBack.IsChecked = Properties.Settings.Default.SendBack;
            BackText.Text = Properties.Settings.Default.BackText;
            MessageTime.IsChecked = Properties.Settings.Default.MessageTime;
            WAExeText.Text = Properties.Settings.Default.WaExe;
            AutoJoinAnythingGoes.IsChecked = Properties.Settings.Default.AutoJoinAnythingGoes;
            DeleteOldLogs.IsChecked = Properties.Settings.Default.DeleteLogs;

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
                SoundChanged(obj.Name);
            }
        }

        private void SoundEnabledChange(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            SoundEnabledChanged(obj.Name, obj.IsChecked.Value);
        }

        private void MessageEnabledChanged(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            Properties.Settings.Default.MessageJoinedGame = obj.IsChecked.Value;
            Properties.Settings.Default.Save();
        }

        private void FontChange(object sender, RoutedEventArgs e)
        {
            var obj = sender as Button;
            Grid g = obj.Parent as Grid;
            int row = (int)obj.GetValue(Grid.RowProperty);
            string title = string.Empty;
            for (int i = 0; i < g.Children.Count; i++)
            {
                if ((int)g.Children[i].GetValue(Grid.RowProperty) == row && (int)g.Children[i].GetValue(Grid.ColumnProperty) == 0)
                    title = (string)((Label)g.Children[i]).Content;
            }
            var window = new FontChooser(obj.Name, title);
            window.SaveSettings += MyFunction;
            window.Closing += FontChooserClosing;
            window.Owner = this;
            window.ShowDialog();
        }

        private void FontChooserClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var obj = sender as FontChooser;
            obj.Closing -= FontChooserClosing;
            obj.SaveSettings -= MyFunction;
        }

        private void MyFunction()
        {
            MessageSettingsEvent();
        }

        private void MarkAwayClick(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            Properties.Settings.Default.MarkAway = obj.IsChecked.Value;
            Properties.Settings.Default.Save();
        }

        private void AwayTextChanged(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            var obj = sender as TextBox;
            Properties.Settings.Default.AwayText = obj.Text;
            Properties.Settings.Default.Save();
        }

        private void SendBackClick(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            Properties.Settings.Default.SendBack = obj.IsChecked.Value;
            Properties.Settings.Default.Save();
        }

        private void BackTextChanged(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            var obj = sender as TextBox;
            Properties.Settings.Default.BackText = obj.Text;
            Properties.Settings.Default.Save();
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
            if (result == true)
            {
                // Open document 
                WAExeText.Text = dlg.FileName;
                Properties.Settings.Default.WaExe = dlg.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void MessageTimeChanged(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            Properties.Settings.Default.MessageTime = obj.IsChecked.Value;
            Properties.Settings.Default.Save();
            MessageSettingsEvent();
        }

        private void ShowLoginScreenChanged(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            Properties.Settings.Default.ShowLoginScreen = obj.IsChecked.Value;
            Properties.Settings.Default.Save();
        }

        private void AutoJoinAnythingGoesChanged(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            Properties.Settings.Default.AutoJoinAnythingGoes = obj.IsChecked.Value;
            Properties.Settings.Default.Save();
        }

        private void DeleteOldLogsChanged(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            Properties.Settings.Default.DeleteLogs = obj.IsChecked.Value;
            Properties.Settings.Default.Save();
        }
    }
}
