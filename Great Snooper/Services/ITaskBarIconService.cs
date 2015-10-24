
namespace GreatSnooper.Services
{
    public interface ITaskbarIconService
    {
        void ShowMessage(string message);
        void Dispose();
    }
}
