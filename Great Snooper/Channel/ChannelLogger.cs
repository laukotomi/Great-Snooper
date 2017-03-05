using System;
using System.IO;
using GreatSnooper.Helpers;
using GreatSnooper.Model;

namespace GreatSnooper.Channel
{
    public class ChannelLogger : IDisposable
    {
        private StreamWriter _logger;
        private int _loggerDay;

        public void LogMessage(Message message, string channelName)
        {
            if (message.IsLogged)
            {
                return;
            }

            try
            {
                DateTime now = DateTime.Now;
                if (now.Day != this._loggerDay)
                {
                    this.EndLogging();

                    string dirPath = GlobalManager.SettingsPath + @"\Logs\" + channelName;
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }

                    string logFile = dirPath + "\\" + now.ToString("yyyy-MM-dd") + ".log";
                    this._logger = new StreamWriter(logFile, true);
                    this._loggerDay = now.Day;
                }

                this._logger.WriteLine("(" + message.Style.Type.ToString() + ") " + message.Time.ToString("yyyy-MM-dd HH:mm:ss") + " " + message.Sender.Name + ": " + message.Text);
                this._logger.Flush();
                message.IsLogged = true;
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }

        public void EndLogging()
        {
            if (this._logger != null)
            {
                try
                {
                    this._logger.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Channel closed.");
                    this._logger.WriteLine("-----------------------------------------------------------------------------------------");
                    this._logger.WriteLine(Environment.NewLine + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }

                this._logger.Dispose();
                this._logger = null;
            }

            this._loggerDay = 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._logger != null)
                {
                    this._logger.Dispose();
                    this._logger = null;
                }
            }
        }
    }
}
