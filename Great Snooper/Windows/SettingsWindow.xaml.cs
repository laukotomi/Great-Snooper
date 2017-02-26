namespace GreatSnooper.Windows
{
    using System;

    using GreatSnooper.Services;
    using GreatSnooper.ViewModel;

    using MahApps.Metro.Controls;

    public partial class SettingsWindow : MetroWindow, IDisposable
    {
        bool disposed = false;
        private SettingsViewModel vm;

        public SettingsWindow()
        {
            this.vm = new SettingsViewModel();
            vm.DialogService = new MetroDialogService(this);
            vm.LoadSettings();
            this.DataContext = this.vm;
            InitializeComponent();
        }

        ~SettingsWindow()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (disposing)
            {
                if (vm != null)
                {
                    vm.Dispose();
                    vm = null;
                }
            }
        }
    }
}