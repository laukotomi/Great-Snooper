using GreatSnooper.Helpers;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Threading.Tasks;

namespace GreatSnooper.Services
{
    class MetroDialogService : IMetroDialogService
    {
        private MetroWindow window;

        public MetroDialogService(MetroWindow window)
        {
            this.window = window;
        }

        public void ShowDialog(string title, string message)
        {
            window.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting);
        }

        public void ShowDialog(string title, string message, MessageDialogStyle style, MetroDialogSettings settings, Action<Task<MessageDialogResult>> action)
        {
            window.ShowMessageAsync(title, message, style, settings)
                .ContinueWith(action, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void ActivationRequest()
        {
            this.window.Activate();
        }

        public void CloseRequest()
        {
            this.window.Close();
        }

        public MetroWindow GetView()
        {
            return this.window;
        }
    }
}
