using GreatSnooper.Services;
using GreatSnooper.Settings;
using GreatSnooper.ViewModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Controls;

namespace GreatSnooper.Windows
{
    public partial class FontChooser : MetroWindow
    {
        private FontChooserViewModel vm;
        private MetroDialogService dialogService;

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
