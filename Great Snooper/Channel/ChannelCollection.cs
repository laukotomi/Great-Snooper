using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using GreatSnooper.ViewModel;

namespace GreatSnooper.Channel
{
    public class ChannelCollection
    {
        private List<AbstractChannelViewModel> _allChannels = new List<AbstractChannelViewModel>();
        private List<ChannelViewModel> _channels = new List<ChannelViewModel>();
        private List<PMChannelViewModel> _pmChannels = new List<PMChannelViewModel>();

        public ReadOnlyCollection<AbstractChannelViewModel> AllChannels
        {
            get { return _allChannels.AsReadOnly(); }
        }

        public ReadOnlyCollection<ChannelViewModel> Channels
        {
            get { return _channels.AsReadOnly(); }
        }

        public ReadOnlyCollection<PMChannelViewModel> PmChannels
        {
            get { return _pmChannels.AsReadOnly(); }
        }

        public void Add(ChannelViewModel chvm)
        {
            _allChannels.Add(chvm);
            _channels.Add(chvm);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, chvm);
        }

        public void Add(PMChannelViewModel chvm)
        {
            _allChannels.Add(chvm);
            _pmChannels.Add(chvm);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, chvm);
        }

        public void Remove(ChannelViewModel chvm)
        {
            _allChannels.Remove(chvm);
            _channels.Remove(chvm);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, chvm);
        }

        public void Remove(PMChannelViewModel chvm)
        {
            _allChannels.Remove(chvm);
            _pmChannels.Remove(chvm);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, chvm);
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, AbstractChannelViewModel chvm)
        {
            NotifyCollectionChangedEventHandler handler = CollectionChanged;
            if (handler != null)
            {
                handler(this, new NotifyCollectionChangedEventArgs(action, chvm));
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Clear()
        {
            _allChannels.Clear();
            _channels.Clear();
            _pmChannels.Clear();
        }
    }
}
