
using Newtonsoft.Json;
using System.Collections.Generic;
namespace GreatSnooper.Helpers
{
    public static class SettingsHelper
    {
        public static void Save(string settingName, object value)
        {
            Properties.Settings.Default.GetType().GetProperty(settingName).SetValue(Properties.Settings.Default, value, null);
            Properties.Settings.Default.Save();
        }

        public static void Save(string settingName, IEnumerable<string> collection)
        {
            if (collection.GetType() == typeof(Dictionary<string, string>))
                Properties.Settings.Default.GetType().GetProperty(settingName).SetValue(Properties.Settings.Default, JsonConvert.SerializeObject(collection), null);
            else
                Properties.Settings.Default.GetType().GetProperty(settingName).SetValue(Properties.Settings.Default, string.Join(",", collection), null);
            Properties.Settings.Default.Save();
        }

        public static bool Exists(string settingName)
        {
            return Properties.Settings.Default.GetType().GetProperty(settingName) != null;
        }

        public static object Load(string settingName)
        {
            return Properties.Settings.Default.GetType().GetProperty(settingName).GetValue(Properties.Settings.Default, null);
        }

        public static T Load<T>(string settingName)
        {
            return (T)(Properties.Settings.Default.GetType().GetProperty(settingName).GetValue(Properties.Settings.Default, null));
        }

        public static T GetDefaultValue<T>(string settingName)
        {
            return (T)Properties.Settings.Default.Properties[settingName].DefaultValue;
        }
    }
}
