using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;



namespace MySnooper
{
    public enum IRCConnectionStates { OK, UsernameInUse, Quit, Error, Cancelled }

    // Delegates to communicate with the UI class. It will subscribe for the events of these delegates.
    public delegate void ListEndDelegate(SortedDictionary<string, string> channelList);
    public delegate void ClientDelegate(string channelName, string clientName, CountryClass country, string clan, int rank, bool ClientGreatSnooper);
    public delegate void JoinedDelegate(string channelName, string clientName, string clan);
    public delegate void PartedDelegate(string channelName, string clientName);
    public delegate void QuittedDelegate(string clientName, string message);
    public delegate void MessageDelegate(string clientName, string to, string message, MessageTypes messageType);
    public delegate void OfflineUserDelegate(string clientName);
    public delegate void ConnectionStateDelegate(IRCConnectionStates state);


    public class IRCCommunicator
    {
        private bool Cancel = false;
        private object CancelLocker = new object();

        // Private IRC variables
        private string ServerAddress;
        private string ServerIrcAddress;
        private int ServerPort;

        // Locker
        private object SendLocker;
        private object ChannelLocker;

        // Buffers
        private byte[] RecvBuffer; // stores the bytes arrived from WormNet server. These bytes will be decoding into RecvMessage or into RecvHTML
        private byte[] SendBuffer; // stores the encoded bytes from the items of the ToSend list which will be sent to WormNet server.
        private List<string> ToSend; // list of the messages to be sent to the WormNet

        // Events to communicate with the UI class
        public event ListEndDelegate ListEnd;
        public event ClientDelegate Client;
        public event JoinedDelegate Joined;
        public event PartedDelegate Parted;
        public event QuittedDelegate Quitted;
        public event MessageDelegate Message;
        public event OfflineUserDelegate OfflineUser;
        public event ConnectionStateDelegate ConnectionState;


        private bool GetChannels = false;
        private SortedDictionary<string, string> ChannelList = new SortedDictionary<string, string>();


        // Regular expressions
        // #Help 10 :05 A place to get help, or help others
        private Regex channelRegex = new Regex(@"((#|&)\S+)[^:]+\S+\s(.*)");
        // #AnythingGoes ~UserName no.address.for.you wormnet1.team17.com Herbsman H :0 68 7 LT The Wheat Snooper 2.8
        private Regex clientRegex =  new Regex(@"((#|&)\S+)\s~?(\S+)\s\S+\s\S+\s(\S+)[^:]+\S+\s(.*)");
        // :sToOMiToO!~AeF@no.address.for.you JOIN :#RopersHeaven
        // :AeF`T!Username@no.address.for.you PRIVMSG #AnythingGoes :\x01ACTION ads\x01
        private Regex messageRegex = new Regex(@":?([^!]+)!~?([^@]+)\S+\s(\S+)\s:?(.*)");


        // Constructor
        public IRCCommunicator(string ServerAddress, int ServerPort)
        {
            this.ServerAddress = ServerAddress;
            this.ServerIrcAddress = ':' + ServerAddress;
            this.ServerPort = ServerPort;

            RecvBuffer = new byte[10240]; // 10kB
            SendBuffer = new byte[1024]; // 1kB
            SendLocker = new object();
            ChannelLocker = new object();
            ToSend = new List<string>();
        }


        public void CancelAsync()
        {
            lock (CancelLocker)
            {
                Cancel = true;
            }
        }


        // Send a message to WormNet
        public void Send(string message)
        {
            lock (SendLocker)
            {
                ToSend.Add(message + "\r\n");
            }
        }

        public void ClearRequests()
        {
            ToSend.Clear();
        }

        // Disconnect wormnet
        public void Disconnect()
        {
            Send("QUIT :Great Snooper v" + App.getVersion());
        }

        public void JoinChannel(string channelName)
        {
            Send("JOIN " + channelName);
        }

        public void LeaveChannel(string channelName)
        {
            Send("PART " + channelName);
        }

        public void GetChannelList()
        {
            lock (ChannelLocker)
            {
                GetChannels = true;
                ChannelList.Clear();
            }
            Send("LIST");
        }

        public void GetChannelClients(string channelName)
        {
            Send("WHO " + channelName);
        }

        public void GetInfoAboutClient(string clientName)
        {
            Send("WHO " + clientName);
        }


