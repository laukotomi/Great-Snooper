using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;



namespace MySnooper
{
    public enum IRCConnectionStates { OK, UsernameInUse, Quit, Error, Cancelled }

    // Delegates to communicate with the UI class. It will subscribe for the events of these delegates.
    //public delegate void ListEndDelegate(SortedDictionary<string, string> channelList);
    //public delegate void ClientDelegate(string channelName, string clientName, CountryClass country, string clan, int rank, bool ClientGreatSnooper);
    //public delegate void JoinedDelegate(string channelName, string clientName, string clan);
    //public delegate void PartedDelegate(string channelName, string clientName);
    //public delegate void QuittedDelegate(string clientName, string message);
    //public delegate void MessageDelegate(string clientName, string to, string message, MessageSetting setting);
    //public delegate void OfflineUserDelegate(string clientName);
    public delegate void ConnectionStateDelegate(IRCConnectionStates state);


    public class IRCCommunicator
    {
        private bool cancel = false;
        private object cancelLocker = new object();

        // Private IRC variables
        private string serverAddress;
        private string serverIrcAddress;
        private int serverPort;

        // Locker
        private object sendLocker;
        private object channelLocker;

        // Buffers
        private byte[] recvBuffer; // stores the bytes arrived from WormNet server. These bytes will be decoding into RecvMessage or into RecvHTML
        private byte[] sendBuffer; // stores the encoded bytes from the items of the ToSend list which will be sent to WormNet server.
        private List<string> toSend; // list of the messages to be sent to the WormNet

        // Events to communicate with the UI class
        //public event ListEndDelegate ListEnd;
        //public event ClientDelegate Client;
        //public event JoinedDelegate Joined;
        //public event PartedDelegate Parted;
        //public event QuittedDelegate Quitted;
        //public event MessageDelegate Message;
        //public event OfflineUserDelegate OfflineUser;
        public event ConnectionStateDelegate ConnectionState;


        private bool getChannels = false;
        private SortedDictionary<string, string> channelList = new SortedDictionary<string, string>();


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
            this.serverAddress = ServerAddress;
            this.serverIrcAddress = ':' + ServerAddress;
            this.serverPort = ServerPort;

            recvBuffer = new byte[10240]; // 10kB
            sendBuffer = new byte[1024]; // 1kB
            sendLocker = new object();
            channelLocker = new object();
            toSend = new List<string>();
        }


        public void CancelAsync()
        {
            lock (cancelLocker)
            {
                cancel = true;
            }
        }


        // Send a message to WormNet
        public void Send(string message)
        {
            lock (sendLocker)
            {
                toSend.Add(message + "\r\n");
            }
        }

