using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MySnooper
{
    public delegate void NewMessageAddedDelegate(Channel ch, MessageClass message, bool insert = false, bool LeagueFound = false);
    public delegate void ChannelLeavingDelegate(Channel ch);

    public class Channel : IComparable, INotifyPropertyChanged
    {
        private static System.Text.RegularExpressions.Regex DateRegex = new System.Text.RegularExpressions.Regex(@"[^0-9]");

        // Private variables to make properties work well
        private string _Scheme;
        private bool _IsPrivMsgChannel;
        private bool _NewMessages;

        // Channel Variables
        public string Name { get; private set; }
        public string LowerName { get; private set; }
        public string Description { get; private set; }
        public bool Joined { get; private set; }
        public bool CanHost { get; private set; }

        // Messages
        public List<MessageClass> Messages { get; set; }
        public bool BeepSoundPlay { get; set; }
        public bool AwaySent { get; set; }
        public int MessagesLoadedFrom = 0;

        // Clients
        public SortedObservableCollection<Client> Clients { get; set; }
        public Client TheClient { get; private set; } // If channel is private message channel then we store the user data here

        // GameList
        public SortedObservableCollection<Game> GameList { get; set; }
        public DateTime GameListUpdatedTime { get; set; }

        // Up & Down keys
        private List<string> UserMessages { get; set; }
        public int UserMessageLoadedIdx { get; set; }
        public string TempMessage { get; set; } // We store the user message here when the user presses up or down keys to make it possible to restore the message

        // Events
        public event NewMessageAddedDelegate NewMessageAdded;
        public event ChannelLeavingDelegate ChannelLeaving;

        // View variables
        public TabItem TheTabItem { get; set; }
        public Border DisconnectedLayout { get; set; }
        public Border ConnectedLayout { get; set; }
        public RichTextBox TheRichTextBox { get; set; }
        public FlowDocument TheFlowDocument { get; set; }
        public ListBox GameListBox { get; set; }
        public TextBox TheTextBox { get; set; }
        public DataGrid TheDataGrid { get; set; }
        public bool ShowOlderMessagesInserted { get; set; }

        // These properties may change and then they will notify the UI thread about that
        public string Scheme
        {
            get
            {
                return _Scheme;
            }
            set
            {
                _Scheme = value;
                // Now we know if we can host or not in this channel
                CanHost = _Scheme != string.Empty && !_Scheme.Contains("Tf");
            }
        }

        public bool IsPrivMsgChannel
        {
            get
            {
                return _IsPrivMsgChannel;
            }
            set
            {
                _IsPrivMsgChannel = value;
                if (_IsPrivMsgChannel)
                {
                    Joined = true;
                }
            }
        }

        public bool NewMessages
        {
            get
            {
                return _NewMessages;
            }
            set
            {
                _NewMessages = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("NewMessages"));
            }
        }

        // Constructor
        public Channel(string Name, string Description, Client TheClient = null)
        {
            this.Name = Name;
            this.LowerName = Name.ToLower();
            this.Description = Description;
            Joined = false;
            CanHost = false;
            Messages = new List<MessageClass>(GlobalManager.MaxMessagesInMemory);
            Clients = new SortedObservableCollection<Client>();
            GameList = new SortedObservableCollection<Game>();
            GameListUpdatedTime = new DateTime(1999, 5, 31);
            UserMessages = new List<string>(5);
            NewMessages = false;
            BeepSoundPlay = true;
            AwaySent = false;
            UserMessageLoadedIdx = -1;
            ShowOlderMessagesInserted = false;
            Scheme = string.Empty;
            IsPrivMsgChannel = Name[0] != '#' && Name[0] != '&';
            this.TheClient = TheClient;
        }


        // Add a message
        public void AddMessage(Client Sender, string Message, MessageSetting style, bool LeagueFound = false)
        {
            if (Messages.Count + 1 > GlobalManager.MaxMessagesInMemory)
            {
                Log(GlobalManager.NumOfOldMessagesToBeLoaded);
            }

            //var sv = (ScrollViewer)TheRichTextBox.Parent;
            //if (sv.VerticalOffset == sv.ScrollableHeight && Messages.Count > GlobalManager.MaxMessagesDisplayed && MessagesLoadedFrom + 1 + GlobalManager.MaxMessagesDisplayed < GlobalManager.MaxMessagesInMemory)
            //    MessagesLoadedFrom++;

            MessageClass message = new MessageClass(Sender, Message, style);
            Messages.Add(message);

            if (NewMessageAdded != null)
                NewMessageAdded(this, message, false, LeagueFound);
        }

        public void Join()
        {
            Joined = true;
            TheTabItem.Content = ConnectedLayout;
        }

        public void Part()
        {
            if (ChannelLeaving != null)
                ChannelLeaving(this);

            Joined = false;
            Log(Messages.Count, true);

            Clients.Clear();
            Messages.Clear();
            GameList.Clear();
            TheFlowDocument.Blocks.Clear();

            TheTabItem.Content = DisconnectedLayout;
        }

        public void UserMessagesAdd(string message)
        {
            if (UserMessages.Count == 0 || UserMessages[0] != message)
            {
                if (UserMessages.Count == UserMessages.Capacity)
                    UserMessages.RemoveAt(UserMessages.Count - 1);
                UserMessages.Insert(0, message);
            }
        }

        public string LoadNextUserMessage()
        {
            if (UserMessageLoadedIdx + 1 == UserMessages.Count)
            {
                UserMessageLoadedIdx++;
                return string.Empty;
            }
            else if (UserMessageLoadedIdx + 1 > UserMessages.Count)
                return string.Empty;

            return UserMessages[++UserMessageLoadedIdx];
        }

        public string LoadPrevUserMessage()
        {
            if (UserMessageLoadedIdx - 1 == -1)
            {
                UserMessageLoadedIdx--;
                return string.Empty;
            }
            else if (UserMessageLoadedIdx - 1 < -1)
                return string.Empty;

            return UserMessages[--UserMessageLoadedIdx];
        }


        public void Log(int db, bool makeend = false)
        {
            if (db == 0) return;

            string settingsPath = Directory.GetParent(Directory.GetParent(System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).FullName).FullName;
            if (!Directory.Exists(settingsPath + @"\Logs\" + Name))
                Directory.CreateDirectory(settingsPath + @"\Logs\" + Name);

            string logpath = settingsPath + @"\Logs\" + Name + "\\" + DateRegex.Replace(DateTime.Now.ToString("d"), "-") + ".log";

            using (StreamWriter writer = new StreamWriter(logpath, true)) 
            {
                for (int i = 0; i < db && Messages.Count > 0; i++)
                {
                    MessageClass msg = Messages[0];
                    writer.WriteLine("(" + msg.MessageType.ToString() + ") " + msg.Time.ToString("G") + " " + msg.Sender.Name + ": " + msg.Message);
                    Messages.RemoveAt(0);
                    MessagesLoadedFrom--;
                }

                if (makeend)
                {
                    writer.WriteLine(DateTime.Now.ToString("G") + " - Channel closed.");
                    writer.WriteLine("-----------------------------------------------------------------------------------------");
                    writer.WriteLine(Environment.NewLine + Environment.NewLine);
                }
            }
        }


        // IComparable interface
        public int CompareTo(object obj)
        {
            var obj2 = obj as Channel;
            int first = _IsPrivMsgChannel.CompareTo(obj2._IsPrivMsgChannel);
            if (first != 0)
                return first;
            return LowerName.CompareTo(obj2.LowerName);
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Channel ch = obj as Channel;
            if ((System.Object)ch == null)
            {
                return false;
            }

            // Return true if the fields match:
            return LowerName == ch.LowerName;
        }

        public bool Equals(Channel ch)
        {
            // If parameter is null return false:
            if ((object)ch == null)
            {
                return false;
            }

            // Return true if the fields match:
            return LowerName == ch.LowerName;
        }

        public override int GetHashCode()
        {
            return LowerName.GetHashCode();
        }

        public static bool operator ==(Channel a, Channel b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.LowerName == b.LowerName;
        }

        public static bool operator !=(Channel a, Channel b)
        {
            return !(a == b);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
