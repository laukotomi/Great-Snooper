using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Helpers;
using GreatSnooper.Services;
using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace GreatSnooper.ViewModel
{
    class AwayViewModel : ViewModelBase
    {
        #region Members
        private bool _isAway;
        private MainViewModel mvm;
        private Dispatcher dispatcher;
        #endregion

        #region Properties
        public IMetroDialogService DialogService { get; set; }
        public string AwayText { get; set; }
        public string AwayButtonText
        {
            get { return (_isAway) ? Localizations.GSLocalization.Instance.AwayButtonBack : Localizations.GSLocalization.Instance.AwayButtonAway; }
        }
        public bool IsAway
        {
            get { return _isAway; }
            private set
            {
                if (_isAway != value)
                {
                    _isAway = value;
                    RaisePropertyChanged("AwayText");
                    RaisePropertyChanged("IsAway");
                }
            }
        }
        #endregion

        public AwayViewModel(MainViewModel mvm, string awayText)
        {
            this.mvm = mvm;
            this.dispatcher = Dispatcher.CurrentDispatcher;

            _isAway = awayText != string.Empty;
            if (_isAway)
                AwayText = awayText;
            else
                AwayText = Properties.Settings.Default.AwayMessage;
        }

        #region AwayCommand
        public ICommand AwayCommand
        {
            get { return new RelayCommand(SetAway); }
        }

        private void SetAway()
        {
            if (IsAway)
            {
                this.dispatcher.BeginInvoke(new Action(() =>
                {
                    this.mvm.SetBack();
                }));
                this.Close();
            }
            else
            {
                string text = WormNetCharTable.RemoveNonWormNetChars(AwayText.Trim());
                if (text.Length > 0)
                {
                    Properties.Settings.Default.AwayMessage = text;
                    Properties.Settings.Default.Save();
                    this.dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.mvm.SetAway(text);
                    }));
                    this.Close();
                }
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
