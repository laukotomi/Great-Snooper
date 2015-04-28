using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;



namespace MySnooper
{
    public delegate void ConnectionStateDelegate(object sender, ConnectionStateEventArgs e);

    public class IRCCommunicator
    {
        // States
        public enum ConnectionStates { Connected, UsernameInUse, Disconnected, Error, AuthOK, AuthBad }

        // Private IRC variables
        private readonly string serverAddress;
        private readonly int serverPort;
        private string serverIrcAddress = string.Empty;
        public readonly bool IsWormNet;
        public bool IsRunning { get; private set; }
        public Client User { get; private set; }

        // Buffers
        private readonly byte[] recvBuffer; // stores the bytes arrived from WormNet server. These bytes will be decoding into RecvMessage or into RecvHTML
        private readonly byte[] sendBuffer; // stores the encoded bytes from the items of the ToSend list which will be sent to WormNet server.
        private readonly StringBuilder recvMessage;
        private readonly SortedDictionary<string, string> channelListHelper = new SortedDictionary<string, string>();
        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        public readonly Dictionary<string, Client> Clients = new Dictionary<string, Client>();
        public readonly Dictionary<string, Channel> ChannelList = new Dictionary<string, Channel>(); // Is used only from UI thread!

        // Regular expressions
        // #Help 10 :05 A place to get help, or help others
        private readonly Regex channelRegex = new Regex(@"((#|&)\S+)[^:]+\S+\s(.*)");
        // #AnythingGoes ~UserName no.address.for.you wormnet1.team17.com Herbsman H :0 68 7 LT The Wheat Snooper 2.8
        // * ~ooo OutofOrder.user.gamesurge *.GameSurge.net OutofOrder Hx :3 Tomás Ticado
        // #worms ~tear tear.moe *.GameSurge.net Tear H :3 Tear
        private readonly Regex clientRegex = new Regex(@"(\S+)\s~?(\S+)\s\S+\s\S+\s(\S+)[^:]+\S+\s(.*)");
        // :sToOMiToO!~AeF@no.address.for.you JOIN :#RopersHeaven
        // :AeF`T!Username@no.address.for.you PRIVMSG #AnythingGoes :\x01ACTION ads\x01
        private readonly Regex messageRegex = new Regex(@":?([^!]+)!~?([^@]+)\S+\s(\S+)\s:?(.*)");
        private readonly Regex gsVersionRegex = new Regex(@"^v[1-9]\.[0-9]\.?[0-9]?$");

        // Variables to stop the thread and disconnect
        public volatile bool Cancel = false;

        // The socket
        private Socket ircServer;

        // The clock
        private Timer timer;
        private Timer reconnectTimer;
        private int reconnectCounter;

        // Variables for the infinite loop to work
        private readonly TimeSpan idleTimeout = new TimeSpan(0, 0, 30);
        private readonly TimeSpan disconnectTimeout = new TimeSpan(0, 0, 60);
        private DateTime lastServerAction;
        private bool pingSent = false;

        // This event reports connection state
        public event ConnectionStateDelegate ConnectionState;

        // Constructor
        public IRCCommunicator(string serverAddress, int serverPort, bool isWormNet = true)
        {
            this.serverAddress = serverAddress;
            this.serverPort = serverPort;
            this.IsWormNet = isWormNet;

            recvBuffer = new byte[10240]; // 10kB
            sendBuffer = new byte[1024]; // 1kB
            recvMessage = new StringBuilder(recvBuffer.Length);

            IsRunning = false;
        }

        public void Reconnect()
        {
            // Reset things
            IsRunning = true;
            Cancel = false;
            pingSent = false;
            reconnectCounter = 0;
            this.serverIrcAddress = string.Empty;

            string message;
            while (messages.TryDequeue(out message));

            recvMessage.Clear();
            this.Clients.Clear();

            reconnectTimer = new Timer(ReconnectNow, null, 500, Timeout.Infinite);
        }