        public void ClearRequests()
        {
            toSend.Clear();
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
            lock (channelLocker)
            {
                getChannels = true;
                channelList.Clear();
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

            string nickClan = GlobalManager.User.Clan;
            if (nickClan.Length == 0)
                nickClan = "Username";

            Send("USER " + nickClan + " hostname servername :" + countryID.ToString() + " " + GlobalManager.User.Rank.ID.ToString() + " " + GlobalManager.User.Country.CountryCode + " Great Snooper v" + App.getVersion()); // USER message
        }


        // The thread logic is here
        public void run()
        {
            // Some needed variables
            int bytes; // The number of bytes arrived
            int spacePos; // To process the data arrived
            string sender;
            DateTime lastAction = DateTime.Now;
            TimeSpan idleTimeout = new TimeSpan(0, 0, 20);
            TimeSpan disconnectTimeout = new TimeSpan(0, 0, 90);
            StringBuilder RecvMessage = new StringBuilder(recvBuffer.Length);

            // Let's try something ;)
            try
            {
                using (Socket IRCServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    IRCServer.Connect(System.Net.Dns.GetHostAddresses(serverAddress), serverPort);

                    // Let's log in
                    SendLoginMessages();

                    // Variables for the infinite loop to work
                    bool stop = false; // To stop the infinite loop

                    // The logic
                    while (!stop)
                    {
                        lock (cancelLocker)
                        {
                            if (cancel)
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
                            bytes = IRCServer.Receive(recvBuffer); // Read the arrived datas into the buffer with a maximal length of the buffer (if the data is bigger than the buffer, it will be read in the next loop)
                            for (int i = 0; i < bytes; i++)
                            {
                                RecvMessage.Append(WormNetCharTable.Decode[recvBuffer[i]]); //Decode the bytes into RecvMessage
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
                                    if (sender == serverIrcAddress)
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
                            Send("PING " + serverAddress);
                        }

                        // SEND
                        lock (sendLocker)
                        {
                            if (toSend.Count > 0)
                            {
                                for (int i = 0; i < toSend.Count; i++)
                                {
                                    for (int j = 0; j < toSend[i].Length && j < sendBuffer.Length; j++)
                                    {
                                        //if (WormNetCharTable.Encode.ContainsKey(ToSend[i][j])) (already validated in MainWindow.Messages.cs:MessageSend()
                                            sendBuffer[j] = WormNetCharTable.Encode[toSend[i][j]];
                                    }
                                    IRCServer.Send(sendBuffer, 0, toSend[i].Length, SocketFlags.None);

                                    if (toSend[i].Substring(0, 4) == "QUIT")
                                    {
                                        stop = true;
                                        break;
                                    }
                                }
                                toSend.Clear();
                            }
                        }
                        Thread.Sleep(200);
                    }
                }
                if (ConnectionState != null)
                    ConnectionState.BeginInvoke(IRCConnectionStates.Quit, null, null);
            }
            // We don't want these, but they may happen!
            catch (Exception ex)
            {
                ErrorLog.log(ex);

                lock (cancelLocker)
                {
                    if (cancel)
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
            if (command == "part")
            {
                string channelName = m.Groups[4].Value.ToLower();
                GlobalManager.UITasks.Enqueue(new PartedUITask(channelName, clientName));
            }

            // :sToOMiToO!~AeF@no.address.for.you JOIN :#RopersHeaven
            else if (command == "join")
            {
                string channelName = m.Groups[4].Value.ToLower();
                GlobalManager.UITasks.Enqueue(new JoinedUITask(channelName, clientName, clan));
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

                GlobalManager.UITasks.Enqueue(new QuitUITask(clientName, message));
            }

            // :Don-Coyote!Username@no.address.for.you PRIVMSG #AnythingGoes :can u take my oral
            // :AeF`T!Username@no.address.for.you PRIVMSG #AnythingGoes :\x01ACTION ads\x01
            else if ((command == "privmsg" || command == "notice"))
            {
                int spacePos = m.Groups[4].Value.IndexOf(' ');
                if (spacePos != -1)
                {
                    string channelName = m.Groups[4].Value.Substring(0, spacePos).ToLower();
                    string message = m.Groups[4].Value.Substring(spacePos + 2);

                    MessageSetting setting;

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
                                setting = MessageSettings.ActionMessage;
                            }
                            else return stop;
                        }
                        else return stop;
                    }
                    else
                    {
                        setting = (command == "privmsg") ? MessageSettings.ChannelMessage : MessageSettings.NoticeMessage;
                    }

                    GlobalManager.UITasks.Enqueue(new MessageUITask(clientName, channelName, message, setting));
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
                    lock (channelLocker)
                    {
                        if (getChannels)
                        {
                            // :wormnet1.team17.com 322 Test #Help 10 :05 A place to get help, or help others
                            m = channelRegex.Match(line, spacePos);
                            if (m.Success)
                            {
                                string channelName = m.Groups[1].Value;
                                string description = m.Groups[3].Value;

                                if (!channelList.ContainsKey(channelName))
                                {
                                    channelList.Add(channelName, description);
                                }
                            }
                        }
                    }
                    break;

                // LIST END
                case 323:
                    lock (channelLocker)
                    {
                        getChannels = false;
                    }

                    GlobalManager.UITasks.Enqueue(new ChannelListUITask(channelList));
                    break;

                // A client (answer for WHO command)
                case 352:
                    // :wormnet1.team17.com 352 Test #AnythingGoes ~UserName no.address.for.you wormnet1.team17.com Herbsman H :0 68 7 LT The Wheat Snooper 2.8
                    m = clientRegex.Match(line, spacePos);
                    if (m.Success)
                    {
                        string channelName = m.Groups[1].Value.ToLower();
                        string clan = m.Groups[3].Value.ToLower() == "username" ? string.Empty : m.Groups[3].Value;
                        string clientName = m.Groups[4].Value;
                        string[] realName = m.Groups[5].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // 68 7 LT The Wheat Snooper

                        CountryClass country = CountriesClass.GetCountryByID(49);
                        int rank = 0;
                        bool clientGreatSnooper = false;

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

                            clientGreatSnooper = realName.Length >= 5 && realName[3] == "Great" && realName[4] == "Snooper";
                        }

                        GlobalManager.UITasks.Enqueue(new ClientUITask(channelName, clientName, country, clan, rank, clientGreatSnooper));
                    }
                    break;

                // The user is offline message
                case 401:
                    // :wormnet1.team17.com 401 Test sToOMiToO :No such nick/channel
                    spacePos2 = line.IndexOf(' ', spacePos);
                    if (spacePos2 != -1)
                    {
                        string clientName = line.Substring(spacePos, spacePos2 - spacePos);
                        GlobalManager.UITasks.Enqueue(new OfflineUITask(clientName));
                    }
                    break;
            }

            return stop;
        }
    }
}
