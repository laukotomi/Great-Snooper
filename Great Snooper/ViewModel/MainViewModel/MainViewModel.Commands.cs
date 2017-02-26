namespace GreatSnooper.ViewModel
{
    using System;
    using System.Windows.Input;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;

    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;

    public partial class MainViewModel : ViewModelBase, IDisposable
    {
        public ICommand ChangeTabCommand
        {
            get
            {
                return new RelayCommand<AbstractChannelViewModel>(ChangeTab);
            }
        }

        public RelayCommand<AbstractChannelViewModel> CloseChannelCommand
        {
            get
            {
                return new RelayCommand<AbstractChannelViewModel>(CloseChannel);
            }
        }

        public RelayCommand<ChannelViewModel> HideChannelCommand
        {
            get
            {
                return new RelayCommand<ChannelViewModel>(HideChannel);
            }
        }

        public ICommand NextChannelCommand
        {
            get
            {
                return new RelayCommand(NextChannel);
            }
        }

        public ICommand PrevChannelCommand
        {
            get
            {
                return new RelayCommand(PrevChannel);
            }
        }

        public ICommand RefreshGameListCommand
        {
            get
            {
                return new RelayCommand(RefreshGameList);
            }
        }

        public void CloseChannel(AbstractChannelViewModel chvm)
        {
            if (chvm.ChannelTabVM != null)
            {
                chvm.ChannelTabVM.CloseChannelTab(chvm);
            }
        }

        private void ChangeTab(AbstractChannelViewModel chvm)
        {
            if (chvm == null)
            {
                chvm = this.SelectedChannel;
            }

            if (chvm != null)
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

        private void NextChannel()
        {
            if (this.SelectedChannel != null)
            {
                this.SelectedChannel.ChannelTabVM.SelectNextChannel();
            }
        }

        private void PrevChannel()
        {
            if (this.SelectedChannel != null)
            {
                this.SelectedChannel.ChannelTabVM.SelectPreviousChannel();
            }
        }

        private void RefreshGameList()
        {
            this.GameListForce = true;
        }
    }
}