        private void ReconnectNow(object state)
        {
            if (Cancel)
            {
                Stop(ConnectionStates.Disconnected);
                return;
            }

            reconnectCounter++;
            if (reconnectCounter == 30) // 15 seconds
                Connect();
            else
                reconnectTimer.Change(500, Timeout.Infinite);
        }


        public void Connect()
        {
            IsRunning = true;

            if (IsWormNet)
            {
                this.User = GlobalManager.User;
            }
            else
            {
                if (Properties.Settings.Default.WormsNick.Length > 0)
                    this.User = new Client(Properties.Settings.Default.WormsNick, GlobalManager.User.Clan);
                else
                    this.User = new Client(GlobalManager.User.Name, GlobalManager.User.Clan);
                this.User.Country = GlobalManager.User.Country;
                this.User.Rank = GlobalManager.User.Rank;
            }

            Task.Factory.StartNew(() =>
            {
                try
                {
                    // Connect to server
                    ircServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    ircServer.SendTimeout = 15000;
                    ircServer.ReceiveBufferSize = 10240;
                    ircServer.Connect(System.Net.Dns.GetHostAddresses(serverAddress), serverPort);

                    lastServerAction = DateTime.Now;

                    // Let's log in
                    SendLoginMessages();

                    // Start timer
                    timer = new System.Threading.Timer(timer_Elapsed, null, 500, Timeout.Infinite);
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                    Stop(ConnectionStates.Error);
                }
            });
        }

        private void TrySendQuitMessage()
        {
            ircServer.SendTimeout = 3000;
            if (Properties.Settings.Default.QuitMessagee.Length > 0)
                Send("QUIT :" + Properties.Settings.Default.QuitMessagee);
            else
                Send("QUIT");
            SendMessages();
        }

        private void timer_Elapsed(object state)
        {
            try
            {
                // The logic
                if (Cancel)
                {
                    TrySendQuitMessage();
                    Stop(ConnectionStates.Disconnected);
                    return;
                }

                DateTime now = DateTime.Now;

                if (pingSent && now - lastServerAction >= disconnectTimeout)
                {
                    TrySendQuitMessage();
                    Stop(ConnectionStates.Error);
                    return; // Connection lost
                }

                // RECEIVE data if there is any (without blocking the thread)
                if (ircServer.Available > 0)
                {
                    lastServerAction = now;
                    if (pingSent)
                        pingSent = false; // we consider server action as pong message

                    int bytes = ircServer.Receive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None); // Read the arrived datas into the buffer with a maximal length of the buffer (if the data is bigger than the buffer, it will be read in the next loop)
                    
                    if (IsWormNet)
                    {
                        for (int i = 0; i < bytes; i++)
                            recvMessage.Append(WormNetCharTable.Decode[recvBuffer[i]]); //Decode the bytes into RecvMessage
                    }
                    else
                        recvMessage.Append(Encoding.UTF8.GetString(recvBuffer, 0, bytes));

                    string[] lines = recvMessage.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                    // process the message line-by-line
                    for (int i = 0; i < lines.Length - 1; i++) // the last line is either string.Empty or a line which end isn't arrived yet
                    {
                        Debug.WriteLine("RECEIVED: " + this.serverAddress + " " + lines[i]);

                        // Get the sender of the message
                        int spacePos = lines[i].IndexOf(' ');
                        if (spacePos != -1)
                        {
                            string sender = lines[i].Substring(0, spacePos).ToLower();

                            // PING Message
                            if (sender == "ping")
                            {
                                Send("PONG " + lines[i].Substring(spacePos + 1));
                            }

                            // Closing link message after QUIT or ban ;)
                            else if (sender == "error")
                            {
                                // ERROR :Closing Link: Test[~Test@77.111.187.159] (Test)
                                System.Windows.MessageBox.Show("An error occurred: " + lines[i].Substring(spacePos + 2));
                            }

                            // If it is a server message
                            // :wormnet1.team17.com 322 Test #Help 10 :05 A place to get help, or help others
                            else if (sender == serverIrcAddress || serverIrcAddress == string.Empty)
                            {
                                if (ProcessServerMessage(lines[i], spacePos))
                                {
                                    Stop(ConnectionStates.UsernameInUse);
                                    return;
                                }
                            }

                            // Message
                            // :sToOMiToO!~AeF@no.address.for.you JOIN :#RopersHeaven
                            else if (ProcessClientMessage(lines[i]))
                            {
                                Stop(ConnectionStates.Error);
                                return;
                            }
                        }
                    }

                    // Clear processed data from the buffer
                    recvMessage.Clear();
                    if (lines[lines.Length - 1] != string.Empty)
                    {
                        recvMessage.Append(lines[lines.Length - 1]);
                    }
                }

                // If there was no server action for idleTimeout, then send ping message in every idleTimeout seconds
                if (now - lastServerAction >= idleTimeout && !pingSent)
                {
                    pingSent = true;
                    Send("PING " + serverAddress);
                }

                // SEND
                if (messages.Count > 0)
                {
                    SendMessages();
                }

                timer.Change(500, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
                Stop(ConnectionStates.Error);
            }
        }

