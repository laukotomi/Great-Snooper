using GreatSnooper.Services;
using GreatSnooper.ViewModel;
using MahApps.Metro.Controls;

namespace GreatSnooper.Windows
{
    public partial class NotificatorWindow : MetroWindow
    {
        private NotificatorViewModel vm;

        public NotificatorWindow()
        {
            this.vm = new NotificatorViewModel();
            this.vm.DialogService = new MetroDialogService(this);
            this.DataContext = vm;
            InitializeComponent();
        }
    }
}
