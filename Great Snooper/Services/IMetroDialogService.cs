namespace GreatSnooper.Services
{
    using System;
    using System.Threading.Tasks;

    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;

    public interface IMetroDialogService
    {
        void ActivationRequest();

        void CloseRequest();

        MetroWindow GetView();

        void ShowDialog(string title, string message);

        void ShowDialog(string title, string message, MessageDialogStyle style, MetroDialogSettings settings, Action<Task<MessageDialogResult>> action);
    }
}