        private void SendMessages()
        {
            string message;
            while (messages.TryDequeue(out message))
            {
                if (message.Length + 2 > sendBuffer.Length)
                    return;

                int i = 0;
                if (IsWormNet)
                {
                    for (; i < message.Length; i++)
                    {
                        sendBuffer[i] = WormNetCharTable.Encode[message[i]];
                    }
                    sendBuffer[i++] = WormNetCharTable.Encode['\r'];
                    sendBuffer[i++] = WormNetCharTable.Encode['\n'];
                }
                else
                {
                    try
                    {
                        i = Encoding.UTF8.GetBytes(message, 0, message.Length, sendBuffer, 0);
                        i += Encoding.UTF8.GetBytes("\r\n", 0, 2, sendBuffer, i);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                if (message == "LIST")
                    channelListHelper.Clear();

                Debug.WriteLine("SENDING: " + this.serverAddress + " " + message);
                ircServer.Send(sendBuffer, 0, i, SocketFlags.None);

                if (message.Length >= 4 && message.Substring(0, 4) == "QUIT")
                {
                    Stop(ConnectionStates.Disconnected);
                    return;
                }
            }
        }

        private void Stop(ConnectionStates state)
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }

            if (reconnectTimer != null)
            {
                reconnectTimer.Dispose();
                reconnectTimer = null;
            }

            if (ircServer != null)
            {
                ircServer.Dispose();
                ircServer = null;
            }

            IsRunning = false;

            if (ConnectionState != null)
            {
                ConnectionState.BeginInvoke(this, new ConnectionStateEventArgs(state), null, null);
            }
        }

        private void SendLoginMessages()
        {
            // USER Username hostname servername :41 0 RU StepS
            int countryID = this.User.Country.ID;
            if (countryID > 52)
                countryID = 49;

            string nickClan = this.User.Clan;
            if (nickClan.Length == 0)
                nickClan = "Username";

            if (this.IsWormNet)
                Send("PASS ELSILRACLIHP"); // Password
            Send("NICK " + this.User.Name); // Nick
            Send("USER " + nickClan + " hostname servername :" + countryID.ToString() + " " + this.User.Rank.ID.ToString() + " " + this.User.Country.CountryCode + " Great Snooper v" + App.GetVersion()); // USER message
        }

