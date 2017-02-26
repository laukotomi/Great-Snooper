namespace GreatSnooper.UserControls
{
    using System.Windows.Controls;

    using GreatSnooper.ViewModel;

    public partial class ChannelTabControl : TabControl
    {
        private readonly ChannelTabControlViewModel _vm;

        public ChannelTabControl()
        {
            this._vm = new ChannelTabControlViewModel(this);
            this.DataContext = this._vm;
            InitializeComponent();
        }

        public int ColumnSpan
        {
            get;
            set;
        }

        public ChannelTabControlViewModel ViewModel
        {
            get
            {
                return this._vm;
            }
        }
    }
}