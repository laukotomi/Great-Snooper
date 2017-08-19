using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Hoster
{
    class WormNat
    {
        private enum Errors { NoError, WormNatError, WormNatInitError, FailedToGetLocalIP, CreateGameFailed, NoGameID, FailedToStartTheGame, Unkown, WormNatClientError }
        private volatile Errors error = Errors.NoError;
        private Exception exception;
        private object exceptionLocker = new object();

        private const string proxyAddress = "proxy.wormnet.net";
        private const int defProxyPort = 9301;
        private const int defaultGamePort = 17011;

        private volatile IPAddress[] wormnatAddress;
        private volatile Socket controlSocket;
        private volatile bool stopping = false;
        private volatile int threadCounter = 0;

        private string gameID = string.Empty;

        private readonly Options options;

        public WormNat(Options options)
        {
            this.options = options;
            if (this.options.Port == default(int))
            {
                this.options.Port = defaultGamePort;
            }
        }

        private void ConnectionThread(int proxyPort)
        {
#pragma warning disable
            Interlocked.Increment(ref threadCounter);
#pragma warning restore
            Debug.WriteLine("Client joined on port: " + proxyPort);

            try
            {
                using (Socket gameSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                using (Socket proxySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    // Connect to the proxy
                    proxySocket.Connect(proxyAddress, proxyPort);

                    // Connect to the game
                    bool ok = false;
                    while (!ok)
                    {
                        if (this.stopping)
                            break;

                        try
                        {
                            gameSocket.Connect("127.0.0.1", this.options.Port);
                            ok = true;
                        }
                        catch
                        {
                            Debug.WriteLine("Failed to join the game.");
                            Thread.Sleep(500);
                        }
                    }


                    if (ok)
                    {
                        byte[] buffer = new byte[4096];
                        int bytes;

                        while (true)
                        {
                            if (proxySocket.Poll(5000, SelectMode.SelectRead) && proxySocket.Available == 0)
                                break; // Connection lost
                            if (proxySocket.Available > 0)
                            {
                                bytes = proxySocket.Receive(buffer);
                                gameSocket.Send(buffer, 0, bytes, SocketFlags.None);
                            }


                            if (gameSocket.Poll(5000, SelectMode.SelectRead) && gameSocket.Available == 0)
                                break;
                            if (gameSocket.Available > 0)
                            {
                                bytes = gameSocket.Receive(buffer);
                                proxySocket.Send(buffer, 0, bytes, SocketFlags.None);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.error = Errors.WormNatClientError;
                lock (this.exceptionLocker)
                {
                    if (this.exception == null)
                        this.exception = ex;
                }
            }

            Debug.WriteLine("Client released (" + proxyPort + ")");
#pragma warning disable
            Interlocked.Decrement(ref threadCounter);
#pragma warning restore
        }

        private void ControlThread()
        {
#pragma warning disable
            Interlocked.Increment(ref threadCounter);
#pragma warning restore
            Debug.WriteLine("ControlThread started.");

            try
            {
                using (this.controlSocket)
                {
                    byte[] buffer = new byte[2];

                    while (true)
                    {
                        if (this.stopping)
                            break;

                        if (this.controlSocket.Poll(5000, SelectMode.SelectRead) && this.controlSocket.Available == 0)
                            break;
                        if (this.controlSocket.Available > 0)
                        {
                            if (this.controlSocket.Receive(buffer) == 2)
                            {
                                Thread t = new Thread(() => ConnectionThread(buffer[1] * 256 + buffer[0]));
                                t.Start();
                            }
                        }
                    }
                }
                controlSocket = null;
            }
            catch (Exception ex)
            {
                this.error = Errors.WormNatError;
                lock (this.exceptionLocker)
                {
                    if (this.exception == null)
                        this.exception = ex;
                }
            }

            Debug.WriteLine("ControlThread stopped.");
#pragma warning disable
            Interlocked.Decrement(ref threadCounter);
#pragma warning restore
        }

        private void CloseGameThread()
        {
#pragma warning disable
            Interlocked.Increment(ref threadCounter);
#pragma warning restore
            Debug.WriteLine("CloseGame thread started.");

            while (true)
            {
                if (stopping)
                    break;

                IntPtr hwnd = NativeMethods.FindWindow(null, "Worms2D");
                if (hwnd != IntPtr.Zero)
                    break;
                Thread.Sleep(2000);
            }

            try
            {
                string sURL = "http://" + this.options.ServerAddress + ":80/wormageddonweb/Game.asp?Cmd=Close&GameID=" + gameID + "&Name=" + this.options.HostName + "&HostID=&GuestID=&GameType=0";
                HttpWebRequest wrGETURL = (HttpWebRequest)WebRequest.Create(sURL);
                wrGETURL.Method = "GET";
                wrGETURL.AllowAutoRedirect = false;
                wrGETURL.UserAgent = "T17Client/1.2";
                wrGETURL.Headers.Add("UserServerIdent", "2");
                wrGETURL.Proxy = null;
                using (WebResponse response = wrGETURL.GetResponse()) { }
                Debug.WriteLine("Game closed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to close the game!");
                lock (this.exceptionLocker)
                {
                    if (this.exception == null)
                        this.exception = ex;
                }
            }

#pragma warning disable
            Interlocked.Decrement(ref threadCounter);
#pragma warning restore
        }

        public void Start()
        {
            try
            {
                string hostIP = this.options.IP;

                // Join to the proxy server
                if (this.options.UseWormNat)
                {
                    try
                    {
                        this.wormnatAddress = Dns.GetHostAddresses(proxyAddress);
                        this.controlSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        this.controlSocket.Connect(this.wormnatAddress, defProxyPort);

                        byte[] buffer = new byte[2];
                        if (this.controlSocket.Receive(buffer) != 2)
                        {
                            error = Errors.WormNatInitError;
                            throw new Exception("Failed to receive external port!");
                        }

                        int externalPort = buffer[1] * 256 + buffer[0];
                        Debug.WriteLine("External port is: " + externalPort);

                        // Since we got the port that we use on the proxy server, we can register the game to the game list with the address of the proxy and the given external port
                        hostIP = proxyAddress + ":" + externalPort.ToString();
                    }
                    catch (Exception ex)
                    {
                        if (this.error != Errors.NoError)
                            this.error = Errors.WormNatError;
                        this.exception = ex;
                        if (controlSocket != null)
                        {
                            controlSocket.Dispose();
                            controlSocket = null;
                        }
                        throw;
                    }
                }
                // Basic way to host
                else if (string.IsNullOrEmpty(hostIP))
                {
                    // Get local IP
                    try
                    {
                        HttpWebRequest wrGETURL = (HttpWebRequest)WebRequest.Create("http://bot.whatismyipaddress.com");
                        wrGETURL.AllowAutoRedirect = false;
                        wrGETURL.Proxy = null;
                        using (WebResponse response = wrGETURL.GetResponse())
                        using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                        {
                            string localIP = stream.ReadToEnd();
                            hostIP = this.CreateIp(localIP);
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
                            request.Proxy = null;
                            using (WebResponse response = request.GetResponse())
                            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                            {
                                // Search for the ip in the html
                                string localIP = stream.ReadToEnd();
                                int first = localIP.IndexOf("Address: ") + 9;
                                int last = localIP.LastIndexOf("</body>");

                                localIP = localIP.Substring(first, last - first);
                                hostIP = this.CreateIp(localIP);
                            }
                        }
                        catch (Exception)
                        {
                            this.error = Errors.FailedToGetLocalIP;
                            this.exception = ex;
                            throw;
                        }
                    }
                }

                try
                {
                    // Create the game and get its ID
                    string sURL = "http://" + this.options.ServerAddress + ":80/wormageddonweb/Game.asp?Cmd=Create&Name=" + this.options.HostName + "&HostIP=" + hostIP + "&Nick=" + this.options.NickName + "&Pwd=" + this.options.PassWord + "&Chan=" + this.options.ChannelName + "&Loc=" + this.options.Location + "&Type=" + this.options.CC;
                    HttpWebRequest wrGETURL = (HttpWebRequest)WebRequest.Create(sURL);
                    wrGETURL.Method = "GET";
                    wrGETURL.AllowAutoRedirect = false;
                    wrGETURL.Proxy = null;
                    // We need to set some headers in order to get the proper answer from Game.asp script
                    wrGETURL.UserAgent = "T17Client/1.2";
                    wrGETURL.Headers.Add("UserServerIdent", "2");
                    wrGETURL.Timeout = 30000;
                    // We will get the GameID in a header
                    using (WebResponse response = wrGETURL.GetResponse())
                    {
                        try
                        {
                            gameID = response.Headers["SetGameId"].Substring(2);

                            // If we hosted too many games too fast, then we may get banned for a while
                            if (gameID == string.Empty)
                                throw new Exception();

                            Debug.WriteLine("Game id: " + gameID);

                            // Report success to the snooper
                            Console.WriteLine(((int)Errors.NoError).ToString());
                        }
                        catch
                        {
                            Debug.WriteLine("GameID is missing!");
                            this.error = Errors.NoGameID;
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (this.error == Errors.NoError)
                        this.error = Errors.CreateGameFailed;
                    this.exception = ex;
                    Debug.WriteLine("The Create-Game request failed!");
                    throw;
                }


                // Wait for snooper to process the result
                Console.ReadLine();


                // Start a Thread that will be responsible to close the game when needed
                Thread t2 = new Thread(CloseGameThread);
                t2.Start();


                // Start W:A with the proper GameID and Scheme
                Debug.WriteLine("Starting the game...");
                try
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.FileName = this.options.GameExePath;
                    p.StartInfo.Arguments = @"wa://?gameid=" + gameID + "&scheme=" + this.options.ChannelScheme + "&pass=" + this.options.PassWord;
                    if (p.Start())
                    {
                        if (this.options.SetHighPriority)
                            p.PriorityClass = ProcessPriorityClass.High;

                        if (this.options.UseWormNat)
                        {
                            Thread t = new Thread(ControlThread);
                            t.Start();
                        }

                        while (true)
                        {
                            if (p.HasExited)
                                break;
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (this.error == Errors.NoError)
                        this.error = Errors.FailedToStartTheGame;
                    lock (this.exceptionLocker)
                    {
                        if (this.exception != null)
                            this.exception = ex;
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (this.error == Errors.NoError)
                    this.error = Errors.Unkown;
                lock (this.exceptionLocker)
                {
                    if (this.exception != null)
                        this.exception = ex;
                }
            }
            finally
            {
                // Stopping threads
                this.stopping = true;
                while (this.threadCounter > 0)
                    Thread.Sleep(50);

                if (this.controlSocket != null)
                    this.controlSocket.Dispose();

                if (this.error != Errors.NoError)
                {
                    Debug.WriteLine("Error: " + this.error.ToString());
                    Console.WriteLine(((int)this.error).ToString());
                }
                if (this.exception != null)
                {
                    try
                    {
                        string filename = this.options.SettingsPath + @"\HosterLog.txt";
                        // Delete log file if it is more than 10 Mb
                        FileInfo logfile = new FileInfo(filename);
                        if (logfile.Exists && logfile.Length > 10 * 1024 * 1024)
                            logfile.Delete();

                        using (StreamWriter w = new StreamWriter(filename, true))
                        {
                            w.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            w.WriteLine(this.error.ToString());
                            w.WriteLine(this.exception.GetType().FullName);
                            w.WriteLine(this.exception.Message);
                            w.WriteLine(this.exception.StackTrace);
                            w.WriteLine(Environment.NewLine + Environment.NewLine + Environment.NewLine);
                        }
                    }
                    catch { }
                }
            }
        }

        private string CreateIp(string ip)
        {
            if (ip.Contains("."))
                return ip + ":" + this.options.Port.ToString(); // IPv4
            else
                return "[" + ip + "]:" + this.options.Port.ToString(); // IPv6
        }
    }
}