        // The login messages
        private void SendLoginMessages()
        {
            Send("PASS ELSILRACLIHP"); // Password
            Send("NICK " + GlobalManager.User.Name); // Nick

            // USER Username hostname servername :41 0 RU StepS
            int countryID = GlobalManager.User.Country.ID;
            if (countryID > 52)
                countryID = 49;

            Send("USER " + GlobalManager.User.Clan + " hostname servername :" + countryID.ToString() + " " + GlobalManager.User.Rank.ID.ToString() + " " + GlobalManager.User.Country.CountryCode + " Great Snooper v" + App.getVersion()); // USER message
        }


        // The thread logic is here
        public void run()
        {
            // Some needed variables
            int bytes; // The number of bytes arrived
            int spacePos; // To process the data arrived
            string sender;
            DateTime lastAction = DateTime.Now;
            TimeSpan idleTimeout = new TimeSpan(0, 0, 30);
            TimeSpan disconnectTimeout = new TimeSpan(0, 0, 90);
            StringBuilder RecvMessage = new StringBuilder(RecvBuffer.Length);

            // Let's try something ;)
            try
            {
                using (Socket IRCServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    IRCServer.Connect(System.Net.Dns.GetHostAddresses(ServerAddress), ServerPort);

                    // Let's log in
                    SendLoginMessages();

                    // Variables for the infinite loop to work
                    bool stop = false; // To stop the infinite loop

                    // The logic
                    while (!stop)
                    {
                        lock (CancelLocker)
                        {
                            if (Cancel)
                            {
                                if (ConnectionState != null)
                                    ConnectionState.BeginInvoke(IRCConnectionStates.Cancelled, null, null);
                                return;
                            }
                        }

                        if (DateTime.Now - lastAction >= disconnectTimeout)
                            break; // Connection lost

                        if (IRCServer.Poll(300000, SelectMode.SelectRead) && IRCServer.Available == 0)
                            break; // Connection lost

                        // RECEIVE data if there is any (without blocking the thread)
                        if (IRCServer.Available > 0)
                        {
                            lastAction = DateTime.Now;
                            bytes = IRCServer.Receive(RecvBuffer); // Read the arrived datas into the buffer with a maximal length of the buffer (if the data is bigger than the buffer, it will be read in the next loop)
                            for (int i = 0; i < bytes; i++)
                            {
                                RecvMessage.Append(WormNetCharTable.Decode[RecvBuffer[i]]); //Decode the bytes into RecvMessage
                            }

                            // process the message line-by-line
                            string[] lines = RecvMessage.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None);
                            for (int i = 0; i < lines.Length - 1; i++) // the last line is either string.Empty or a line which end isn't arrived yet
                            {
                                // Get the sender of the message
                                spacePos = lines[i].IndexOf(' ');
                                if (spacePos != -1)
                                {
                                    sender = lines[i].Substring(0, spacePos).ToLower();
                                    // If it is a server message
                                    // :wormnet1.team17.com 322 Test #Help 10 :05 A place to get help, or help others
                                    if (sender == ServerIrcAddress)
                                        stop = ProcessServerMessage(lines[i], spacePos);

                                    // PING Message
                                    else if (sender == "ping")
                                    {
                                        Send("PONG " + lines[i].Substring(spacePos + 1));
                                    }

                                    // Closing link message after QUIT or ban ;)
                                    else if (sender == "error")
                                    {
                                        // ERROR :Closing Link: Test[~Test@77.111.187.159] (Test)
                                        System.Windows.MessageBox.Show("An error occurred: " + lines[i].Substring(spacePos + 2));
                                    }

                                    // Message
                                    // :sToOMiToO!~AeF@no.address.for.you JOIN :#RopersHeaven
                                    else
                                        stop = ProcessClientMessage(lines[i]);

                                    // STOP
                                    if (stop) break;
                                }
                            }

                            // STOP
                            if (stop)
                                break;

                            // Clear processed data from the buffer
                            RecvMessage.Clear();
                            if (lines[lines.Length - 1] != string.Empty)
                            {
                                RecvMessage.Append(lines[lines.Length - 1]);
                            }
                        }

                        if (DateTime.Now - lastAction >= idleTimeout)
                        {
                            Send("PING " + ServerAddress);
                        }

                        // SEND
                        lock (SendLocker)
                        {
                            if (ToSend.Count > 0)
                            {
                                for (int i = 0; i < ToSend.Count; i++)
                                {
                                    for (int j = 0; j < ToSend[i].Length && j < SendBuffer.Length; j++)
                                    {
                                        //if (WormNetCharTable.Encode.ContainsKey(ToSend[i][j])) (already validated in MainWindow.Messages.cs:MessageSend()
                                            SendBuffer[j] = WormNetCharTable.Encode[ToSend[i][j]];
                                    }
                                    IRCServer.Send(SendBuffer, 0, ToSend[i].Length, SocketFlags.None);

                                    if (ToSend[i].Substring(0, 4) == "QUIT")
                                    {
                                        stop = true;
                                        break;
                                    }
                                }
                                ToSend.Clear();
                            }
                        }
                    }
                }
                if (ConnectionState != null)
                    ConnectionState.BeginInvoke(IRCConnectionStates.Quit, null, null);
            }
            // We don't want these, but they may happen!
            catch (Exception ex)
            {
                ErrorLog.log(ex);

                lock (CancelLocker)
                {
                    if (Cancel)
                    {
                        if (ConnectionState != null)
                            ConnectionState.BeginInvoke(IRCConnectionStates.Cancelled, null, null);
                        return;
                    }
                }

                if (ConnectionState != null)
                    ConnectionState.BeginInvoke(IRCConnectionStates.Error, null, null);
            }
        }

