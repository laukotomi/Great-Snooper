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

        public string InMessages
        {
            get { return Properties.Settings.Default.NotificatorInMessages; }
            set
            {
                if (Properties.Settings.Default.NotificatorInMessages != value)
                {
                    Properties.Settings.Default.NotificatorInMessages = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public string InSenderNames
        {
            get { return Properties.Settings.Default.NotificatorInSenderNames; }
            set
            {
                if (Properties.Settings.Default.NotificatorInSenderNames != value)
                {
                    Properties.Settings.Default.NotificatorInSenderNames = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public string InGameNames
        {
            get { return Properties.Settings.Default.NotificatorInGameNames; }
            set
            {
                if (Properties.Settings.Default.NotificatorInGameNames != value)
                {
                    Properties.Settings.Default.NotificatorInGameNames = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public string InHosterNames
        {
            get { return Properties.Settings.Default.NotificatorInHosterNames; }
            set
            {
                if (Properties.Settings.Default.NotificatorInHosterNames != value)
                {
                    Properties.Settings.Default.NotificatorInHosterNames = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public string InJoinMessages
        {
            get { return Properties.Settings.Default.NotificatorInJoinMessages; }
            set
            {
                if (Properties.Settings.Default.NotificatorInJoinMessages != value)
                {
                    Properties.Settings.Default.NotificatorInJoinMessages = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

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
        #endregion

        public NotificatorViewModel()
        {

        }

        #region StartStopCommand
        public ICommand StartStopCommand
        {
            get { return new RelayCommand(StartStop); }
        }

        private void StartStop()
        {
            Notificator.Instance.IsEnabled = !Notificator.Instance.IsEnabled;
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
