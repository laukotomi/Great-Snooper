using System;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Helpers;
using GreatSnooper.Windows;

namespace GreatSnooper.ViewModel
{
    public partial class MainViewModel : ViewModelBase, IDisposable
    {
        public ICommand WindowActivatedCommand
        {
            get { return new RelayCommand(WindowActivated); }
        }

        private void WindowActivated()
        {
            this.IsWindowFlashing = false;
            if (this.SelectedChannel != null)
                this.SelectedChannel.ChannelTabVM.ActivateSelectedChannel();
        }

        public ICommand WindowStateChangedCommand
        {
            get { return new RelayCommand(WindowStateChanged); }
        }

        private void WindowStateChanged()
        {
            var window = this.DialogService.GetView();
            if (window.WindowState == WindowState.Minimized)
            {
                if (Properties.Settings.Default.EnergySaveModeWin && !IsEnergySaveMode)
                    EnterEnergySaveMode();
            }
            else
            {
                if (IsEnergySaveMode)
                    this.shouldLeaveEnergySaveMode = true; // Somehow leaving energysave mode doesn't work

                // save window state
                Properties.Settings.Default.WindowState = (int)window.WindowState;
            }
        }

        public ICommand ColumnsWidthChangedCommand
        {
            get { return new RelayCommand(ColumnsWidthChanged); }
        }

        private void ColumnsWidthChanged()
        {
            this.LeftColumnWidth = ((MainWindow)this.DialogService.GetView()).LeftColumn.Width;
            this.RightColumnWidth = ((MainWindow)this.DialogService.GetView()).RightColumn.Width;
        }

        public ICommand RowsHeightChangedCommand
        {
            get { return new RelayCommand(RowsHeightChanged); }
        }

        private void RowsHeightChanged()
        {
            this.TopRowHeight = ((MainWindow)this.DialogService.GetView()).TopRow.Height;
            this.BottomRowHeight = ((MainWindow)this.DialogService.GetView()).BottomRow.Height;
        }

        private void HideWindow()
        {
            this.DialogService.GetView().Hide();
            this.isHidden = true;

            if (Properties.Settings.Default.EnergySaveModeWin && !IsEnergySaveMode)
                EnterEnergySaveMode();
        }

        internal void FlashWindow()
        {
            if (Properties.Settings.Default.TrayFlashing && this.IsWindowActive == false && this.IsWindowFlashing == false)
                this.IsWindowFlashing = true;
        }

        public bool IsGameWindowOn()
        {
            var lobby = NativeMethods.FindWindow(null, "Worms Armageddon");
            if (lobby != IntPtr.Zero)
                return NativeMethods.GetPlacement(lobby).showCmd == ShowWindowCommands.Normal;

            return false;
        }

    }
}
