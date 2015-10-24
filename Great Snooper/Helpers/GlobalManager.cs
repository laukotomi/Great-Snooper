using GreatSnooper.Model;
using GreatSnooper.IRCTasks;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace GreatSnooper.Helpers
{
    public class GlobalManager
    {
        public const int MaxMessagesInMemory = 1000;
        public const int MaxMessagesDisplayed = 100;
        public const int NumOfOldMessagesToBeLoaded = 50;
        public const int WebRequestTimeout = 15000;
        public const int LastMessageCapacity = 10;


        // This method ensures that the initialization will be made from the appropriate thread
        public static void Initialize()
        {
            DefaultGroup = new UserGroup(UserGroups.SystemGroupID);
            SettingsPath = Directory.GetParent(Directory.GetParent(System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).FullName).FullName;
            DebugMode = false;
            SpamAllowed = false;
            SystemUser = new User(Localizations.GSLocalization.Instance.SystemUserName);

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
                StringComparer.OrdinalIgnoreCase
                );
            AutoJoinList = new HashSet<string>(
                Properties.Settings.Default.AutoJoinChannels.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);
            if (TusAccounts == null)
                TusAccounts = new Dictionary<string, TusAccount>(StringComparer.OrdinalIgnoreCase);
        }

        public static User User { get; set; }

        public static User SystemUser { get; private set; }

        public static bool DebugMode { get; set; }

        public static string SettingsPath { get; private set; }

        public static UserGroup DefaultGroup { get; private set; }

        public static bool SpamAllowed { get; set; }

        public static HashSet<string> BanList { get; private set; }

        public static HashSet<string> AutoJoinList { get; private set; }

        public static MetroDialogSettings OKDialogSetting { get; private set; }

        public static MetroDialogSettings YesNoDialogSetting { get; private set; }

        public static MetroDialogSettings MoreInfoDialogSetting { get; private set; }

        public static Dictionary<string, TusAccount> TusAccounts { get; set; }
    }
}
