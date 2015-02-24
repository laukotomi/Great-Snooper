using System;
using System.Windows;
using System.Windows.Threading;


namespace MySnooper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorLog.Log(e.Exception);
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
    }
}
