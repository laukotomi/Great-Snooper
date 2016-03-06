using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Services;
using GreatSnooper.ViewModel;
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
