namespace GreatSnooper.Services
{
    using GreatSnooper.ViewModel;

    using Hardcodet.Wpf.TaskbarNotification;

    public interface ITaskbarIconService
    {
        TaskbarIcon Icon
        {
            get;
        }

        void Dispose();

        void ShowMessage(string message, AbstractChannelViewModel chvm = null);
    }
}