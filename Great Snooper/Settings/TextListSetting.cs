using GalaSoft.MvvmLight.Command;
using GreatSnooper.Services;
using GreatSnooper.Validators;
using GreatSnooper.Windows;
using System.Windows.Input;

namespace GreatSnooper.Settings
{
    class TextListSetting : AbstractSetting
    {
        #region Members
        private IMetroDialogService dialogService;
        private string editorTitle;
        private AbstractValidator validator;
        #endregion

        #region ListEditorCommand
        public ICommand ListEditorCommand
        {
            get { return new RelayCommand(OpenListEditor); }
        }

        private void OpenListEditor()
        {
            var window = new ListEditor(this.settingName, this.editorTitle, this.validator);
            window.Owner = this.dialogService.GetView();
            window.ShowDialog();
        }
        #endregion

        public TextListSetting(string settingName, string text, string editorTitle, IMetroDialogService dialogService, AbstractValidator validator)
            : base(settingName, text)
        {
            this.dialogService = dialogService;
            this.editorTitle = editorTitle;
            this.validator = validator;
        }
    }
}
