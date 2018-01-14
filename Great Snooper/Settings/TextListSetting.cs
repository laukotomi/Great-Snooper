namespace GreatSnooper.Settings
{
    using System.Windows.Input;

    using GalaSoft.MvvmLight.Command;

    using GreatSnooper.Services;
    using GreatSnooper.Validators;
    using GreatSnooper.Windows;

    class TextListSetting : AbstractSetting
    {
        private IMetroDialogService dialogService;
        private string editorTitle;
        private AbstractValidator validator;

        public TextListSetting(string settingName, string text, string editorTitle, IMetroDialogService dialogService, AbstractValidator validator)
        : base(settingName, text)
        {
            this.dialogService = dialogService;
            this.editorTitle = editorTitle;
            this.validator = validator;
        }

        public ICommand ListEditorCommand
        {
            get
            {
                return new RelayCommand(OpenListEditor);
            }
        }

        private void OpenListEditor()
        {
            var window = new ListEditor(this.SettingName, this.editorTitle, this.validator);
            window.Owner = this.dialogService.GetView();
            window.ShowDialog();
        }
    }
}