using System.Windows.Controls;

namespace GreatSnooper.UserControls
{
    public partial class GameListLayout : Grid
    {
        public GameListLayout()
        {
            InitializeComponent();
        }

        private void ListBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            e.Handled = true;
            ((ListBox)sender).SelectedItem = null;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