        private bool ProcessClientMessage(string line)
        {
            bool stop = false;

            // :Test!~Test@no.address.for.you PART #AnythingGoes
            Match m = messageRegex.Match(line);
            if (!m.Success)
                return stop;

            string clientName = m.Groups[1].Value;
            string clan = m.Groups[2].Value.ToLower() == "username" ? string.Empty : m.Groups[2].Value;
            string command = m.Groups[3].Value.ToLower();

            // :sToOMiToO2!~AeF@no.address.for.you PART #AnythingGoes
            if (command == "part" && Parted != null)
            {
                string channelName = m.Groups[4].Value.ToLower();
                Parted(channelName, clientName);
            }

            // :sToOMiToO!~AeF@no.address.for.you JOIN :#RopersHeaven
            else if (command == "join" && Joined != null)
            {
                string channelName = m.Groups[4].Value.ToLower();
                Joined(channelName, clientName, clan);
            }

            // :Snaker!Username@no.address.for.you QUIT :Joined Game
            else if (command == "quit")
            {
                string message = m.Groups[4].Value;
                if (clientName.ToLower() == GlobalManager.User.LowerName)
                {
                    System.Windows.MessageBox.Show("Server disconnected: " + message);
                    stop = true;
                }

                if (Quitted != null)
                    Quitted(clientName, message);
            }

            // :Don-Coyote!Username@no.address.for.you PRIVMSG #AnythingGoes :can u take my oral
            // :AeF`T!Username@no.address.for.you PRIVMSG #AnythingGoes :\x01ACTION ads\x01
            else if ((command == "privmsg" || command == "notice") && Message != null)
            {
                int spacePos = m.Groups[4].Value.IndexOf(' ');
                if (spacePos != -1)
                {
                    string channelName = m.Groups[4].Value.Substring(0, spacePos).ToLower();
                    string message = m.Groups[4].Value.Substring(spacePos + 2);

                    MessageTypes messageType;

                    // Is it action message? (CTCP message) (\x01ACTION message..\x01)
                    if (message[0] == '\x01' && message[message.Length - 1] == '\x01')
                    {
                        spacePos = message.IndexOf(' ');
                        if (spacePos != -1)
                        {
                            string ctcpCommand = message.Substring(1, spacePos - 1).ToLower();
                            if (ctcpCommand == "action")
                            {
                                message = message.Substring(spacePos + 1, message.Length - spacePos - 2);
                                messageType = MessageTypes.Action;
                            }
                            else return stop;
                        }
                        else return stop;
                    }
                    else
                    {
                        messageType = (command == "privmsg") ? MessageTypes.Channel : MessageTypes.Notice;
                    }

                    Message(clientName, channelName, message, messageType);
                }
            }

            return stop;
        }

