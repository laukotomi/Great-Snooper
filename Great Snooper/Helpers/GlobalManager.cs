using GreatSnooper.Model;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace GreatSnooper.Helpers
{
    public class GlobalManager
    {
        public const int MaxMessagesInMemory = 1000;
        public const int MaxMessagesDisplayed = 100;
        public const int NumOfOldMessagesToBeLoaded = 50;
        public const int WebRequestTimeout = 60000;
        public const int LastMessageCapacity = 10;


        // This method ensures that the initialization will be made from the appropriate thread
        public static void Initialize()
        {
            DefaultGroup = new UserGroup(UserGroups.SystemGroupID);
            SettingsPath = Directory.GetParent(Directory.GetParent(System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).FullName).FullName;
            DebugMode = false;
            SpamAllowed = false;
            SystemUser = new User(Localizations.GSLocalization.Instance.SystemUserName);
            CIStringComparer = StringComparer.Create(new CultureInfo("en-US"), true);

            OKDialogSetting = new MetroDialogSettings()
            {
                AffirmativeButtonText = Localizations.GSLocalization.Instance.OKText,
                AnimateHide = false,
                AnimateShow = false,
                ColorScheme = MetroDialogColorScheme.Accented
            };

            YesNoDialogSetting = new MetroDialogSettings()
            {
                AffirmativeButtonText = Localizations.GSLocalization.Instance.YesText,
                NegativeButtonText = Localizations.GSLocalization.Instance.NoText,
                AnimateHide = false,
                AnimateShow = false,
                ColorScheme = MetroDialogColorScheme.Accented
            };

            MoreInfoDialogSetting = new MetroDialogSettings()
            {
                AffirmativeButtonText = Localizations.GSLocalization.Instance.MoreInfoText,
                NegativeButtonText = Localizations.GSLocalization.Instance.OKText,
                AnimateHide = false,
                AnimateShow = false,
                ColorScheme = MetroDialogColorScheme.Accented
            };
        }

        public static void MainWindowInit()
        {
            BanList = new HashSet<string>(
                Properties.Settings.Default.BanList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                CIStringComparer
                );
            // Backwards compatibility
            if (Properties.Settings.Default.AutoJoinChannels.Contains(":") == false)
            {
                AutoJoinList = new Dictionary<string, string>();
                string[] parts = Properties.Settings.Default.AutoJoinChannels.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                    AutoJoinList.Add(part, null);
            }
            else
                AutoJoinList = JsonConvert.DeserializeObject<Dictionary<string, string>>(Properties.Settings.Default.AutoJoinChannels);

            HiddenChannels = new HashSet<string>(
                Properties.Settings.Default.HiddenChannels.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                CIStringComparer);

            if (TusAccounts == null)
                TusAccounts = new Dictionary<string, TusAccount>(CIStringComparer);
        }

        public static User User { get; set; }

        public static User SystemUser { get; private set; }

        public static bool DebugMode { get; set; }

        public static string SettingsPath { get; private set; }

        public static UserGroup DefaultGroup { get; private set; }

        public static bool SpamAllowed { get; set; }

        public static HashSet<string> BanList { get; private set; }

        public static Dictionary<string, string> AutoJoinList { get; private set; }

        public static HashSet<string> HiddenChannels { get; set; }

        public static MetroDialogSettings OKDialogSetting { get; private set; }

        public static MetroDialogSettings YesNoDialogSetting { get; private set; }

        public static MetroDialogSettings MoreInfoDialogSetting { get; private set; }

        public static Dictionary<string, TusAccount> TusAccounts { get; set; }

        public static StringComparer CIStringComparer { get; private set; }
    }
}
