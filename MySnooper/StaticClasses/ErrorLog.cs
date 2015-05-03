using System;
using System.IO;
using System.Threading;

namespace MySnooper
{
    public static class ErrorLog
    {
        public class ExceptionLogger
        {
            private string filename;
            private Exception ex;

            public ExceptionLogger(Exception ex, string filename)
            {
                this.ex = ex;
                this.filename = filename;
            }

            public void DoLog()
            {
                try
                {
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
                ErrorLog.Logging = false;
            }
        }

        public static volatile bool Logging = false;
        private static string filename = string.Empty;

        public static void Log(Exception ex)
        {
            if (filename == string.Empty)
            {
                string settingsPath = Directory.GetParent(Directory.GetParent(System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).FullName).FullName;
                filename = settingsPath + @"\errorlog.txt";
            }

            while (true)
            {
                if (!Logging)
                {
                    Logging = true;
                    ExceptionLogger el = new ExceptionLogger(ex, filename);
                    Thread t = new Thread(el.DoLog);
                    t.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
                    t.Start();
                    break;
                }
                Thread.Sleep(10);
            }
        }
    }
}
