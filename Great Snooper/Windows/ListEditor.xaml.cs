namespace GreatSnooper.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using GreatSnooper.Helpers;
    using GreatSnooper.Services;
    using GreatSnooper.Validators;
    using GreatSnooper.ViewModel;

    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;

    public partial class ListEditor : MetroWindow
    {
        private ListEditorViewModel vm;

        public ListEditor(IEnumerable<string> list, string title, Action<string> addAction, Action<string> removeAction, AbstractValidator validator = null)
        {
            this.vm = new ListEditorViewModel(list, title, addAction, removeAction, validator);
            this.vm.DialogService = new MetroDialogService(this);
            this.ContentRendered += vm.ContentRendered;
            this.DataContext = vm;
            InitializeComponent();
        }

        public ListEditor(string settingName, string title, AbstractValidator validator)
        {
            this.vm = new ListEditorViewModel(settingName, title, validator);
            this.vm.DialogService = new MetroDialogService(this);
            this.DataContext = vm;
            InitializeComponent();
        }

        public enum ListModes
        {
            Users, Normal, Setting
        }

        private void AddToList(object sender, KeyEventArgs e)
        {
            var obj = sender as TextBox;
            if (e.Key == Key.Enter && obj.Text.Length > 0)
            {
                vm.AddCommand.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void AddToListEnter(object sender, KeyboardFocusChangedEventArgs e)
        {
            var obj = sender as TextBox;
            if (obj.Text == (string)obj.Tag)
            {
                obj.Clear();
            }
        }

        private void AddToListLeave(object sender, KeyboardFocusChangedEventArgs e)
        {
            var obj = sender as TextBox;
            if (obj.Text.Trim() == string.Empty)
            {
                obj.Text = (string)obj.Tag;
            }
        }

        private void InformationClicked(object sender, RoutedEventArgs e)
        {
            this.ShowMessageAsync(Localizations.GSLocalization.Instance.InformationText, Localizations.GSLocalization.Instance.ListEditorInfoText, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting);
        }

        private void MyListView_LostFocus(object sender, RoutedEventArgs e)
        {
            var obj = (ListBox)sender;
            obj.SelectedIndex = -1;
        }
    }
}