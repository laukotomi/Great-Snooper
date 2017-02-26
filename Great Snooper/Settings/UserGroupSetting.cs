namespace GreatSnooper.Settings
{
    using System.Windows.Input;
    using System.Windows.Media;

    using GalaSoft.MvvmLight.Command;

    using GreatSnooper.Helpers;
    using GreatSnooper.Model;
    using GreatSnooper.Services;
    using GreatSnooper.Validators;
    using GreatSnooper.Windows;

    class UserGroupSetting : AbstractSetting
    {
        private IMetroDialogService dialogService;
        private UserGroup group;
        private AbstractValidator validator;

        public UserGroupSetting(UserGroup group, AbstractValidator validator, IMetroDialogService dialogService)
        : base(string.Empty, string.Empty)
        {
            this.group = group;
            this.validator = validator;
            this.dialogService = dialogService;
        }

        public Color GroupColor
        {
            get
            {
                return group.GroupColor.Color;
            }
            set
            {
                if (group.GroupColor.Color != value)
                {
                    group.GroupColor = new SolidColorBrush(value);
                    SaveSettings();
                }
            }
        }

        public string GroupName
        {
            get
            {
                return group.Name;
            }
            set
            {
                if (group.Name != value)
                {
                    string error = this.validator.Validate(ref value);
                    if (error != string.Empty)
                    {
                        this.dialogService.ShowDialog(Localizations.GSLocalization.Instance.InvalidValueText, error);
                        return;
                    }

                    group.Name = value;
                    SaveSettings();
                }
            }
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
            ListEditor window = new ListEditor(this.group.SettingName + "List", this.group.Name, Validator.NickNameValidator);
            window.Owner = this.dialogService.GetView();
            window.ShowDialog();
        }

        private void SaveSettings()
        {
            string setting = group.Name + "|" + string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", group.GroupColor.Color.A, group.GroupColor.Color.R, group.GroupColor.Color.G, group.GroupColor.Color.B);
            SettingsHelper.Save(group.SettingName, setting);
        }
    }
}