        private bool ProcessClientMessage(string line)
        {
            // :Test!~Test@no.address.for.you PART #AnythingGoes
            Match m = messageRegex.Match(line);
            if (!m.Success)
                return false;

            string command = m.Groups[3].Value.ToLower();

            // :sToOMiToO2!~AeF@no.address.for.you PART #AnythingGoes
            if (command == "part")
            {
                string clientNameL = m.Groups[1].Value.ToLower();
                string channelHash = m.Groups[4].Value.ToLower();
                GlobalManager.UITasks.Enqueue(new PartedUITask(this, channelHash, clientNameL));
            }

            // :sToOMiToO!~AeF@no.address.for.you JOIN :#RopersHeaven
            else if (command == "join")
            {
                string clientName = m.Groups[1].Value;
                string channelHash = m.Groups[4].Value.ToLower();
                string clan = m.Groups[2].Value.ToLower() == "username" ? string.Empty : m.Groups[2].Value;
                GlobalManager.UITasks.Enqueue(new JoinedUITask(this, channelHash, clientName, clan));
            }

            // :Snaker!Username@no.address.for.you QUIT :Joined Game
            else if (command == "quit")
            {
                string clientNameL = m.Groups[1].Value.ToLower();
                string message = m.Groups[4].Value;
                if (clientNameL == this.User.LowerName)
                {
                    System.Windows.MessageBox.Show("Server disconnected: " + message);
                    return true;
                }

                GlobalManager.UITasks.Enqueue(new QuitUITask(this, clientNameL, message));
            }

            // :Don-Coyote!Username@no.address.for.you PRIVMSG #AnythingGoes :can u take my oral
            // :AeF`T!Username@no.address.for.you PRIVMSG #AnythingGoes :\x01ACTION ads\x01
            else if ((command == "privmsg" || command == "notice"))
            {
                string clientName = m.Groups[1].Value;
                string clientLower = clientName.ToLower();
                if (!IsWormNet && clientLower == "global")
                    return false;

                int spacePos = m.Groups[4].Value.IndexOf(' ');
                if (spacePos != -1)
                {
                    string channelHash = m.Groups[4].Value.Substring(0, spacePos).ToLower();
                    if (channelHash == this.User.LowerName)
                        channelHash = clientName; // Hashname for pm channels are case-sensitive!
                    string message = m.Groups[4].Value.Substring(spacePos + 2);

                    /*
                    if (!IsWormNet && lower == "authserv" && ConnectionState != null)
                    {
                        if (message == "I recognize you.")
                        {
                            ConnectionState(this, ConnectionStates.AuthOK);
                            return false;
                        }
                        else
                            ConnectionState(this, ConnectionStates.AuthBad);
                    }
                    */

                    // Is it an action message? (CTCP message) (\x01ACTION message..\x01)
                    if (message.Length > 2 && message[0] == '\x01' && message[message.Length - 1] == '\x01')
                    {
                        spacePos = message.IndexOf(' ');
                        if (spacePos != -1) // ctcp command with message
                        {
                            string ctcpCommand = message.Substring(1, spacePos - 1).ToLower(); // skip \x01
                            message = message.Substring(spacePos + 1, message.Length - spacePos - 2); // skip \x01
                            int vertBarPos;
                            switch (ctcpCommand)
                            {
                                case "action":
                                    GlobalManager.UITasks.Enqueue(new MessageUITask(this, clientName, channelHash, message, MessageSettings.ActionMessage));
                                    break;

                                case "cmessage":
                                    vertBarPos = message.IndexOf('|');
                                    if (vertBarPos != -1)
                                    {
                                        channelHash = SplitUserAndSenderName(message.Substring(0, vertBarPos), clientName);
                                        string msg = message.Substring(vertBarPos + 1);
                                        GlobalManager.UITasks.Enqueue(new MessageUITask(this, clientName, channelHash, msg, MessageSettings.ChannelMessage));
                                    }
                                    break;

                                case "caction":
                                    vertBarPos = message.IndexOf('|');
                                    if (vertBarPos != -1)
                                    {
                                        channelHash = SplitUserAndSenderName(message.Substring(0, vertBarPos), clientName);
                                        string msg = message.Substring(vertBarPos + 1);
                                        GlobalManager.UITasks.Enqueue(new MessageUITask(this, clientName, channelHash, msg, MessageSettings.ActionMessage));
                                    }
                                    break;

                                case "clientadd":
                                    vertBarPos = message.IndexOf('|');
                                    if (vertBarPos != -1)
                                    {
                                        channelHash = SplitUserAndSenderName(message.Substring(0, vertBarPos), clientName);
                                        string clientNameToAdd = message.Substring(vertBarPos + 1);
                                        GlobalManager.UITasks.Enqueue(new ClientAddOrRemoveTask(this, channelHash, clientName, clientNameToAdd, ClientAddOrRemoveTask.TaskType.Add));
                                    }
                                    break;

                                case "clientrem":
                                    vertBarPos = message.IndexOf('|');
                                    if (vertBarPos != -1)
                                    {
                                        channelHash = SplitUserAndSenderName(message.Substring(0, vertBarPos), clientName);
                                        string clientNameToRemove = message.Substring(vertBarPos + 1);
                                        GlobalManager.UITasks.Enqueue(new ClientAddOrRemoveTask(this, channelHash, clientName, clientNameToRemove, ClientAddOrRemoveTask.TaskType.Remove));
                                    }
                                    break;

                                case "cleaving":
                                    channelHash = SplitUserAndSenderName(message, clientName);
                                    GlobalManager.UITasks.Enqueue(new ClientLeaveConvTask(this, channelHash, clientName));
                                    break;
                            }
                        }
                        else
                        {
                            string ctcpCommand = message.Substring(1, message.Length - 2).ToLower();
                            switch (ctcpCommand)
                            {
                                case "version":
                                    Send("NOTICE " + clientName + " :VERSION Great Snooper v" + App.GetVersion());
                                    break;
                            }
                        }
                    }
                    else
                    {
                        MessageSetting setting = (command == "privmsg") ? MessageSettings.ChannelMessage : MessageSettings.NoticeMessage;
                        GlobalManager.UITasks.Enqueue(new MessageUITask(this, clientName, channelHash, message, setting));
                    }
                }
            }
            // :Tomi!~Tomi@h187-159.pool77-111.dyn.tolna.net NICK :Tomi3
            else if (!IsWormNet && command == "nick")
            {
                string oldClientName = m.Groups[1].Value;
                string newClientName = m.Groups[4].Value;
                GlobalManager.UITasks.Enqueue(new NickUITask(this, oldClientName, newClientName));
            }

            return false;
        }

