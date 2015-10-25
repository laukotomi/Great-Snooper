using GreatSnooper.ViewModel;
using System.Windows.Controls;

namespace GreatSnooper.UserControls
{
    public partial class ConnectedLayout : Border
    {
        public ConnectedLayout(AbstractChannelViewModel chvm)
        {
            this.DataContext = chvm;
            InitializeComponent();
        }
    }
}
