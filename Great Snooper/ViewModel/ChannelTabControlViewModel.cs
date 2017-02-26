namespace GreatSnooper.ViewModel
{
    using System;
    using System.Linq;
    using System.Windows.Controls;

    using GalaSoft.MvvmLight;

    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;

    public class ChannelTabControlViewModel : ViewModelBase
    {
        private readonly SortedObservableCollection<AbstractChannelViewModel> _channels =
            new SortedObservableCollection<AbstractChannelViewModel>();
        private readonly TabControl _view;
        private readonly VisitedChannels _visitedChannels = new VisitedChannels();

        private AbstractChannelViewModel _selectedChannel;
        private int _selectedChannelIndex = -1;

        public ChannelTabControlViewModel(TabControl view)
        {
            this._view = view;

            this.Channels.CollectionChanged += Channels_CollectionChanged;
            Properties.Settings.Default.PropertyChanged += SettingsChanged;
        }

        public SortedObservableCollection<AbstractChannelViewModel> Channels
        {
            get
            {
                return this._channels;
            }
        }

        public AbstractChannelViewModel SelectedChannel
        {
            get
            {
                return this._selectedChannel;
            }
            set
            {
                int index = this.Channels.IndexOf(value);
                if (index != -1)
                {
                    this.SelectedChannelIndex = index;
                }
            }
        }

        public int SelectedChannelIndex
        {
            get
            {
                return this._selectedChannelIndex;
            }
            set
            {
                if (value != this._selectedChannelIndex)
                {
                    PMChannelViewModel oldPMChannel = this._selectedChannel as PMChannelViewModel;

                    if (value == -1)
                    {
                        this._selectedChannelIndex = value;
                        this._selectedChannel = null;
                    }
                    else if (value >= 0 && value < this.Channels.Count)
                    {
                        this._selectedChannelIndex = value;
                        this._visitedChannels.Visit(value);
                        this._selectedChannel = Channels[value];
                        this.ActivateSelectedChannel();
                    }

                    if (oldPMChannel != null)
                    {
                        oldPMChannel.GenerateHeader();
                    }

                    this.RaisePropertyChanged("SelectedChannelIndex");
                }
            }
        }

        public TabControl View
        {
            get
            {
                return this._view;
            }
        }

        public void ActivateSelectedChannel()
        {
            if (this.SelectedChannel.IsHighlighted)
            {
                this.SelectedChannel.IsHighlighted = false;
                if (this.SelectedChannel is PMChannelViewModel)
                {
                    ((PMChannelViewModel)this.SelectedChannel).GenerateHeader();
                }
            }
            if (this._selectedChannel.Joined)
            {
                this._view.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this._view.UpdateLayout();
                    if (!this._selectedChannel.Disabled)
                        this._selectedChannel.IsTBFocused = true;
                }));
            }
        }

        public void CloseChannelTab(AbstractChannelViewModel chvm)
        {
            int index = (this.SelectedChannel == chvm)
                        ? this.SelectedChannelIndex
                        : this.Channels.IndexOf(chvm);

            if (this.SelectedChannel == chvm) // Channel was selected
            {
                int lastindex = this._visitedChannels.GetBeforeLastIndex();
                if (lastindex != -1)
                {
                    this.SelectedChannelIndex = lastindex;
                }
            }

            ChannelViewModel channel = chvm as ChannelViewModel;
            if (channel != null)
            {
                if (channel.Joined)
                {
                    channel.LeaveChannelCommand.Execute(null);
                }
            }
            else
            {
                chvm.EndLogging();
                chvm.ClearUsers();
            }

            chvm.Server.Channels.Remove(chvm.Name);
            if (chvm.Server is GameSurgeCommunicator && chvm.Server.Channels.Any(x => x.Value.Joined) == false)
            {
                chvm.Server.CancelAsync();
            }

            this.Channels.Remove(chvm);

            // Refresh selected channel, because selected item will be lost
            int lastIndex = this._visitedChannels.GetLastIndex();
            if (lastIndex != -1)
            {
                this.SelectedChannelIndex = lastIndex;
            }
        }

        public void SelectNextChannel()
        {
            if (this._channels.Count > 0)
            {
                if (this._selectedChannelIndex + 1 < this._channels.Count)
                {
                    this.SelectedChannelIndex++;
                }
                else
                {
                    this.SelectedChannelIndex = 0;
                }
            }
        }

        public void SelectPreviousChannel()
        {
            if (this._channels.Count > 0)
            {
                if (this._selectedChannelIndex > 0)
                {
                    this.SelectedChannelIndex--;
                }
                else
                {
                    this.SelectedChannelIndex = this._channels.Count - 1;
                }
            }
        }

        private void Channels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var chvm = (AbstractChannelViewModel)e.NewItems[0];
                chvm.ChannelTabVM = this;
                this._view.Items.Insert(e.NewStartingIndex, chvm.GetLayout());

                if (this.Channels.Count == 1)
                {
                    this.SelectedChannelIndex = 0;
                }
            }
            else
            {
                var chvm = (AbstractChannelViewModel)e.OldItems[0];
                chvm.ChannelTabVM = null;
                this._visitedChannels.HandleRemovedIndex(e.OldStartingIndex);
                this._view.Items.RemoveAt(e.OldStartingIndex);
            }
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ChatMode")
            {
                foreach (AbstractChannelViewModel chvm in this.Channels)
                {
                    if (chvm is ChannelViewModel)
                    {
                        if (chvm.Joined)
                        {
                            chvm.LoadMessages(GlobalManager.MaxMessagesDisplayed, true);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}