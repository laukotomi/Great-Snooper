using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Services;
using MahApps.Metro.Controls.Dialogs;
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
        private bool changed;
        private string _inMessages;
        private string _inSenderNames;
        private string _inGameNames;
        private string _inHosterNames;
        private string _inJoinMessages;
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

        public string InMessages
        {
            get { return _inMessages; }
            set
            {
                if (_inMessages != value)
                {
                    _inMessages = value;
                    this.changed = true;
                }
            }
        }

        public string InSenderNames
        {
            get { return _inSenderNames; }
            set
            {
                if (_inSenderNames != value)
                {
                    _inSenderNames = value;
                    this.changed = true;
                }
            }
        }

        public string InGameNames
        {
            get { return _inGameNames; }
            set
            {
                if (_inGameNames != value)
                {
                    _inGameNames = value;
                    this.changed = true;
                }
            }
        }

        public string InHosterNames
        {
            get { return _inHosterNames; }
            set
            {
                if (_inHosterNames != value)
                {
                    _inHosterNames = value;
                    this.changed = true;
                }
            }
        }

        public string InJoinMessages
        {
            get { return _inJoinMessages; }
            set
            {
                if (_inJoinMessages != value)
                {
                    _inJoinMessages = value;
                    this.changed = true;
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
            this._inGameNames = Properties.Settings.Default.NotificatorInGameNames;
            this._inHosterNames = Properties.Settings.Default.NotificatorInHosterNames;
            this._inJoinMessages = Properties.Settings.Default.NotificatorInJoinMessages;
            this._inMessages = Properties.Settings.Default.NotificatorInMessages;
            this._inSenderNames = Properties.Settings.Default.NotificatorInSenderNames;
        }

        #region StartStopCommand
        public ICommand StartStopCommand
        {
            get { return new RelayCommand(StartStop); }
        }

        private void SaveChanges()
        {
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
            this.changed = false;
        }

        private void StartStop()
        {
            if (Notificator.Instance.IsEnabled == false)
            {
                Notificator.Instance.IsEnabled = true;
                if (this.changed)
                    this.SaveChanges();
                this.DialogService.CloseRequest();
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
            if (this.changed == false)
            {
                DialogService.CloseRequest();
                return;
            }

            DialogService.ShowDialog(Localizations.GSLocalization.Instance.QuestionText, Localizations.GSLocalization.Instance.SaveChangesQuestion, MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative, GlobalManager.YesNoDialogSetting, (t) =>
            {
                if (t.Result == MessageDialogResult.Affirmative)
                    this.SaveChanges();
                this.changed = false;
                DialogService.CloseRequest();
            });
        }
        #endregion

        internal void ClosingRequest(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.changed)
            {
                e.Cancel = true;
                this.CloseCommand.Execute(null);
            }
        }
    }
}
