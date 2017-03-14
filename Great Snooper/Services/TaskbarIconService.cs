namespace GreatSnooper.Services
{
    using System;

    using GreatSnooper.UserControls;
    using GreatSnooper.ViewModel;

    using Hardcodet.Wpf.TaskbarNotification;

    public class TaskbarIconService : ITaskbarIconService, IDisposable
    {
        bool disposed = false;

        public TaskbarIconService(TaskbarIcon icon)
        {
            this.Icon = icon;
        }

        ~TaskbarIconService()
        {
            this.Dispose(false);
        }

        public TaskbarIcon Icon
        {
            get;
            private set;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void ShowMessage(string message, AbstractChannelViewModel chvm = null)
        {
            this.Icon.ShowCustomBalloon(
                new GSBalloon(chvm)
                {
                    BalloonText = message
                },
                System.Windows.Controls.Primitives.PopupAnimation.None,
                8000);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            if (disposing)
            {
                if (this.Icon != null)
                {
                    this.Icon.Dispose();
                    this.Icon = null;
                }
            }
        }
    }
}