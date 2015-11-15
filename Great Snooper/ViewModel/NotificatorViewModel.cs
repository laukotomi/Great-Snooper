using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Classes;
using GreatSnooper.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace GreatSnooper.ViewModel
{
    class NotificatorViewModel : ViewModelBase
    {
        #region Members
        #endregion

        #region Properties
        public IMetroDialogService DialogService { get; set; }

        public bool IsSearching
        {
            get { return Notificator.Instance.IsEnabled; }
        }

        public bool? AutoStart
        {
            get { return Properties.Settings.Default.NotificatorStartWithSnooper; }
            set
            {
                if (value.HasValue && Properties.Settings.Default.NotificatorStartWithSnooper != value.Value)
                {
                    Properties.Settings.Default.NotificatorStartWithSnooper = value.Value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public string InMessages { get; set; }

        public string InSenderNames { get; set; }

        public string InGameNames { get; set; }

        public string InHosterNames { get; set; }

        public string InJoinMessages { get; set; }

        public bool? StartNotifWithSnooper
        {
            get { return Properties.Settings.Default.NotificatorStartWithSnooper; }
            set
            {
                if (value.HasValue && value.Value != Properties.Settings.Default.NotificatorStartWithSnooper)
                {
                    Properties.Settings.Default.NotificatorStartWithSnooper = value.Value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public string StartStopButtonText
        {
            get
            {
                return (Notificator.Instance.IsEnabled)
                    ? Localizations.GSLocalization.Instance.StopSearchingText
                    : Localizations.GSLocalization.Instance.StartSearchingText;
            }
        }
        #endregion

        public NotificatorViewModel()
        {
            this.InGameNames = Properties.Settings.Default.NotificatorInGameNames;
            this.InHosterNames = Properties.Settings.Default.NotificatorInHosterNames;
            this.InJoinMessages = Properties.Settings.Default.NotificatorInJoinMessages;
            this.InMessages = Properties.Settings.Default.NotificatorInMessages;
            this.InSenderNames = Properties.Settings.Default.NotificatorInSenderNames;
        }

        #region StartStopCommand
        public ICommand StartStopCommand
        {
            get { return new RelayCommand(StartStop); }
        }

        private void StartStop()
        {
            if (Notificator.Instance.IsEnabled == false)
            {
                Notificator.Instance.IsEnabled = true;
                if (Properties.Settings.Default.NotificatorInGameNames != this.InGameNames)
                    Properties.Settings.Default.NotificatorInGameNames = this.InGameNames;
                if (Properties.Settings.Default.NotificatorInHosterNames != this.InHosterNames)
                    Properties.Settings.Default.NotificatorInHosterNames = this.InHosterNames;
                if (Properties.Settings.Default.NotificatorInJoinMessages != this.InJoinMessages)
                    Properties.Settings.Default.NotificatorInJoinMessages = this.InJoinMessages;
                if (Properties.Settings.Default.NotificatorInMessages != this.InMessages)
                    Properties.Settings.Default.NotificatorInMessages = this.InMessages;
                if (Properties.Settings.Default.NotificatorInSenderNames != this.InSenderNames)
                    Properties.Settings.Default.NotificatorInSenderNames = this.InSenderNames;
                Properties.Settings.Default.Save();
                CloseCommand.Execute(null);
            }
            else
            {
                Notificator.Instance.IsEnabled = false;
                RaisePropertyChanged("StartStopButtonText");
                RaisePropertyChanged("IsSearching");
            }
        }
        #endregion

        #region CloseCommand
        public ICommand CloseCommand
        {
            get { return new RelayCommand(Close); }
        }

        private void Close()
        {
            this.DialogService.CloseRequest();
        }
        #endregion
    }
}
