using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Threading.Tasks;

namespace GreatSnooper.Services
{
    public interface IMetroDialogService
    {
        void ShowDialog(string title, string message);
        void ShowDialog(string title, string message, MessageDialogStyle style, MetroDialogSettings settings, Action<Task<MessageDialogResult>> action);
        void ActivationRequest();
        void CloseRequest();
        MetroWindow GetView();
    }
}
