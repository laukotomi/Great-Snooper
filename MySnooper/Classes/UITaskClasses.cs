using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySnooper
{
    public abstract class UITask
    {

    }
    public class QuitUITask : UITask
    {
        public string ClientName { get; private set; }
        public string Message { get; private set; }

        public QuitUITask(string clientName, string message)
        {
            this.ClientName = clientName;
            this.Message = message;
        }
    }

    public class MessageUITask : UITask
    {
        public string ClientName { get; private set; }
        public string ChannelName { get; private set; }
        public string Message { get; private set; }
        public MessageSetting Setting { get; private set; }

        public MessageUITask(string clientName, string channelName, string message, MessageSetting setting)
        {
            this.ClientName = clientName;
            this.ChannelName = channelName;
            this.Message = message;
            this.Setting = setting;
        }
    }
    

    public class PartedUITask : UITask
    {
        public string ChannelName { get; private set; }
        public string ClientName { get; private set; }

        public PartedUITask(string channelName, string clientName)
        {
            this.ChannelName = channelName;
            this.ClientName = clientName;
        }
    }

    public class JoinedUITask : UITask
    {
        public string ChannelName { get; private set; }
        public string ClientName { get; private set; }
        public string Clan { get; private set; }

        public JoinedUITask(string channelName, string clientName, string clan)
        {
            this.ChannelName = channelName;
            this.ClientName = clientName;
            this.Clan = clan;
        }
    }

    public class ChannelListUITask : UITask
    {
        public SortedDictionary<string, string> ChannelList { get; private set; }

        public ChannelListUITask(SortedDictionary<string, string> channelList)
        {
            this.ChannelList = channelList;
        }
    }

    public class ClientUITask : UITask
    {
        public string ChannelName { get; private set; }
        public string ClientName { get; private set; }
        public string Clan { get; private set; }
        public CountryClass Country { get; private set; }
        public int Rank { get; private set; }
        public bool ClientGreatSnooper { get; private set; }

        public ClientUITask(string channelName, string clientName, CountryClass country, string clan, int rank, bool clientGreatSnooper)
        {
            this.ChannelName = channelName;
            this.ClientName = clientName;
            this.Country = country;
            this.Clan = clan;
            this.Rank = rank;
            this.ClientGreatSnooper = clientGreatSnooper;
        }
    }

    public class OfflineUITask : UITask
    {
        public string ClientName { get; private set; }

        public OfflineUITask(string clientName)
        {
            this.ClientName = clientName;
        }
    }
}
