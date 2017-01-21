using System;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Classes;
using GreatSnooper.Helpers;

namespace GreatSnooper.ViewModel
{
    public partial class MainViewModel : ViewModelBase, IDisposable
    {
        public RelayCommand<AbstractChannelViewModel> CloseChannelCommand
        {
            get { return new RelayCommand<AbstractChannelViewModel>(CloseChannel); }
        }

        public void CloseChannel(AbstractChannelViewModel chvm)
        {
            if (chvm.ChannelTabVM != null)
                chvm.ChannelTabVM.CloseChannelTab(chvm);
        }

        public RelayCommand<ChannelViewModel> HideChannelCommand
        {
            get { return new RelayCommand<ChannelViewModel>(HideChannel); }
        }

        private void HideChannel(ChannelViewModel chvm)
        {
            this.CloseChannel(chvm);
            if (chvm.Server is WormNetCommunicator)
            {
                GlobalManager.HiddenChannels.Add(chvm.Name);
                SettingsHelper.Save("HiddenChannels", GlobalManager.HiddenChannels);
            }
            if (GlobalManager.AutoJoinList.ContainsKey(chvm.Name))
            {
                GlobalManager.AutoJoinList.Remove(chvm.Name);
                SettingsHelper.Save("AutoJoinChannels", GlobalManager.AutoJoinList);
            }
        }

        public ICommand RefreshGameListCommand
        {
            get { return new RelayCommand(RefreshGameList); }
        }

        private void RefreshGameList()
        {
            this.GameListForce = true;
        }

        public ICommand NextChannelCommand
        {
            get { return new RelayCommand(NextChannel); }
        }

        private void NextChannel()
        {
            if (this.SelectedChannel != null)
                this.SelectedChannel.ChannelTabVM.SelectNextChannel();
        }

        public ICommand PrevChannelCommand
        {
            get { return new RelayCommand(PrevChannel); }
        }

        private void PrevChannel()
        {
            if (this.SelectedChannel != null)
                this.SelectedChannel.ChannelTabVM.SelectPreviousChannel();
        }

        public ICommand ChangeTabCommand
        {
            get { return new RelayCommand<AbstractChannelViewModel>(ChangeTab); }
        }

        private void ChangeTab(AbstractChannelViewModel chvm)
        {
            if (chvm.ChannelTabVM == this._channelTabControl1)
            {
                this._channelTabControl1.Channels.Remove(chvm);
                this._channelTabControl2.Channels.Add(chvm);
            }
            else
            {
                this._channelTabControl2.Channels.Remove(chvm);
                this._channelTabControl1.Channels.Add(chvm);
            }
        }
    }
}
