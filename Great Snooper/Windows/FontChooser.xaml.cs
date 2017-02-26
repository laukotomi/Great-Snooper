namespace GreatSnooper.Windows
{
    using System.Windows.Controls;

    using GreatSnooper.Services;
    using GreatSnooper.Settings;
    using GreatSnooper.ViewModel;

    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;

    public partial class FontChooser : MetroWindow
    {
        private MetroDialogService dialogService;
        private FontChooserViewModel vm;

        public FontChooser(StyleSetting style)
        {
            this.vm = new FontChooserViewModel(style);
            this.dialogService = new MetroDialogService(this);
            this.vm.DialogService = dialogService;
            this.DataContext = vm;
            this.Closing += this.vm.ClosingRequest;
            InitializeComponent();
        }

        private void FontFamilyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FontFamilyListBox.SelectionChanged -= FontFamilyListBox_SelectionChanged;
            FontFamilyListBox.IsEnabled = true;
            FontFamilyListBox.ScrollIntoView(FontFamilyListBox.SelectedItem);
        }
    }
}