        private string SplitUserAndSenderName(string channelHash, string clientName)
        {
            string[] helper = channelHash.Split(new char[] { ',' });
            for (int i = 0; i < helper.Length; i++)
            {
                if (helper[i] == this.User.Name)
                {
                    helper[i] = clientName;
                    break;
                }
            }
            Array.Sort(helper);
            return String.Join(",", helper);
        }

        private bool ProcessServerMessage(string line, int spacePos)
        {
            // Get the number out of the message
            // :wormnet1.team17.com 322 Test #Help 10 :05 A place to get help, or help others
            int number;
            int spacePos2 = line.IndexOf(' ', spacePos + 1);
            if (spacePos2 == -1 || !int.TryParse(line.Substring(spacePos + 1, spacePos2 - spacePos - 1), out number))
                return false;

            // Find the space after our nickname in the message
            spacePos = line.IndexOf(' ', spacePos2 + 1);
            if (spacePos == -1)
                return false;
            spacePos++; // we will start all the regex match from this position

            // Process the message
            switch (number)
            {
                // Connection success
                case 4:
                    spacePos2 = line.IndexOf(' ', spacePos);
                    if (spacePos2 != -1)
                    {
                        this.serverIrcAddress = ':' + line.Substring(spacePos, spacePos2 - spacePos).ToLower();

                        //if (!this.IsWormNet)
                        //    Send("authserv auth " + this.User.Name + " " + Properties.Settings.Default.WormsPassword);

                        if (ConnectionState != null)
                            ConnectionState.BeginInvoke(this, new ConnectionStateEventArgs(ConnectionStates.Connected), null, null);
                    }
                    break;

                // This nickname is already in use!
                case 433:
                    if (serverIrcAddress == string.Empty)
                        return true;
                    else
                    {
                        // nickname is in use when we tried to change with /NICK command
                        GlobalManager.UITasks.Enqueue(new NickNameInUseTask(this));
                    }
                    break;

                // A channel (answer for the LIST command)
                case 322:
                    // :wormnet1.team17.com 322 Test #Help 10 :05 A place to get help, or help others
                    Match chMatch = channelRegex.Match(line, spacePos);
                    if (chMatch.Success)
                    {
                        string channelName = chMatch.Groups[1].Value;
                        string description = chMatch.Groups[3].Value;

                        if (!channelListHelper.ContainsKey(channelName))
                        {
                            channelListHelper.Add(channelName, description);
                        }
                    }
                    break;

                // LIST END
                case 323:
                    GlobalManager.UITasks.Enqueue(new ChannelListUITask(this, channelListHelper));
                    break;

                // A client (answer for WHO command)
                case 352:
                    // :wormnet1.team17.com 352 Test #AnythingGoes ~UserName no.address.for.you wormnet1.team17.com Herbsman H :0 68 7 LT The Wheat Snooper 2.8
                    Match clMatch = clientRegex.Match(line, spacePos);
                    if (clMatch.Success)
                    {
                        string channelHash = clMatch.Groups[1].Value.ToLower();
                        string clan = clMatch.Groups[2].Value.ToLower() == "username" ? string.Empty : clMatch.Groups[2].Value;
                        string clientName = clMatch.Groups[3].Value;
                        string[] realName = clMatch.Groups[4].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // 68 7 LT The Wheat Snooper

                        CountryClass country = CountriesClass.DefaultCountry;
                        int rank = 0;
                        bool clientGreatSnooper = false;
                        string clientApp = clMatch.Groups[4].Value;

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

                            clientGreatSnooper = realName.Length >= 6 && realName[3] == "Great" && realName[4] == "Snooper" && gsVersionRegex.IsMatch(realName[5]);
                            StringBuilder sb = new StringBuilder();
                            for (int i = 3; i < realName.Length; i++)
                            {
                                sb.Append(realName[i]);
                                if (i + 1 < realName.Length)
                                    sb.Append(" ");
                            }
                            clientApp = sb.ToString();
                        }

                        GlobalManager.UITasks.Enqueue(new ClientUITask(this, channelHash, clientName, country, clan, rank, clientGreatSnooper, clientApp));
                    }
                    break;

                // The user is offline message
                case 401:
                    // :wormnet1.team17.com 401 Test sToOMiToO :No such nick/channel
                    spacePos2 = line.IndexOf(' ', spacePos);
                    if (spacePos2 != -1)
                    {
                        string clientName = line.Substring(spacePos, spacePos2 - spacePos);
                        GlobalManager.UITasks.Enqueue(new OfflineUITask(this, clientName));
                    }
                    break;
            }

