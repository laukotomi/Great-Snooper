namespace GreatSnooper
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Windows;
    using GalaSoft.MvvmLight.Threading;
    using GreatSnooper.Helpers;
    using GreatSnooper.Startup;

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

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorLog.Log(e.Exception);

            foreach (var server in GreatSnooper.ViewModel.MainViewModel.Instance.Servers)
            {
                foreach (var item in server.Channels)
                {
                    if (item.Value.Joined)
                    {
                        item.Value.Dispose();
                    }
                }
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(GreatSnooper.Properties.Settings.Default.CultureName);
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

            DI.Init();
            SettingsUpgrader.UpgradeSettings();
            MessageSettings.Initialize();
            Countries.Initialize();
            Ranks.Initialize();
            GlobalManager.Initialize();
            UserGroups.Initialize();
            Sounds.Initialize();
        }
    }
}