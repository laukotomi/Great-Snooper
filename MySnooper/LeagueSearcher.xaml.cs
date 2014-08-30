using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MySnooper
{
    public delegate void LookForThese(Dictionary<string, string> leagues, bool spam);

    /// <summary>
    /// Interaction logic for LeagueSearcher.xaml
    /// </summary>
    public partial class LeagueSearcher : MetroWindow
    {
        private Dictionary<string, string> LeaguesToSearch;
        private Dictionary<string, List<string>> LookingForThese;
        private bool Searching;
        public event LookForThese LuckyLuke;

        public LeagueSearcher() { } // Never used, but visual stdio throws an error if not exists
        public LeagueSearcher(Dictionary<string, string> Leagues, Dictionary<string, List<string>> LookingForThese, bool Spam, Channel SearchHere)
        {
            InitializeComponent();

            this.LookingForThese = LookingForThese;
            LeaguesToSearch = new Dictionary<string, string>();
            TheList.ItemsSource = Leagues;

            if (SearchHere != null)
            {
                StartButton.Content = "Stop searching";
                Searching = true;
                TheList.IsEnabled = false;
                this.Spam.IsEnabled = false;
            }
            else
                Searching = false;

            this.Spam.IsChecked = Spam;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
            e.Handled = true;
        }


        private void StartSearching(object sender, RoutedEventArgs e)
        {
            if (Searching) // Stop!
            {
                StartButton.Content = "Start searching";
                LuckyLuke(null, false);
                Searching = false;
                TheList.IsEnabled = true;
                this.Spam.IsEnabled = true;
            }
            else if (LeaguesToSearch.Count != 0) // Start!
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in LeaguesToSearch)
                {
                    sb.Append(item.Key);
                    sb.Append(',');
                }
                Properties.Settings.Default.SearchForThese = sb.ToString(0, sb.Length - 1);
                Properties.Settings.Default.Save();
                LuckyLuke(LeaguesToSearch, Spam.IsChecked.Value);
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
                LeaguesToSearch.Add(data.Key.ToLower(), data.Key);
            }
            else
            {
                LeaguesToSearch.Remove(data.Key.ToLower());
            }
            e.Handled = true;
        }

        private void CheckLoaded(object sender, RoutedEventArgs e)
        {
            if (LookingForThese != null)
            {
                var obj = (CheckBox)sender;
                var data = (KeyValuePair<string, string>)obj.DataContext;
                obj.IsChecked = LookingForThese.ContainsKey(data.Key.ToLower());
                if (obj.IsChecked.Value)
                {
                    LeaguesToSearch.Add(data.Key.ToLower(), data.Key);
                }
            }
            e.Handled = true;
        }

        private void TheListLostFocus(object sender, RoutedEventArgs e)
        {
            TheList.SelectedIndex = -1;
        }
    }
}
