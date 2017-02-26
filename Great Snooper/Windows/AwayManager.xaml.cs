namespace GreatSnooper.Windows
{
    using System.Windows;

    using GreatSnooper.Services;
    using GreatSnooper.ViewModel;

    using MahApps.Metro.Controls;

    public partial class AwayManager : MetroWindow
    {
        private AwayViewModel vm;

        public AwayManager(MainViewModel mvm, string awayText)
        {
            this.vm = new AwayViewModel(mvm, awayText);
            this.vm.DialogService = new MetroDialogService(this);
            this.DataContext = this.vm;
            InitializeComponent();
        }
    }
}