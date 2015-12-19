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
        private const int gamePort = 17011;

        private volatile IPAddress[] wormnatAddress;
        private volatile Socket controlSocket;
        private volatile bool stopping = false;
        private volatile int threadCounter = 0;

        private string gameID = string.Empty;

        private readonly string serverAddress;
        private readonly string gameExePath;
        private readonly string nickName;
        private readonly string hostName;
        private readonly string passWord;
        private readonly string channelName;
        private readonly string channelScheme;
        private readonly string location;
        private readonly string cc;
        private readonly string snooperSettingsPath;
        private readonly bool useWormNat;
        private readonly bool highPriority;

        public WormNat(string serverAddress, string gameExePath, string nickName, string hostName, string passWord, string channelName, string channelScheme, string location, string cc, string useWormNat, string highPriority, string settingsPath)
        {
            this.serverAddress = serverAddress;
            this.gameExePath = gameExePath;
            this.nickName = nickName;
            this.hostName = hostName;
            this.passWord = passWord;
            this.channelName = channelName;
            this.channelScheme = channelScheme;
            this.location = location;
            this.cc = cc;
            this.useWormNat = (useWormNat == "1");
            this.highPriority = (highPriority == "1");
            this.snooperSettingsPath = settingsPath;
        }

        private void ConnectionThread(int proxyPort)
        {
            Interlocked.Increment(ref threadCounter);
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
                            gameSocket.Connect("127.0.0.1", gamePort);
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
            Interlocked.Decrement(ref threadCounter);
        }

        private void ControlThread()
        {
            Interlocked.Increment(ref threadCounter);
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
            Interlocked.Decrement(ref threadCounter);
        }

        private void CloseGameThread()
        {
            Interlocked.Increment(ref threadCounter);
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
                string sURL = "http://" + serverAddress + ":80/wormageddonweb/Game.asp?Cmd=Close&GameID=" + gameID + "&Name=" + hostName + "&HostID=&GuestID=&GameType=0";
                HttpWebRequest wrGETURL = (HttpWebRequest)WebRequest.Create(sURL);
                wrGETURL.Method = "HEAD";
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

            Interlocked.Decrement(ref threadCounter);
        }

        public void Start()
        {
            try
            {
                string hostIP;

                // Join to the proxy server
                if (this.useWormNat)
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
                else
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
                            if (localIP.Contains("."))
                                hostIP = localIP + ":" + gamePort.ToString(); // IPv4
                            else
                                hostIP = "[" + localIP + "]:" + gamePort.ToString(); // IPv6
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
                                //Search for the ip in the html
                                string localIP = stream.ReadToEnd();
                                int first = localIP.IndexOf("Address: ") + 9;
                                int last = localIP.LastIndexOf("</body>");
                                localIP = localIP.Substring(first, last - first);

                                if (localIP.Contains("."))
                                    hostIP = localIP + ":" + gamePort.ToString(); // IPv4
                                else
                                    hostIP = "[" + localIP + "]:" + gamePort.ToString(); // IPv6
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
                    string sURL = "http://" + serverAddress + ":80/wormageddonweb/Game.asp?Cmd=Create&Name=" + hostName + "&HostIP=" + hostIP + "&Nick=" + nickName + "&Pwd=" + passWord + "&Chan=" + channelName + "&Loc=" + location + "&Type=" + cc;
                    HttpWebRequest wrGETURL = (HttpWebRequest)WebRequest.Create(sURL);
                    wrGETURL.Method = "HEAD";
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
                    p.StartInfo.FileName = gameExePath;
                    p.StartInfo.Arguments = @"wa://?gameid=" + gameID + "&scheme=" + channelScheme + "&pass=" + passWord;
                    if (p.Start())
                    {
                        if (highPriority)
                            p.PriorityClass = ProcessPriorityClass.High;

                        if (useWormNat)
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
                        string filename = this.snooperSettingsPath + @"\HosterLog.txt";
                        // Delete log file if it is more than 10 Mb
                        FileInfo logfile = new FileInfo(filename);
                        if (logfile.Exists && logfile.Length > 10 * 1024 * 1024)
                            logfile.Delete();

                        using (StreamWriter w = new StreamWriter(filename, true))
                        {
                            w.WriteLine(DateTime.Now.ToString("U"));
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
    }
}
