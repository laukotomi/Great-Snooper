namespace GreatSnooper.Windows
{
    using GreatSnooper.Helpers;
    using GreatSnooper.Services;
    using GreatSnooper.ViewModel;

    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;

    public partial class NotificatorWindow : MetroWindow
    {
        private NotificatorViewModel vm;

        public NotificatorWindow()
        {
            this.vm = new NotificatorViewModel();
            this.vm.DialogService = new MetroDialogService(this);
            this.DataContext = vm;
            this.Closing += this.vm.ClosingRequest;
            InitializeComponent();
        }

        private void NotificatorHelp(object sender, System.Windows.RoutedEventArgs e)
        {
            e.Handled = true;
            this.ShowMessageAsync(Localizations.GSLocalization.Instance.InformationText, Localizations.GSLocalization.Instance.NotificatorHelpText, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting);
        }
    }
}