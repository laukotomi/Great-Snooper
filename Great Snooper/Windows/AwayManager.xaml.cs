using GreatSnooper.Services;
using GreatSnooper.ViewModel;
using MahApps.Metro.Controls;
using System.Windows;

namespace GreatSnooper.Windows
{
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
