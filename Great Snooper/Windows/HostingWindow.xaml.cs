namespace GreatSnooper.Windows
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using GreatSnooper.Helpers;
    using GreatSnooper.Services;
    using GreatSnooper.ViewModel;
    using GreatSnooper.ViewModelInterfaces;
    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;

    public partial class HostingWindow : MetroWindow
    {
        private readonly DI _di;
        private IHostingViewModel _vm;

        public HostingWindow(DI di)
        {
            _vm = di.Resolve<IHostingViewModel>();
        }

        public void Init(ChannelViewModel channel)
        {
            IMetroDialogService dialogService = _di.Resolve<IMetroDialogService>();
            dialogService.Init(this);

            _vm.Init(dialogService, channel);
            DataContext = _vm;
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