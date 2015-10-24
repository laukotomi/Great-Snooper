using GreatSnooper.Services;
using GreatSnooper.ViewModel;
using MahApps.Metro.Controls;
using System;

namespace GreatSnooper.Windows
{
    public partial class SettingsWindow : MetroWindow, IDisposable
    {
        private SettingsViewModel vm;

        public SettingsWindow()
        {
            this.vm = new SettingsViewModel();
            vm.DialogService = new MetroDialogService(this);
            vm.LoadSettings();
            this.DataContext = this.vm;
            InitializeComponent();
        }

        #region IDisposable
        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

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

        ~SettingsWindow()
        {
            Dispose(false);
        }

        #endregion
    }
}
