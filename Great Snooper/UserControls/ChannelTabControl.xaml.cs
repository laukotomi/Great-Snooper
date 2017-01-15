using GreatSnooper.ViewModel;
using System.Windows.Controls;

namespace GreatSnooper.UserControls
{
    public partial class ChannelTabControl : TabControl
    {
        private readonly ChannelTabControlViewModel _vm;

        public ChannelTabControlViewModel ViewModel
        {
            get { return this._vm; }
        }

        public ChannelTabControl()
        {
            this._vm = new ChannelTabControlViewModel(this);
            this.DataContext = this._vm;
            InitializeComponent();
        }
    }
}
