namespace GreatSnooper.Services
{
    using System;
    using System.Threading.Tasks;

    using GreatSnooper.Helpers;

    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;

    class MetroDialogService : IMetroDialogService
    {
        private MetroWindow window;

        public MetroDialogService(MetroWindow window)
        {
            this.window = window;
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

        public void ShowDialog(string title, string message)
        {
            this.window.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting);
        }

        public void ShowDialog(string title, string message, MessageDialogStyle style, MetroDialogSettings settings, Action<Task<MessageDialogResult>> action)
        {
            this.window.ShowMessageAsync(title, message, style, settings)
            .ContinueWith(action, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}