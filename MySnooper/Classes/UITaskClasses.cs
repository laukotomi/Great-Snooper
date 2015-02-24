using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySnooper
{
    public abstract class UITask
    {
        public IRCCommunicator Sender { get; protected set; }
    }
    public class QuitUITask : UITask
    {
        public string ClientNameL { get; private set; }
        public string Message { get; private set; }

        public QuitUITask(IRCCommunicator sender, string clientNameL, string message)
        {
            this.Sender = sender;
            this.ClientNameL = clientNameL;
            this.Message = message;
        }
    }

    public class MessageUITask : UITask
    {
        public string ClientName { get; private set; }
        public string ChannelHash { get; private set; }
        public string Message { get; private set; }
        public MessageSetting Setting { get; private set; }

        public MessageUITask(IRCCommunicator sender, string clientName, string channelHash, string message, MessageSetting setting)
        {
            this.Sender = sender;
            this.ClientName = clientName;
            this.ChannelHash = channelHash;
            this.Message = message;
            this.Setting = setting;
        }
    }
    

    public class PartedUITask : UITask
    {
        public string ChannelHash { get; private set; }
        public string ClientNameL { get; private set; }

        public PartedUITask(IRCCommunicator sender, string channelHash, string clientNameL)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.ClientNameL = clientNameL;
        }
    }

    public class JoinedUITask : UITask
    {
        public string ChannelHash { get; private set; }
        public string ClientName { get; private set; }
        public string Clan { get; private set; }

        public JoinedUITask(IRCCommunicator sender, string channelHash, string clientName, string clan)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.ClientName = clientName;
            this.Clan = clan;
        }
    }

    public class ChannelListUITask : UITask
    {
        public SortedDictionary<string, string> ChannelList { get; private set; }

        public ChannelListUITask(IRCCommunicator sender, SortedDictionary<string, string> channelList)
        {
            this.Sender = sender;
            this.ChannelList = channelList;
        }
    }

    public class ClientUITask : UITask
    {
        public string ChannelHash { get; private set; }
        public string ClientName { get; private set; }
        public string Clan { get; private set; }
        public CountryClass Country { get; private set; }
        public int Rank { get; private set; }
        public bool ClientGreatSnooper { get; private set; }
        public string ClientApp { get; private set; }

        public ClientUITask(IRCCommunicator sender, string channelHash, string clientName, CountryClass country, string clan, int rank, bool clientGreatSnooper, string clientApp)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.ClientName = clientName;
            this.Country = country;
            this.Clan = clan;
            this.Rank = rank;
            this.ClientGreatSnooper = clientGreatSnooper;
            this.ClientApp = clientApp;
        }
    }

    public class OfflineUITask : UITask
    {
        public string ClientName { get; private set; }

        public OfflineUITask(IRCCommunicator sender, string clientName)
        {
            this.Sender = sender;
            this.ClientName = clientName;
        }
    }

    public class NickNameInUseTask : UITask
    {
        public NickNameInUseTask(IRCCommunicator sender)
        {
            this.Sender = sender;
        }
    }

    public class NickUITask : UITask
    {
        public string OldClientName { get; private set; }
        public string NewClientName { get; private set; }

        public NickUITask(IRCCommunicator sender, string oldClientName, string newClientName)
        {
            this.Sender = sender;
            this.OldClientName = oldClientName;
            this.NewClientName = newClientName;
        }
    }

    public class ClientAddOrRemoveTask : UITask
    {
        public enum TaskType { Add, Remove };
        public string ChannelHash { get; private set; }
        public string SenderName { get; private set; }
        public string ClientName { get; private set; }
        public TaskType Type { get; private set; }

        public ClientAddOrRemoveTask(IRCCommunicator sender, string channelHash, string senderName, string clientName, TaskType type)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.SenderName = senderName;
            this.ClientName = clientName;
            this.Type = type;
        }
    }

    public class ClientLeaveConvTask : UITask
    {
        public string ChannelHash { get; private set; }
        public string ClientName { get; private set; }

        public ClientLeaveConvTask(IRCCommunicator sender, string channelHash, string clientName)
        {
            this.Sender = sender;
            this.ChannelHash = channelHash;
            this.ClientName = clientName;
        }
    }
}
