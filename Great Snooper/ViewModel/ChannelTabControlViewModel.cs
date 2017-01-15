using GalaSoft.MvvmLight;
using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using System.Linq;
using System.Windows.Controls;

namespace GreatSnooper.ViewModel
{
    class ChannelTabControlViewModel : ViewModelBase
    {
        private readonly SortedObservableCollection<AbstractChannelViewModel> _channels =
            new SortedObservableCollection<AbstractChannelViewModel>();
        public SortedObservableCollection<AbstractChannelViewModel> Channels
        {
            get
            {
                return this._channels;
            }
        }

        private int _selectedChannelIndex = -1;
        public int SelectedChannelIndex
        {
            get { return this._selectedChannelIndex; }
            set
            {
                PMChannelViewModel oldPMChannel = this._selectedChannel as PMChannelViewModel;

                if (value == -1)
                {
                    this._selectedChannelIndex = value;
                    this._selectedChannel = null;
                }
                else if (value > 0 && value < this.Channels.Count)
                {
                    this._selectedChannelIndex = value;
                    this.visitedChannels.Visit(value);
                    this._selectedChannel = Channels[value];
                    this._selectedChannel.IsHighlighted = false;

                    PMChannelViewModel pmChannel = this._selectedChannel as PMChannelViewModel;
                    if (pmChannel != null)
                        pmChannel.GenerateHeader();
                }

                if (oldPMChannel != null)
                    oldPMChannel.GenerateHeader();

                this.RaisePropertyChanged("SelectedChannelIndex");
            }
        }

        private AbstractChannelViewModel _selectedChannel;
        public AbstractChannelViewModel SelectedChannel
        {
            get { return this._selectedChannel; }
            set
            {
                int index = this.Channels.IndexOf(value);
                if (index != -1)
                {
                    this.SelectedChannelIndex = index;
                }
            }
        }

        private readonly VisitedChannels visitedChannels = new VisitedChannels();
        private readonly TabControl view;

        public ChannelTabControlViewModel(TabControl view)
        {
            this.view = view;

            this.Channels.CollectionChanged += Channels_CollectionChanged;
            Properties.Settings.Default.PropertyChanged += SettingsChanged;
        }

        private void Channels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var chvm = (AbstractChannelViewModel)e.NewItems[0];
                this.view.Items.Insert(e.NewStartingIndex, chvm.GetLayout());

                if (this.Channels.Count == 1)
                {
                    this.SelectedChannelIndex = 0;
                }
            }
            else
            {
                var chvm = (AbstractChannelViewModel)e.OldItems[0];
                this.view.Items.RemoveAt(e.OldStartingIndex);
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
                            chvm.LoadMessages(GlobalManager.MaxMessagesDisplayed, true);
                    }
                    else
                        break;
                }
            }
        }

        public void CloseChannelTab(AbstractChannelViewModel chvm)
        {
            int index = (this.SelectedChannel == chvm)
                ? this.SelectedChannelIndex
                : this.Channels.IndexOf(chvm);

            if (this.SelectedChannel == chvm) // Channel was selected
            {
                int lastindex = this.visitedChannels.GetBeforeLastIndex();
                if (lastindex != -1)
                    this.SelectedChannelIndex = lastindex;
            }

            this.visitedChannels.HandleRemovedIndex(index);

            ChannelViewModel channel = chvm as ChannelViewModel;
            if (channel != null)
            {
                if (channel.Joined)
                    channel.LeaveChannelCommand.Execute(null);
            }
            else
            {
                chvm.Log(chvm.Messages.Count, true);
                chvm.ClearUsers();
                chvm.Server.Channels.Remove(chvm.Name);
            }

            if (chvm.Server is GameSurgeCommunicator && chvm.Server.Channels.Any(x => x.Value.Joined) == false)
                chvm.Server.CancelAsync();

            this.Channels.Remove(chvm);

            // Refresh selected channel, because selected item will be lost
            int lastIndex = this.visitedChannels.GetLastIndex();
            if (lastIndex != -1)
                this.SelectedChannelIndex = lastIndex;
        }
    }
}
