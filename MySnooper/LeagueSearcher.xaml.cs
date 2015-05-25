using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MySnooper
{
    public delegate void LookForThese(object sender, LookForTheseEventArgs e);

    /// <summary>
    /// Interaction logic for LeagueSearcher.xaml
    /// </summary>
    public partial class LeagueSearcher : MetroWindow
    {
        private Dictionary<string, string> leaguesToSearch;
        private Dictionary<string, string> lookingForThese;
        private bool searching;

        public event LookForThese LuckyLuke;

        public LeagueSearcher() { } // Never used, but visual stdio throws an error if not exists
        public LeagueSearcher(Dictionary<string, string> Leagues, bool searching)
        {
            InitializeComponent();

            leaguesToSearch = new Dictionary<string, string>();
            this.lookingForThese = new Dictionary<string, string>();
            this.lookingForThese.DeSerialize(Properties.Settings.Default.SearchForThese);
            TheList.ItemsSource = Leagues;

            this.searching = searching;
            if (searching)
            {
                StartButton.Content = "Stop searching";
                TheList.IsEnabled = false;
                this.Spam.IsEnabled = false;
            }
            else
                this.Spam.IsEnabled = GlobalManager.SpamAllowed;

            if (GlobalManager.SpamAllowed)
                this.Spam.IsChecked = Properties.Settings.Default.SpammingChecked;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            this.Close();
        }


        private void StartSearching(object sender, RoutedEventArgs e)
        {
            if (searching) // Stop!
            {
                StartButton.Content = "Start searching";
                LuckyLuke(this, null);
                searching = false;
                TheList.IsEnabled = true;
                this.Spam.IsEnabled = GlobalManager.SpamAllowed;
            }
            else if (leaguesToSearch.Count != 0) // Start!
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in leaguesToSearch)
                {
                    sb.Append(item.Key);
                    sb.Append(',');
                }

                Properties.Settings.Default.SearchForThese = sb.ToString();
                if (Spam.IsEnabled)
                {
                    Properties.Settings.Default.SpammingChecked = Spam.IsChecked.Value;
                }
                Properties.Settings.Default.Save();

                LuckyLuke(this, new LookForTheseEventArgs(leaguesToSearch, Spam.IsChecked.Value));
                this.Close();
            }
            e.Handled = true;
        }

        private void CheckChanged(object sender, RoutedEventArgs e)
        {
            var obj = (CheckBox)sender;
            var data = (KeyValuePair<string, string>)obj.DataContext;
            if (obj.IsChecked.Value)
            {
                leaguesToSearch.Add(data.Key.ToLower(), data.Key);
            }
            else
            {
                leaguesToSearch.Remove(data.Key.ToLower());
            }
            e.Handled = true;
        }

        private void CheckLoaded(object sender, RoutedEventArgs e)
        {
            var obj = (CheckBox)sender;
            var data = (KeyValuePair<string, string>)obj.DataContext;
            obj.IsChecked = lookingForThese.ContainsKey(data.Key.ToLower());
            if (obj.IsChecked.Value)
            {
                leaguesToSearch.Add(data.Key.ToLower(), data.Key);
            }
            e.Handled = true;
        }

        private void TheListLostFocus(object sender, RoutedEventArgs e)
        {
            TheList.SelectedIndex = -1;
        }
    }
}
