using System;
using System.IO;
using System.Threading;

namespace GreatSnooper.Helpers
{
    public static class ErrorLog
    {
        private static object locker = new object();

        public static void Log(Exception ex)
        {
            lock (locker)
            {
                try
                {
                    string filename = GlobalManager.SettingsPath + @"\errorlog.txt";
                    // Delete log file if it is more than 10 Mb
                    FileInfo logfile = new FileInfo(filename);
                    if (logfile.Exists && logfile.Length > 10 * 1024 * 1024)
                        logfile.Delete();

                    using (StreamWriter w = new StreamWriter(filename, true))
                    {
                        w.WriteLine(DateTime.Now.ToString("U"));
                        w.WriteLine(ex.GetType().FullName);
                        w.WriteLine(ex.Message);
                        w.WriteLine(ex.StackTrace);
                        w.WriteLine(Environment.NewLine + Environment.NewLine + Environment.NewLine);
                    }
                }
                catch (Exception) { }
            }
        }
    }
}
