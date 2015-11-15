
using Hardcodet.Wpf.TaskbarNotification;
namespace GreatSnooper.Services
{
    public interface ITaskbarIconService
    {
        TaskbarIcon Icon { get; }
        void ShowMessage(string message);
        void Dispose();
    }
}
