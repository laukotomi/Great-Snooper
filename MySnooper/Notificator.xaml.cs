using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MySnooper
{
    public delegate void NotificatorDelegate(List<NotificatorClass> list);

    /// <summary>
    /// Interaction logic for Notificator.xaml
    /// </summary>
    public partial class Notificator : MetroWindow
    {
        private bool searching;

        public event NotificatorDelegate NotificatorEvent;

        public Notificator(bool searching)
        {
            InitializeComponent();

            this.searching = searching;
            if (searching)
            {
                StartButton.Content = "Stop searching";
                TheList.IsEnabled = false;
            }

            string[] ncs = Properties.Settings.Default.Notificator.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < ncs.Length; i++)
            {
                string[] data = ncs[i].Split(new char[] { ',' });
                if (data.Length == 8)
                {
                    NotificatorClass nc = new NotificatorClass();
                    nc.WordsAsText = data[0].Replace(":", Environment.NewLine);
                    nc.InGameNames = data[1] == "True";
                    nc.InHosterNames = data[2] == "True";
                    nc.InJoinMessages = data[3] == "True";
                    nc.InMessages = data[4] == "True";
                    nc.InMessageSenders = data[5] == "True";
                    nc.IsEnabled = data[6] == "True";
                    nc.MatchType = (NotificatorClass.MatchTypes)Convert.ToInt32(data[7]);
                    this.TheList.Items.Add(nc);
                }
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
            e.Handled = true;
        }

        private void AddNewNotificator(object sender, RoutedEventArgs e)
        {
            TheList.Items.Add(new NotificatorClass());
        }

        private void StartSearching(object sender, RoutedEventArgs e)
        {
            if (searching) // Stop!
            {
                StartButton.Content = "Start searching";
                NotificatorEvent(null);
                searching = false;
                TheList.IsEnabled = true;
            }
            else // Start!
            {
                List<NotificatorClass> list = new List<NotificatorClass>();
                StringBuilder sb = new StringBuilder();

                foreach (var item in TheList.Items)
                {
                    NotificatorClass nc = (NotificatorClass)item;

                    sb.Append(nc.WordsAsText.Trim().Replace(Environment.NewLine, ":"));
                    sb.Append(',');
                    sb.Append(nc.InGameNames.ToString());
                    sb.Append(',');
                    sb.Append(nc.InHosterNames.ToString());
                    sb.Append(',');
                    sb.Append(nc.InJoinMessages.ToString());
                    sb.Append(',');
                    sb.Append(nc.InMessages.ToString());
                    sb.Append(',');
                    sb.Append(nc.InMessageSenders.ToString());
                    sb.Append(',');
                    sb.Append(nc.IsEnabled.ToString());
                    sb.Append(',');
                    sb.Append(((int)nc.MatchType).ToString());
                    sb.Append('|');
                    
                    if (nc.IsEnabled && nc.Words.Count > 0 && (nc.InGameNames || nc.InHosterNames || nc.InJoinMessages || nc.InMessages || nc.InMessageSenders))
                        list.Add(nc);
                }

                if (list.Count > 0)
                {
                    Properties.Settings.Default.Notificator = sb.ToString();
                    Properties.Settings.Default.Save();
                    NotificatorEvent(list);
                    this.Close();
                }
            }
            e.Handled = true;

        }

        private void RemoveNotificator(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure to remove?", "Confirmation needed", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                TheList.Items.Remove((NotificatorClass)((Button)sender).DataContext);
            }
        }
    }
}
