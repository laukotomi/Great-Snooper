using GalaSoft.MvvmLight;
using GreatSnooper.Helpers;
using GreatSnooper.IRCTasks;
using GreatSnooper.Model;
using GreatSnooper.ViewModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GreatSnooper.Classes
{
    public delegate void ConnectionStateDelegate(object sender, AbstractCommunicator.ConnectionStates oldState);

    public abstract class AbstractCommunicator : ObservableObject, IDisposable
    {
        #region Enums
        public enum ConnectionStates { Disconnected, Connecting, Connected, Disconnecting, ReConnecting }
        public enum ErrorStates { None, UsernameInUse, Error, /*,AuthOK, AuthBad*/TimeOut }
        #endregion

        #region Members
        private volatile ConnectionStates _connectionState = ConnectionStates.Disconnected;
        protected readonly int serverPort;
        protected string serverIrcAddress = string.Empty;

        protected readonly byte[] recvBuffer = new byte[10240]; // stores the bytes arrived from WormNet server. These bytes will be decoding into RecvMessage or into RecvHTML
        protected readonly byte[] sendBuffer = new byte[1024]; // stores the encoded bytes from the items of the ToSend list which will be sent to WormNet server.
        protected readonly StringBuilder recvMessage = new StringBuilder(10240);
        protected readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        protected SortedDictionary<string, string> channelListHelper;

        // #Help 10 :05 A place to get help, or help others
        protected readonly Regex channelRegex = new Regex(@"((#|&)\S+)[^:]+\S+\s(.*)", RegexOptions.Compiled);
        // #AnythingGoes ~UserName no.address.for.you wormnet1.team17.com Herbsman H :0 68 7 LT The Wheat Snooper 2.8
        // * ~ooo OutofOrder.user.gamesurge *.GameSurge.net OutofOrder Hx :3 Tomás Ticado
        // #worms ~tear tear.moe *.GameSurge.net Tear H :3 Tear
        protected readonly Regex clientRegex = new Regex(@"(\S+)\s~?(\S+)\s\S+\s\S+\s(\S+)[^:]+\S+\s(.*)", RegexOptions.Compiled);
        // :sToOMiToO!~AeF@no.address.for.you JOIN :#RopersHeaven
        // :AeF`T!Username@no.address.for.you PRIVMSG #AnythingGoes :\x01ACTION ads\x01
        protected readonly Regex messageRegex = new Regex(@":?([^!]+)!~?([^@]+)\S+\s(\S+)\s?:?(.*)", RegexOptions.Compiled);
        protected readonly Regex namesRegex = new Regex(@"^(=|\*|@) ([^ ]+) :(.*)", RegexOptions.Compiled);

        // Communication things
        protected Socket ircServer;
        protected Task connectionTask;
        protected Timer timer;
        protected readonly TimeSpan idleTimeout = new TimeSpan(0, 0, 60);
        protected readonly TimeSpan disconnectTimeout = new TimeSpan(0, 0, 120);
        protected DateTime lastServerAction;
        protected bool pingSent = false;

        // Reconnet things
        protected Timer reconnectTimer;
        protected DateTime lastReconnectAttempt;
        protected readonly TimeSpan reconnectTimeout = new TimeSpan(0, 0, 30);

        private bool handleGlobalMessage;
        #endregion

        #region Properties
        public ConnectionStates State
        {
            get { return _connectionState; }
            protected set
            {
                if (_connectionState != value)
                {
                    var oldValue = _connectionState;
                    _connectionState = value;
                    if (ConnectionState != null)
                        ConnectionState.BeginInvoke(this, oldValue, null, null);
                }
            }
        }
        public ErrorStates ErrorState { get; protected set; }
        public string ServerAddress { get; private set; }
        public User User { get; protected set; }
        public bool HandleNickChange { get; private set; }
        public bool HandleJoinRequest { get; private set; }
        public MainViewModel MVM { get; set; }
        public Dictionary<string, User> Users { get; private set; } // Is used only from UI thread!
        public Dictionary<string, AbstractChannelViewModel> Channels { get; private set; } // Is used only from UI thread!
        #endregion

        #region Events
        public event ConnectionStateDelegate ConnectionState;
        #endregion

        #region Abstract methods
        protected abstract void SetUser();
        protected abstract void EncodeMessage(int bytes);
        protected abstract int DecodeMessage(string message);
        protected virtual void SendPassword() { }
        public abstract string VerifyString(string str);
        #endregion

        protected AbstractCommunicator(string serverAddress, int serverPort, bool handleGlobalMessage, bool handleNickChange, bool handleJoinRequest)
        {
            this.ErrorState = ErrorStates.None;
            this.ServerAddress = serverAddress;
            this.serverPort = serverPort;
            this.handleGlobalMessage = handleGlobalMessage;
            this.HandleNickChange = handleNickChange;
            this.HandleJoinRequest = handleJoinRequest;
            this.Users = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
            this.Channels = new Dictionary<string, AbstractChannelViewModel>(StringComparer.OrdinalIgnoreCase);
            this.lastReconnectAttempt = new DateTime(1999, 5, 31);
        }

        public void CancelAsync()
        {
            if (this.State != ConnectionStates.Disconnected)
                this.State = ConnectionStates.Disconnecting;
        }

        public void Reconnect()
        {
            if (this.State != ConnectionStates.Disconnected)
            {
                Stop(ErrorStates.None);
                return;
            }

            try
            {
                // Reset things
                this.State = ConnectionStates.ReConnecting;

                if (reconnectTimer == null)
                    reconnectTimer = new Timer(ReconnectNow, null, 500, Timeout.Infinite);
                else
                    reconnectTimer.Change(500, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
                Stop(ErrorStates.Error);
            }
        }

        private void ReconnectNow(object state)
        {
            try
            {
                if (this.State != ConnectionStates.ReConnecting)
                {
                    Stop(ErrorStates.None);
                    return;
                }

                if (DateTime.Now - lastReconnectAttempt > reconnectTimeout)
                    Connect();
                else
                    reconnectTimer.Change(500, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
                Stop(ErrorStates.Error);
            }
        }

        public void Connect()
        {
            if (this.State != ConnectionStates.Disconnected && this.State != ConnectionStates.ReConnecting)
            {
                Stop(ErrorStates.None);
                return;
            }
            if (this.State == ConnectionStates.Disconnected)
                this.State = ConnectionStates.Connecting;

            this.lastReconnectAttempt = DateTime.Now;
            this.ErrorState = ErrorStates.None;
            this.SetUser();

            if (connectionTask != null)
            {
                connectionTask.Dispose();
                connectionTask = null;
            }

            connectionTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    if (ircServer != null)
                    {
                        ircServer.Dispose();
                        ircServer = null;
                    }

                    // Reset things
                    lastServerAction = DateTime.Now;
                    pingSent = false;
                    this.serverIrcAddress = string.Empty;

                    string message;
                    while (messages.TryDequeue(out message)) ;
                    recvMessage.Clear();

                    // Connect to server
                    ircServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    ircServer.SendTimeout = 60000;
                    ircServer.ReceiveBufferSize = 10240;
                    Debug.WriteLine("Trying to connect " + this.ServerAddress + ":" + serverPort.ToString());
                    ircServer.Connect(System.Net.Dns.GetHostAddresses(ServerAddress), serverPort);

                    // Let's log in
                    SendLoginMessages();

                    // Start timer
                    if (timer == null)
                        timer = new Timer(timer_Elapsed, null, 1000, Timeout.Infinite);
                    else
                        timer.Change(1000, Timeout.Infinite);
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                    Stop(ErrorStates.Error);
                }
            });
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

            this.SendPassword();
            Send(this, "NICK " + this.User.Name); // Nick
            Send(this, "USER " + nickClan + " hostname servername :" + countryID.ToString() + " " + this.User.Rank.ID.ToString() + " " + this.User.Country.CountryCode + " Great Snooper v" + App.GetVersion()); // USER message
        }

        private void Stop(ErrorStates state)
        {
            this.ErrorState = state;
            this.State = ConnectionStates.Disconnected;
        }

        private void TrySendQuitMessage()
        {
            ircServer.SendTimeout = 5000;
            if (Properties.Settings.Default.QuitMessagee.Length > 0)
                Send(this, "QUIT :" + Properties.Settings.Default.QuitMessagee);
            else
                Send(this, "QUIT");
            SendMessages();
        }

        private void timer_Elapsed(object state)
        {
            if (this.State == ConnectionStates.Disconnected)
            {
                Stop(ErrorStates.None);
                return;
            }

            try
            {
                // The logic
                if (this.State == ConnectionStates.Disconnecting)
                {
                    TrySendQuitMessage();
                    Stop(ErrorStates.None);
                    return;
                }

                DateTime now = DateTime.Now;

                if (now - lastServerAction >= disconnectTimeout)
                {
                    TrySendQuitMessage();
                    Stop(ErrorStates.TimeOut);
                    return; // Connection lost
                }

                // RECEIVE data if there are any
                if (ircServer.Available > 0)
                {
                    lastServerAction = now;
                    if (pingSent)
                        pingSent = false; // we consider server action as pong message

                    int bytes = ircServer.Receive(recvBuffer); // Read the arrived datas into the buffer with a maximal length of the buffer (if the data is bigger than the buffer, it will be read in the next loop)
                    this.EncodeMessage(bytes); // Encodes recvBuffer into recvMesssage
                    string[] lines = recvMessage.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                    // process the message line-by-line
                    for (int i = 0; i < lines.Length - 1; i++) // the last line is either string.Empty or a line which end hasn't arrived yet
                    {
                        Debug.WriteLine("RECEIVED: " + this.ServerAddress + " " + lines[i]);

                        // Get the sender of the message
                        int spacePos = lines[i].IndexOf(' ');
                        if (spacePos != -1)
                        {
                            string sender = lines[i].Substring(0, spacePos);

                            // PING Message
                            if (sender.Equals("PING", StringComparison.OrdinalIgnoreCase))
                            {
                                Send(this, "PONG " + lines[i].Substring(spacePos + 1));
                            }

                            // Closing link message after QUIT or ban ;)
                            else if (sender.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
                            {
                                if (MVM != null)
                                {
                                    MVM.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        MVM.DialogService.ShowDialog(
                                            Localizations.GSLocalization.Instance.ErrorText,
                                            string.Format(Localizations.GSLocalization.Instance.ServerErrorMessage, lines[i].Substring(spacePos + 2))
                                        );
                                    }));
                                }
                                // ERROR :Closing Link: Test[~Test@ip-addr] (Test)
                                TrySendQuitMessage();
                                Stop(ErrorStates.Error);
                                return;
                            }

                            // If it is a server message
                            // :wormnet1.team17.com 322 Test #Help 10 :05 A place to get help, or help others
                            else if (sender.Equals(serverIrcAddress, StringComparison.OrdinalIgnoreCase) || serverIrcAddress == string.Empty)
                            {
                                if (ProcessServerMessage(lines[i], spacePos))
                                {
                                    Stop(ErrorStates.UsernameInUse);
                                    return;
                                }
                            }

                            // Message
                            // :sToOMiToO!~AeF@no.address.for.you JOIN :#RopersHeaven
                            else if (ProcessClientMessage(lines[i]))
                            {
                                Stop(ErrorStates.Error);
                                return;
                            }
                        }
                    }

                    // Clear processed data from the buffer
                    recvMessage.Clear();
                    if (lines.Length > 0 && lines[lines.Length - 1] != string.Empty)
                        recvMessage.Append(lines[lines.Length - 1]);
                }

                // If there was no server action for idleTimeout, then send ping message in every idleTimeout seconds
                if (!pingSent && now - lastServerAction >= idleTimeout && serverIrcAddress != string.Empty)
                {
                    pingSent = true;
                    Send(this, "PING " + serverIrcAddress);
                }

                // SEND
                if (messages.Count > 0)
                    SendMessages();

                timer.Change(1000, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
                Stop(ErrorStates.Error);
            }
        }

        private void SendMessages()
        {
            string message;
            while (messages.TryDequeue(out message))
            {
                if (message.Length + 2 > sendBuffer.Length)
                    return;

                int i = this.DecodeMessage(message);
                if (i == -1)
                    continue;

                if (message == "cancel")
                {
                    this.CancelAsync();
                    return;
                }

                Debug.WriteLine("SENDING: " + this.ServerAddress + " " + message);
                ircServer.Send(sendBuffer, 0, i, SocketFlags.None);
            }
        }

        private bool ProcessClientMessage(string line)
        {
            // :Test!~Test@no.address.for.you PART #AnythingGoes
            Match m = messageRegex.Match(line);
            if (!m.Success)
                return false;

            string command = m.Groups[3].Value;

            // :sToOMiToO2!~AeF@no.address.for.you PART #AnythingGoes
            // PART <channel> *( "," <channel> ) [ <Part Message> ]
            if (command.Equals("PART", StringComparison.OrdinalIgnoreCase))
            {
                string clientName = m.Groups[1].Value;
                var param = m.Groups[4].Value;
                int spacePos = param.IndexOf(' ');
                string channelHash = (spacePos != -1) ? param.Substring(0, spacePos) : param;
                string message = string.Empty;
                if (spacePos != -1 && param.Length > spacePos + 1)
                    message = (param[spacePos + 1] == ':') ? param.Substring(spacePos + 2) : param.Substring(spacePos + 1);
                if (MVM != null)
                {
                    MVM.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        new PartedTask(this, channelHash, clientName, message).DoTask(MVM);
                    }));
                }
            }

            // :sToOMiToO!~AeF@no.address.for.you JOIN :#RopersHeaven
            // JOIN ( <channel> *( "," <channel> ) [ <key> *( "," <key> ) ] ) / "0"
            else if (command.Equals("JOIN", StringComparison.OrdinalIgnoreCase))
            {
                string clientName = m.Groups[1].Value;
                var param = m.Groups[4].Value;
                int spacePos = param.IndexOf(' ');
                string channelHash = (spacePos != -1) ? param.Substring(0, spacePos) : param;
                string clan = m.Groups[2].Value.Equals("Username", StringComparison.OrdinalIgnoreCase) ? string.Empty : m.Groups[2].Value;
                if (MVM != null)
                {
                    MVM.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        new JoinedTask(this, channelHash, clientName, clan).DoTask(MVM);
                    }));
                }
            }

            // :Snaker!Username@no.address.for.you QUIT :Joined Game
            else if (command.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                string clientName = m.Groups[1].Value;
                string message = m.Groups[4].Value;
                if (clientName.Equals(this.User.Name, StringComparison.OrdinalIgnoreCase))
                {
                    System.Windows.MessageBox.Show(string.Format(Localizations.GSLocalization.Instance.ServerQuitMessage, message));
                    return true;
                }
                if (MVM != null)
                {
                    MVM.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        new QuitTask(this, clientName, message).DoTask(MVM);
                    }));
                }
            }

            // :Don-Coyote!Username@no.address.for.you PRIVMSG #AnythingGoes :can u take my oral
            // :AeF`T!Username@no.address.for.you PRIVMSG #AnythingGoes :\x01ACTION ads\x01
            else if (command.Equals("PRIVMSG", StringComparison.OrdinalIgnoreCase) || command.Equals("NOTICE", StringComparison.OrdinalIgnoreCase))
            {
                string clientName = m.Groups[1].Value;
                if (clientName.Equals("Global", StringComparison.OrdinalIgnoreCase) && this.handleGlobalMessage == false)
                    return false;

                string param = m.Groups[4].Value;
                int spacePos = param.IndexOf(' ');
                if (spacePos != -1)
                {
                    string channelHash = param.Substring(0, spacePos);
                    if (channelHash.Equals(this.User.Name, StringComparison.OrdinalIgnoreCase))
                        channelHash = clientName; // if private message then the channel will be the channel with name of the sender of this message
                    string message = (param.Length > spacePos + 1 && param[spacePos + 1] == ':')
                        ? param.Substring(spacePos + 2)
                        : param.Substring(spacePos + 1); // this will never happen in theory

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
                            string ctcpCommand = message.Substring(1, spacePos - 1); // skip \x01
                            message = message.Substring(spacePos + 1, message.Length - spacePos - 2); // skip \x01

                            if (ctcpCommand.Equals("ACTION", StringComparison.OrdinalIgnoreCase))
                            {
                                if (MVM != null)
                                {
                                    MVM.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        new MessageTask(this, clientName, channelHash, message, MessageSettings.ActionMessage).DoTask(MVM);
                                    }));
                                }
                            }
                            else if (ctcpCommand.Equals("AWAY", StringComparison.OrdinalIgnoreCase))
                            {
                                if (MVM != null)
                                {
                                    MVM.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        new MessageTask(this, clientName, channelHash, string.Format(Localizations.GSLocalization.Instance.AwayMessageFormat, message), MessageSettings.ChannelMessage).DoTask(MVM);
                                    }));
                                }
                            }
                            else if (ctcpCommand.Equals("CMESSAGE", StringComparison.OrdinalIgnoreCase) || ctcpCommand.Equals("CNOTICE", StringComparison.OrdinalIgnoreCase))
                            {
                                int vertBarPos = message.IndexOf('|');
                                if (vertBarPos != -1)
                                {
                                    channelHash = SplitUserAndSenderName(message.Substring(0, vertBarPos), clientName);
                                    string msg = message.Substring(vertBarPos + 1);
                                    if (MVM != null)
                                    {
                                        MVM.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            MessageSetting setting = (ctcpCommand.Equals("CMESSAGE", StringComparison.OrdinalIgnoreCase)) ? MessageSettings.ChannelMessage : MessageSettings.NoticeMessage;
                                            new MessageTask(this, clientName, channelHash, msg, setting).DoTask(MVM);
                                        }));
                                    }
                                }
                            }
                            else if (ctcpCommand.Equals("CACTION", StringComparison.OrdinalIgnoreCase))
                            {
                                int vertBarPos = message.IndexOf('|');
                                if (vertBarPos != -1)
                                {
                                    channelHash = SplitUserAndSenderName(message.Substring(0, vertBarPos), clientName);
                                    string msg = message.Substring(vertBarPos + 1);
                                    if (MVM != null)
                                    {
                                        MVM.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            new MessageTask(this, clientName, channelHash, msg, MessageSettings.ActionMessage).DoTask(MVM);
                                        }));
                                    }
                                }
                            }
                            else if (ctcpCommand.Equals("CLIENTADD", StringComparison.OrdinalIgnoreCase))
                            {
                                int vertBarPos = message.IndexOf('|');
                                if (vertBarPos != -1)
                                {
                                    channelHash = SplitUserAndSenderName(message.Substring(0, vertBarPos), clientName);
                                    string clientNameToAdd = message.Substring(vertBarPos + 1);
                                    if (MVM != null)
                                    {
                                        MVM.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            new ClientAddTask(this, channelHash, clientName, clientNameToAdd).DoTask(MVM);
                                        }));
                                    }
                                }
                            }
                            else if (ctcpCommand.Equals("CLIENTREM", StringComparison.OrdinalIgnoreCase))
                            {
                                int vertBarPos = message.IndexOf('|');
                                if (vertBarPos != -1)
                                {
                                    channelHash = SplitUserAndSenderName(message.Substring(0, vertBarPos), clientName);
                                    string clientNameToRemove = message.Substring(vertBarPos + 1);
                                    if (MVM != null)
                                    {
                                        MVM.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            new ClientRemoveTask(this, channelHash, clientName, clientNameToRemove).DoTask(MVM);
                                        }));
                                    }
                                }
                            }
                            else if (ctcpCommand.Equals("CLEAVING", StringComparison.OrdinalIgnoreCase))
                            {
                                channelHash = SplitUserAndSenderName(message, clientName);
                                if (MVM != null)
                                {
                                    MVM.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        new ClientLeftTask(this, channelHash, clientName).DoTask(MVM);
                                    }));
                                }
                            }
                        }
                        else // ctcp command without message
                        {
                            string ctcpCommand = message.Substring(1, message.Length - 2).ToLower(); // remove \x01s
                            if (ctcpCommand.Equals("VERSION", StringComparison.OrdinalIgnoreCase) && command.Equals("PRIVMSG", StringComparison.OrdinalIgnoreCase)) // No asnwer for notice
                            {
                                Send(this, "NOTICE " + clientName + " :VERSION Great Snooper v" + App.GetVersion());
                            }
                        }
                    }
                    else // simple message, not ctcp
                    {
                        if (MVM != null)
                        {
                            MVM.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                MessageSetting setting = (command.Equals("PRIVMSG", StringComparison.OrdinalIgnoreCase)) ? MessageSettings.ChannelMessage : MessageSettings.NoticeMessage;
                                new MessageTask(this, clientName, channelHash, message, setting).DoTask(MVM);
                            }));
                        }
                    }
                }
            }
            // :Tomi!~Tomi@irc.org NICK :Tomi3
            else if (command.Equals("NICK", StringComparison.OrdinalIgnoreCase) && HandleNickChange)
            {
                string oldClientName = m.Groups[1].Value;
                string newClientName = m.Groups[4].Value;
                MVM.Dispatcher.BeginInvoke(new Action(() =>
                {
                    new NickChangeTask(this, oldClientName, newClientName).DoTask(MVM);
                }));
            }
            // :Angel!wings@irc.org INVITE Wiz #Dust
            else if (command.Equals("INVITE", StringComparison.OrdinalIgnoreCase))
            {
                string[] data = m.Groups[4].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (data.Length == 2 && data[0].Equals(this.User.Name, StringComparison.OrdinalIgnoreCase))
                {
                    MVM.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        new JoinedTask(this, data[1], this.User.Name, this.User.Clan).DoTask(MVM);
                    }));
                }
            }
            // :WiZ!jto@tolsun.oulu.fi KICK #Finnish John
            else if (command.Equals("KICK", StringComparison.OrdinalIgnoreCase))
            {
                string[] data = m.Groups[4].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (data.Length == 2)
                {
                    string clientName = m.Groups[1].Value;
                    MVM.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        new PartedTask(this, data[0], data[1], string.Format(Localizations.GSLocalization.Instance.KickMessage, clientName));
                    }));
                }
            }
            // :WiZ!jto@tolsun.oulu.fi TOPIC #test :New topic
            else if (command.Equals("TOPIC", StringComparison.OrdinalIgnoreCase))
            {
                string[] data = m.Groups[4].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (data.Length == 2)
                {
                    string clientName = m.Groups[1].Value;
                    string topic = (data[1][0] == ':') ? data[1].Substring(1) : data[1];
                    MVM.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        new MessageTask(this, GlobalManager.SystemUser.Name, data[0], string.Format(Localizations.GSLocalization.Instance.TopicMessage, clientName, topic), MessageSettings.SystemMessage);
                    }));
                }
            }

            return false;
        }

        private string SplitUserAndSenderName(string channelHash, string clientName)
        {
            string[] helper = channelHash.Split(new char[] { ',' });
            for (int i = 0; i < helper.Length; i++)
            {
                if (helper[i].Equals(this.User.Name, StringComparison.OrdinalIgnoreCase))
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
                        this.serverIrcAddress = ':' + line.Substring(spacePos, spacePos2 - spacePos);

                        //if (!this.IsWormNet)
                        //    Send("authserv auth " + this.User.Name + " " + Properties.Settings.Default.WormsPassword);

                        if (this.State == ConnectionStates.Connecting || this.State == ConnectionStates.ReConnecting)
                            this.State = ConnectionStates.Connected;
                    }
                    break;

                // This nickname is already in use!
                case 433:
                    if (serverIrcAddress == string.Empty)
                        return true;
                    else if (MVM != null)
                    {
                        // nickname is in use when we tried to change with /NICK command
                        MVM.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            new NickNameInUseTask(this).DoTask(MVM);
                        }));
                    }
                    break;

                // A channel (answer for the LIST command)
                case 322:
                    if (channelListHelper != null)
                    {
                        // :wormnet1.team17.com 322 Test #Help 10 :05 A place to get help, or help others
                        Match chMatch = channelRegex.Match(line, spacePos);
                        if (chMatch.Success)
                        {
                            string channelName = chMatch.Groups[1].Value;
                            string description = chMatch.Groups[3].Value;

                            channelListHelper[channelName] = description;
                        }
                    }
                    break;

                // LIST END
                case 323:
                    if (MVM != null)
                    {
                        var temp = channelListHelper;
                        MVM.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            new ChannelListTask(this, temp).DoTask(MVM);
                        }));
                    }
                    channelListHelper = null;
                    break;

                // A client (answer for WHO command)
                case 352:
                    if (MVM != null)
                    {
                        // :wormnet1.team17.com 352 Test #AnythingGoes ~UserName no.address.for.you wormnet1.team17.com Herbsman H :0 68 7 LT The Wheat Snooper 2.8
                        Match clMatch = clientRegex.Match(line, spacePos);
                        if (clMatch.Success)
                        {
                            string channelHash = clMatch.Groups[1].Value;
                            string clan = clMatch.Groups[2].Value.Equals("Username", StringComparison.OrdinalIgnoreCase) ? string.Empty : clMatch.Groups[2].Value;
                            string clientName = clMatch.Groups[3].Value;
                            string[] realName = clMatch.Groups[4].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // 68 7 LT The Wheat Snooper

                            Country country = Countries.DefaultCountry;
                            int rank = 0;
                            string clientApp = clMatch.Groups[4].Value;

                            if (realName.Length >= 3)
                            {
                                // set rank
                                if (int.TryParse(realName[1], out rank))
                                {
                                    if (rank > 13)
                                        rank = 13;
                                    if (rank < 0)
                                        rank = 0;
                                }

                                // set country
                                int countrycode;
                                if (int.TryParse(realName[0], out countrycode) && countrycode >= 0 && countrycode <= 52)
                                {
                                    if (countrycode == 49 && realName[2].Length == 2) // use cc as countricode
                                    {
                                        if (realName[2].Equals("UK", StringComparison.OrdinalIgnoreCase))
                                            realName[2] = "GB";
                                        else if (realName[2].Equals("EL", StringComparison.OrdinalIgnoreCase))
                                            realName[2] = "GR";
                                        country = Countries.GetCountryByCC(realName[2]);
                                    }
                                    else
                                        country = Countries.GetCountryByID(countrycode);
                                }
                                else if (realName[2].Length == 2) // use cc if countrycode is bigger than 52
                                {
                                    if (realName[2].Equals("UK", StringComparison.OrdinalIgnoreCase))
                                        realName[2] = "GB";
                                    else if (realName[2].Equals("EL", StringComparison.OrdinalIgnoreCase))
                                        realName[2] = "GR";
                                    country = Countries.GetCountryByCC(realName[2]);
                                }

                                StringBuilder sb = new StringBuilder();
                                for (int i = 3; i < realName.Length; i++)
                                {
                                    sb.Append(realName[i]);
                                    if (i + 1 < realName.Length)
                                        sb.Append(" ");
                                }
                                clientApp = sb.ToString();
                            }

                            MVM.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                new UserInfoTask(this, channelHash, clientName, country, clan, rank, clientApp).DoTask(MVM);
                            }));
                        }
                    }
                    break;

                // NAMES command answer
                case 353:
                    if (Properties.Settings.Default.UseWhoMessages == false && MVM != null)
                    {
                        Match m = namesRegex.Match(line.Substring(spacePos));
                        if (m.Success)
                        {
                            string channelName = m.Groups[2].Value;
                            string[] names = m.Groups[3].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            MVM.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                new NamesTask(this, channelName, names).DoTask(MVM);
                            }));
                        }
                    }
                    break;

                // The user is offline message
                case 401:
                    if (MVM != null)
                    {
                        // :wormnet1.team17.com 401 Test sToOMiToO :No such nick/channel
                        spacePos2 = line.IndexOf(' ', spacePos);
                        if (spacePos2 != -1)
                        {
                            string clientName = line.Substring(spacePos, spacePos2 - spacePos);
                            MVM.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                new OfflineTask(this, clientName).DoTask(MVM);
                            }));
                        }
                    }
                    break;

                default:
                    if (MVM != null && GlobalManager.DebugMode && number > 401)
                    {
                        string text = (line[spacePos + 1] == ':') ? line.Substring(spacePos + 2) : line.Substring(spacePos + 1);
                        MVM.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MVM.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, text);
                        }));
                    }
                    break;
            }

            return false;
        }

        #region Send messages
        // Send a message to WormNet
        public void Send(object sender, string message)
        {
            if (sender is AbstractCommunicator || this.State == ConnectionStates.Connected) // dont allow adding commands while the server is not connected
                messages.Enqueue(message);
        }

        public void SendMessage(object sender, string channelOrClientName, string message)
        {
            Send(sender, "PRIVMSG " + channelOrClientName + " :" + message);
        }

        public void SendNotice(object sender, string channelOrClientName, string message)
        {
            Send(sender, "NOTICE " + channelOrClientName + " :" + message);
        }

        public void SendCTCPMessage(object sender, string channelOrClientName, string command, string message = "")
        {
            if (message.Length == 0)
                Send(sender, "PRIVMSG " + channelOrClientName + " :\x01" + command + "\x01");
            else
                Send(sender, "PRIVMSG " + channelOrClientName + " :\x01" + command + " " + message + "\x01");
        }

        public void JoinChannel(object sender, string channelName, string password = null)
        {
            if (password == null)
                Send(sender, "JOIN " + channelName);
            else
                Send(sender, "JOIN " + channelName + " " + password);
        }

        public void JoinChannels(object sender, IEnumerable<string> channels)
        {
            Send(sender, "JOIN " + string.Join(",", channels));
        }

        public void NickChange(object sender, string newNick)
        {
            Send(sender, "NICK " + newNick);
        }

        public void LeaveChannel(object sender, string channelName)
        {
            Send(sender, "PART " + channelName);
        }

        public void GetChannelList(object sender)
        {
            Send(sender, "LIST");
        }

        public void GetChannelClients(object sender, string channelName)
        {
            Send(sender, "WHO " + channelName);
        }

        public void GetInfoAboutClient(object sender, string clientName)
        {
            Send(sender, "WHO " + clientName);
        }
        #endregion

        #region IDisposable
        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;

            if (disposing)
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

                if (connectionTask != null)
                {
                    connectionTask.Dispose();
                    connectionTask = null;
                }

                foreach (var chvm in this.Channels)
                {
                    ChannelViewModel channel = chvm.Value as ChannelViewModel;
                    if (channel != null && channel.ChannelSchemeTask != null)
                        channel.ChannelSchemeTask.Dispose();
                }
            }
        }

        ~AbstractCommunicator()
        {
            Dispose(false);
        }
        #endregion
    }
}
