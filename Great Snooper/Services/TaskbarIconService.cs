using GreatSnooper.UserControls;
using Hardcodet.Wpf.TaskbarNotification;
using System;

namespace GreatSnooper.Services
{
    public class TaskbarIconService : ITaskbarIconService, IDisposable
    {
        public TaskbarIcon Icon { get; private set; }

        public TaskbarIconService(TaskbarIcon icon)
        {
            this.Icon = icon;
        }

        public void ShowMessage(string message)
        {
            this.Icon.ShowCustomBalloon(new GSBalloon() { BalloonText = message }, System.Windows.Controls.Primitives.PopupAnimation.None, 5000);
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
                if (this.Icon != null)
                {
                    this.Icon.Dispose();
                    this.Icon = null;
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
