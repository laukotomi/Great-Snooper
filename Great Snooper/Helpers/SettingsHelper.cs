namespace GreatSnooper.Helpers
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public static class SettingsHelper
    {
        public static bool Exists(string settingName)
        {
            return Properties.Settings.Default.GetType().GetProperty(settingName) != null;
        }

        public static T GetDefaultValue<T>(string settingName)
        {
            return (T)Properties.Settings.Default.Properties[settingName].DefaultValue;
        }

        public static object Load(string settingName)
        {
            return Properties.Settings.Default.GetType().GetProperty(settingName).GetValue(Properties.Settings.Default, null);
        }

        public static T Load<T>(string settingName)
        {
            return (T)(Properties.Settings.Default.GetType().GetProperty(settingName).GetValue(Properties.Settings.Default, null));
        }

        public static void Save(string settingName, object value, bool save = true)
        {
            Properties.Settings.Default.GetType().GetProperty(settingName).SetValue(Properties.Settings.Default, value, null);
            if (save)
            {
                Properties.Settings.Default.Save();
            }
        }

        public static void Save(string settingName, IEnumerable<string> collection, bool save = true)
        {
            Properties.Settings.Default.GetType().GetProperty(settingName).SetValue(Properties.Settings.Default, string.Join(",", collection), null);
            if (save)
            {
                Properties.Settings.Default.Save();
            }
        }

        public static void Save(string settingName, Dictionary<string, string> collection, bool save = true)
        {
            Properties.Settings.Default.GetType().GetProperty(settingName).SetValue(Properties.Settings.Default, JsonConvert.SerializeObject(collection), null);
            if (save)
            {
                Properties.Settings.Default.Save();
            }
        }
    }
}