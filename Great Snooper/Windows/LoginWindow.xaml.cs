using GreatSnooper.Helpers;
using GreatSnooper.Services;
using GreatSnooper.ViewModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GreatSnooper.Windows
{
    public partial class Login : MetroWindow, IDisposable
    {
        private LoginViewModel vm;

        public Login()
        {
            this.vm = new LoginViewModel();
            this.DataContext = this.vm;
            this.ContentRendered += this.vm.ContentRendered;
            this.Closing += this.vm.ClosingRequest;

            InitializeComponent();
            vm.DialogService = new MetroDialogService(this);
            vm.TaskbarIconService = new TaskbarIconService(myNotifyIcon);
        }

        private void ServerHelp(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            this.ShowMessageAsync(Localizations.GSLocalization.Instance.InformationText, Localizations.GSLocalization.Instance.ServerInfoText, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting);
        }

        private void TusLoginHelp(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            this.ShowMessageAsync(Localizations.GSLocalization.Instance.InformationText, Localizations.GSLocalization.Instance.WhatIsTusLoginText, MessageDialogStyle.AffirmativeAndNegative, GlobalManager.MoreInfoDialogSetting).ContinueWith((t) =>
            {
                if (t.Result == MessageDialogResult.Affirmative)
                {
                    try
                    {
                        Process.Start("http://www.tus-wa.com/forums/announcements/bringing-back-wn-ranks-and-registered-usernames-4819/");
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Log(ex);
                    }
                }
            });
        }

        private void ClanHelp(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            this.ShowMessageAsync(Localizations.GSLocalization.Instance.InformationText, Localizations.GSLocalization.Instance.ClanInfoText, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting);
        }

        private void SnooperRankHelp(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            this.ShowMessageAsync(Localizations.GSLocalization.Instance.InformationText, Localizations.GSLocalization.Instance.UseSnooperRankHelp, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting);
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

        ~Login()
        {
            Dispose(false);
        }

        #endregion
    }
}
