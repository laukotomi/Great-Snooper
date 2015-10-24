using GreatSnooper.EventArguments;
using GreatSnooper.Helpers;
using GreatSnooper.Services;
using GreatSnooper.ViewModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Diagnostics;
using System.Windows;

namespace GreatSnooper.Windows
{
    public partial class HostingWindow : MetroWindow
    {
        private HostingViewModel vm;

        public HostingWindow(MainViewModel mvm, string serverAddress, ChannelViewModel channel, string cc)
        {
            this.vm = new HostingViewModel(mvm, serverAddress, channel, cc);
            this.vm.DialogService = new MetroDialogService(this);
            this.DataContext = this.vm;
            InitializeComponent();
        }

        private void WormNatHelp(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            this.ShowMessageAsync(Localizations.GSLocalization.Instance.InformationText, Localizations.GSLocalization.Instance.AboutWormNat2Text, MessageDialogStyle.AffirmativeAndNegative, GlobalManager.MoreInfoDialogSetting).ContinueWith((t) =>
            {
                if (t.Result == MessageDialogResult.Affirmative)
                {
                    try
                    {
                        Process.Start("http://worms2d.info/Hosting");
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Log(ex);
                    }
                }
            });
        }
    }
}