        private bool ProcessServerMessage(string line, int spacePos)
        {
            Match m;
            int number;
            bool stop = false;

            // Get the number out of the message
            // :wormnet1.team17.com 322 Test #Help 10 :05 A place to get help, or help others
            int spacePos2 = line.IndexOf(' ', spacePos + 1);
            if (spacePos2 == -1 || !int.TryParse(line.Substring(spacePos + 1, spacePos2 - spacePos - 1), out number))
                return stop;

            // Find the space after our nickname in the message
            spacePos = line.IndexOf(' ', spacePos2 + 1);
            if (spacePos == -1)
                return stop;
            spacePos++; // we will start all the regex match from this position

            // Process the message
            switch (number)
            {
                // End of message of the day, we can get the channel list
                case 1:
                    if (ConnectionState != null)
                        ConnectionState(IRCConnectionStates.OK);
                    break;

                // This nickname is already in use!
                case 433:
                    if (ConnectionState != null)
                        ConnectionState(IRCConnectionStates.UsernameInUse);
                        stop = true;
                    break;

                // A channel (answer for the LIST command)
                case 322:
                    lock (ChannelLocker)
                    {
                        if (GetChannels)
                        {
                            // :wormnet1.team17.com 322 Test #Help 10 :05 A place to get help, or help others
                            m = channelRegex.Match(line, spacePos);
                            if (m.Success)
                            {
                                string channelName = m.Groups[1].Value;
                                string description = m.Groups[3].Value;

                                if (!ChannelList.ContainsKey(channelName))
                                {
                                    ChannelList.Add(channelName, description);
                                }
                            }
                        }
                    }
                    break;

                // LIST END
                case 323:
                    lock (ChannelLocker)
                    {
                        GetChannels = false;
                    }

                    if (ListEnd != null)
                        ListEnd(ChannelList);
                    break;

                // A client (answer for WHO command)
                case 352:
                    // :wormnet1.team17.com 352 Test #AnythingGoes ~UserName no.address.for.you wormnet1.team17.com Herbsman H :0 68 7 LT The Wheat Snooper 2.8
                    if (Client != null)
                    {
                        m = clientRegex.Match(line, spacePos);
                        if (m.Success)
                        {
                            string channelName = m.Groups[1].Value.ToLower();
                            string clan = m.Groups[3].Value.ToLower() == "username" ? string.Empty : m.Groups[3].Value;
                            string clientName = m.Groups[4].Value;
                            string[] realName = m.Groups[5].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // 68 7 LT The Wheat Snooper

                            CountryClass country = CountriesClass.GetCountryByID(49);
                            int rank = 0;
                            bool ClientGreatSnooper = false;

                            if (realName.Length >= 3)
                            {
                                if (int.TryParse(realName[1], out rank))
                                {
                                    if (rank > 13)
                                        rank = 13;
                                    if (rank < 0)
                                        rank = 0;
                                }

                                int countrycode;
                                if (int.TryParse(realName[0], out countrycode) && countrycode >= 0 && countrycode <= 52)
                                {
                                    if (countrycode == 49 && realName[2].Length == 2) // use cc as countricode
                                    {
                                        if (realName[2] == "UK")
                                            realName[2] = "GB";
                                        else if (realName[2] == "EL")
                                            realName[2] = "GR";
                                        country = CountriesClass.GetCountryByCC(realName[2]);
                                    }
                                    else
                                        country = CountriesClass.GetCountryByID(countrycode);
                                }
                                else if (realName[2].Length == 2) // use cc if countrycode is bigger than 52
                                {
                                    if (realName[2] == "UK")
                                        realName[2] = "GB";
                                    else if (realName[2] == "EL")
                                        realName[2] = "GR";
                                    country = CountriesClass.GetCountryByCC(realName[2]);
                                }

                                ClientGreatSnooper = realName.Length >= 5 && realName[3] == "Great" && realName[4] == "Snooper";
                            }

                            Client(channelName, clientName, country, clan, rank, ClientGreatSnooper);
                        }
                    }
                    break;

                // The user is offline message
                case 401:
                    // :wormnet1.team17.com 401 Test sToOMiToO :No such nick/channel
                    if (OfflineUser != null)
                    {
                        spacePos2 = line.IndexOf(' ', spacePos);
                        if (spacePos2 != -1)
                        {
                            string clientName = line.Substring(spacePos, spacePos2 - spacePos);
                            OfflineUser(clientName);
                        }
                    }
                    break;
            }

            return stop;
        }
    }
}
