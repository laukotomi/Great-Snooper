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

        public MainWindow(WormNetCommunicator wormNetC)
        {
            InitializeComponent();

            this.vm = new MainViewModel(new MetroDialogService(this), new TaskbarIconService(myNotifyIcon), wormNetC);
            this.ContentRendered += this.vm.ContentRendered;
            this.Closing += this.vm.ClosingRequest;
            this.DataContext = vm;
        }
    }
}