            return false;
        }


        // Send a message to WormNet
        public void SendMessage(string channelOrClientName, string message)
        {
            Send("PRIVMSG " + channelOrClientName + " :" + message);
        }

        public void SendCTCPMessage(string channelOrClientName, string command, string message = "")
        {
            if (message.Length == 0)
                Send("PRIVMSG " + channelOrClientName + " :\x01" + command + "\x01");
            else
                Send("PRIVMSG " + channelOrClientName + " :\x01" + command + " " + message + "\x01");
        }

        private void Send(string message)
        {
            messages.Enqueue(message);
        }

        public void JoinChannel(string channelName)
        {
            Send("JOIN " + channelName);
        }

        public void NickChange(string newNick)
        {
            Send("NICK " + newNick);
        }

        public void LeaveChannel(string channelName)
        {
            Send("PART " + channelName);
        }

        public void GetChannelList()
        {
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

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            IRCCommunicator irc = obj as IRCCommunicator;
            if ((System.Object)irc == null)
            {
                return false;
            }

            // Return true if the fields match:
            return this.serverAddress == irc.serverAddress;
        }

        public bool Equals(IRCCommunicator irc)
        {
            // If parameter is null return false:
            if ((object)irc == null)
            {
                return false;
            }

            // Return true if the fields match:
            return this.serverAddress == irc.serverAddress;
        }

        public override int GetHashCode()
        {
            return this.serverAddress.GetHashCode();
        }

        public static bool operator ==(IRCCommunicator a, IRCCommunicator b)
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
            return a.serverAddress == b.serverAddress;
        }

        public static bool operator !=(IRCCommunicator a, IRCCommunicator b)
        {
            return !(a == b);
        }
    }
}
