using System;
using System.IO;
using System.Threading;

namespace MySnooper
{
    public class ExceptionLogger
    {
        private byte mode;
        private string filename;
        private string errorstr;
        private Exception ex;

        public ExceptionLogger(Exception ex, string filename)
        {
            this.mode = 1;
            this.ex = ex;
            this.filename = filename;
        }

        public ExceptionLogger(string error, string filename)
        {
            this.mode = 2;
            this.errorstr = error;
            this.filename = filename;
        }

        public void DoLog()
        {
            try
            {
                if (mode == 1)
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
                else if (mode == 2)
                {
                    using (StreamWriter w = new StreamWriter(filename, true))
                    {
                        w.WriteLine(DateTime.Now.ToString("U"));
                        w.WriteLine(errorstr);
                        w.WriteLine(Environment.NewLine + Environment.NewLine + Environment.NewLine);
                    }
                }
            }
            catch (Exception) { }
            lock (ErrorLog.Locker)
            {
                ErrorLog.Logging = false;
            }
        }
    }

    public static class ErrorLog
    {
        public static bool Logging = false;
        public static object Locker = new object();
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
                lock (Locker)
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
                }
                Thread.Sleep(10);
            }
        }
    }
}
