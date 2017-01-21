using System.Windows;
using System.Windows.Controls;
using GreatSnooper.Classes;
using GreatSnooper.Services;
using GreatSnooper.ViewModel;
using MahApps.Metro.Controls;

namespace GreatSnooper.Windows
{
    public partial class MainWindow : MetroWindow
    {
        private MainViewModel vm;

        public MainWindow(WormNetCommunicator wormNetC, ITaskbarIconService taskbarIconService)
        {
            InitializeComponent();

            this.WindowState = (WindowState)Properties.Settings.Default.WindowState;
            if (Properties.Settings.Default.WindowWidth != 0)
                this.Width = Properties.Settings.Default.WindowWidth;
            if (Properties.Settings.Default.WindowHeight != 0)
                this.Height = Properties.Settings.Default.WindowHeight;

            this.vm = new MainViewModel(new MetroDialogService(this), taskbarIconService, wormNetC);
            taskbarIconService.Icon.DataContext = this.vm;
            taskbarIconService.Icon.ContextMenu = (ContextMenu)this.FindResource("taskbarContextMenu");
            taskbarIconService.Icon.DoubleClickCommand = this.vm.ActivationCommand;
            this.ContentRendered += this.vm.ContentRendered;
            this.Closing += this.vm.ClosingRequest;
            this.DataContext = vm;
        }
    }
}
