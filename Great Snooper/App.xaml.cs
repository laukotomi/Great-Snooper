using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using GalaSoft.MvvmLight.Threading;
using GreatSnooper.Helpers;
using GreatSnooper.Validators;
using Microsoft.Win32;

namespace GreatSnooper
{
    public partial class App : Application
    {
        static App()
        {
            DispatcherHelper.Initialize();
        }

        public static string GetVersion()
        {
            Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (v.Build == 0)
            {
                return v.Major.ToString() + "." + v.Minor.ToString();
            }
            return v.Major.ToString() + "." + v.Minor.ToString() + "." + v.Build.ToString();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var settings = GreatSnooper.Properties.Settings.Default;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(settings.CultureName);
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

            WormNetCharTable.Initialize();
            bool save = false;

            if (!settings.SettingsUpgraded)
            {
                try
                {
                    settings.Upgrade();
                    if (settings.Group0List.Length == 0)
                        settings.Group0List = settings.BuddyList;

                    // new colors since version 1.4.9
                    if (settings.ChannelMessageStyle == "F0FFFF|13|0|0|0|0|Tahoma")
                        settings.ChannelMessageStyle = SettingsHelper.GetDefaultValue<string>("ChannelMessageStyle");
                    if (settings.JoinMessageStyle == "808000|12|0|0|0|0|Tahoma")
                        settings.JoinMessageStyle = SettingsHelper.GetDefaultValue<string>("JoinMessageStyle");
                    if (settings.PartMessageStyle == "808000|12|0|0|0|0|Tahoma")
                        settings.PartMessageStyle = SettingsHelper.GetDefaultValue<string>("PartMessageStyle");
                    if (settings.QuitMessageStyle == "808000|12|0|0|0|0|Tahoma")
                        settings.QuitMessageStyle = SettingsHelper.GetDefaultValue<string>("QuitMessageStyle");
                    if (settings.SystemMessageStyle == "FF0000|13|0|0|0|0|Tahoma")
                        settings.SystemMessageStyle = SettingsHelper.GetDefaultValue<string>("OfflineMessageStyle");
                    if (settings.ActionMessageStyle == "FFFF00|13|0|0|0|0|Tahoma")
                        settings.ActionMessageStyle = SettingsHelper.GetDefaultValue<string>("ActionMessageStyle");
                    if (settings.UserMessageStyle == "E9967A|13|0|0|0|0|Tahoma")
                        settings.UserMessageStyle = SettingsHelper.GetDefaultValue<string>("UserMessageStyle");
                    if (settings.NoticeMessageStyle == "E9967A|13|0|0|0|0|Tahoma")
                        settings.NoticeMessageStyle = SettingsHelper.GetDefaultValue<string>("NoticeMessageStyle");
                }
                catch (Exception) { }

                if (settings.BatLogo == false)
                {
                    settings.BatLogo = settings.UserName.IndexOf("guuria", StringComparison.OrdinalIgnoreCase) != -1
                        || settings.UserName.IndexOf("guuuria", StringComparison.OrdinalIgnoreCase) != -1;
                }
                settings.SettingsUpgraded = true;
                save = true;
            }

            if (settings.WaExe.Length == 0 || !File.Exists(settings.WaExe))
            {
                object WALoc = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Team17SoftwareLTD\WormsArmageddon", "PATH", null);
                if (WALoc != null)
                {
                    string WAPath = WALoc.ToString() + @"\WA.exe";
                    if (File.Exists(WAPath))
                    {
                        settings.WaExe = WAPath;
                        save = true;
                    }
                }
            }

            // Check quit message
            string quitMessage = settings.QuitMessagee;
            if (settings.QuitMessagee == string.Empty || Validator.GSVersionValidator.Validate(ref quitMessage) != string.Empty)
            {
                settings.QuitMessagee = "Great Snooper v" + App.GetVersion();
                save = true;
            }

            MessageSettings.Initialize();
            Countries.Initialize();
            Ranks.Initialize();
            GlobalManager.Initialize();
            UserGroups.Initialize();
            Sounds.Initialize();

            if (save)
                settings.Save();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorLog.Log(e.Exception);

            foreach (var server in GreatSnooper.ViewModel.MainViewModel.Instance.Servers)
            {
                foreach (var item in server.Channels)
                {
                    if (item.Value.Joined)
                        item.Value.EndLogging();
                }
            }
        }
    }
}
