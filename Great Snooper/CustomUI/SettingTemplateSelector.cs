namespace GreatSnooper.CustomUI
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    using GreatSnooper.Settings;

    public class SettingTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BoolSettingTemplate
        {
            get;
            set;
        }

        public DataTemplate ComboboxSettingTemplate
        {
            get;
            set;
        }

        public DataTemplate ExportImportSettingTemplate
        {
            get;
            set;
        }

        public DataTemplate SoundSettingTemplate
        {
            get;
            set;
        }

        public DataTemplate StringSettingTemplate
        {
            get;
            set;
        }

        public DataTemplate StyleSettingTemplate
        {
            get;
            set;
        }

        public DataTemplate TextListSettingTemplate
        {
            get;
            set;
        }

        public DataTemplate UserGroupSettingTemplate
        {
            get;
            set;
        }

        public DataTemplate WAExeSettingTemplate
        {
            get;
            set;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Type type = item.GetType();
            if (type == typeof(StringSetting))
            {
                return StringSettingTemplate;
            }
            else if (type == typeof(BoolSetting))
            {
                return BoolSettingTemplate;
            }
            else if (type == typeof(SoundSetting))
            {
                return SoundSettingTemplate;
            }
            else if (type == typeof(TextListSetting))
            {
                return TextListSettingTemplate;
            }
            else if (type == typeof(UserGroupSetting))
            {
                return UserGroupSettingTemplate;
            }
            else if (type == typeof(WAExeSetting))
            {
                return WAExeSettingTemplate;
            }
            else if (type == typeof(ComboboxSetting))
            {
                return ComboboxSettingTemplate;
            }
            else if (type == typeof(ExportImportSettings))
            {
                return ExportImportSettingTemplate;
            }
            return StyleSettingTemplate;
        }
    }
}