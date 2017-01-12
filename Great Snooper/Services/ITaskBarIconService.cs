
using GreatSnooper.ViewModel;
using Hardcodet.Wpf.TaskbarNotification;
namespace GreatSnooper.Services
{
    public interface ITaskbarIconService
    {
        TaskbarIcon Icon { get; }
        void ShowMessage(string message, AbstractChannelViewModel chvm = null);
        void Dispose();
    }
}
