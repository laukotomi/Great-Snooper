using GreatSnooper.UserControls;
using Hardcodet.Wpf.TaskbarNotification;
using System;

namespace GreatSnooper.Services
{
    public class TaskbarIconService : ITaskbarIconService, IDisposable
    {
        private TaskbarIcon icon;

        public TaskbarIconService(TaskbarIcon icon)
        {
            this.icon = icon;
        }

        public void ShowMessage(string message)
        {
            this.icon.ShowCustomBalloon(new GSBalloon() { BalloonText = message }, System.Windows.Controls.Primitives.PopupAnimation.None, 5000);
        }

        #region IDisposable
        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;

            if (disposing)
            {
                if (icon != null)
                {
                    icon.Dispose();
                    icon = null;
                }
            }
        }

        ~TaskbarIconService()
        {
            Dispose(false);
        }

        #endregion
    }
}
