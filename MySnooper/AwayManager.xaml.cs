using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Input;

namespace MySnooper
{
    public delegate void AwayChangedDelegate(bool Away);
    /// <summary>
    /// Interaction logic for AwayManager.xaml
    /// </summary>
    public partial class AwayManager : MetroWindow
    {
        public event AwayChangedDelegate AwayChanged;

        private bool _Away;
        private bool Away {
            get
            {
                return _Away;
            }
            set
            {
                _Away = value;

                if (value)
                {
                    AwayButton.Content = "Set back";
                    AwayText.IsEnabled = false;
                }
                else
                {
                    AwayButton.Content = "Set away";
                    AwayText.IsEnabled = true;
                }
            }
        }


        public AwayManager() { } // Never used, but visual stdio throws an error if not exists
        public AwayManager(string text)
        {
            InitializeComponent();

            Away = text != string.Empty;
            AwayText.Text = Properties.Settings.Default.AwayMessage;
        }

        private void AwayClick(object sender, RoutedEventArgs e)
        {
            if (Away)
                Away = false;
            else
            {
                Away = true;
                Properties.Settings.Default.AwayMessage = WormNetCharTable.RemoveNonWormNetChars(AwayText.Text.Trim());
                Properties.Settings.Default.Save();
            }

            AwayChanged(Away);
            e.Handled = true;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
            e.Handled = true;
        }
    }
}
