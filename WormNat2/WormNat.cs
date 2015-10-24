using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WormNat2
{
    class WormNat
    {
        private enum Errors { NoError, WormNatError, WormNatInitError, FailedToGetLocalIP, CreateGameFailed, NoGameID, FailedToStartTheGame, Unkown, WormNatClientError }
        private volatile Errors error = Errors.NoError;

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
        private readonly bool useWormNat;
        private readonly bool highPriority;

        public WormNat(string serverAddress, string gameExePath, string nickName, string hostName, string passWord, string channelName, string channelScheme, string location, string cc, string useWormNat, string highPriority)
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
                        if (stopping)
                            break;

                        try
                        {
                            gameSocket.Connect("127.0.0.1", gamePort);
                            ok = true;
                        }
                        catch (Exception)
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
            catch (Exception)
            {
                error = Errors.WormNatClientError;
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
                using (controlSocket)
                {
                    byte[] buffer = new byte[2];

                    while (true)
                    {
                        if (stopping)
                            break;

                        if (controlSocket.Poll(5000, SelectMode.SelectRead) && controlSocket.Available == 0)
                            break;
                        if (controlSocket.Available > 0)
                        {
                            if (controlSocket.Receive(buffer) == 2)
                            {
                                Thread t = new Thread(() => ConnectionThread(buffer[1] * 256 + buffer[0]));
                                t.Start();
                            }
                        }
                    }
                }
                controlSocket = null;
            }
            catch (Exception)
            {
                error = Errors.WormNatError;
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
            catch (Exception)
            {
                Debug.WriteLine("Failed to close the game!");
            }

            Interlocked.Decrement(ref threadCounter);
        }

        public void Start()
        {
            try
            {
                string hostIP;

                // Join to the proxy server
                if (useWormNat)
                {
                    try
                    {
                        wormnatAddress = Dns.GetHostAddresses(proxyAddress);
                        controlSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        controlSocket.Connect(wormnatAddress, defProxyPort);

                        byte[] buffer = new byte[2];
                        if (controlSocket.Receive(buffer) != 2)
                        {
                            error = Errors.WormNatInitError;
                            throw new Exception();
                        }

                        int externalPort = buffer[1] * 256 + buffer[0];
                        Debug.WriteLine("External port is: " + externalPort);

                        // Since we got the port that we use on the proxy server, we can register the game to the game list with the address of the proxy and the given external port
                        hostIP = proxyAddress + ":" + externalPort.ToString();
                    }
                    catch (Exception)
                    {
                        if (error != Errors.NoError)
                            error = Errors.WormNatError;
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
                    catch (Exception)
                    {
                        error = Errors.FailedToGetLocalIP;
                        throw;
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
                    // We will get the GameID in a header
                    using (WebResponse response = wrGETURL.GetResponse())
                    {
                        string[] SetGameId = response.Headers.GetValues("SetGameId");
                        if (SetGameId.Length != 0)
                        {
                            gameID = response.Headers["SetGameId"].Substring(2);

                            // If we hosted too many games too fast, then we may get banned for a while
                            if (gameID == string.Empty)
                            {
                                Debug.WriteLine("GameID is missing!");
                                error = Errors.NoGameID;
                                throw new Exception();
                            }

                            Debug.WriteLine("Game id: " + gameID);
                            Console.WriteLine("1");
                        }
                    }
                }
                catch (Exception)
                {
                    if (error != Errors.NoError)
                        error = Errors.CreateGameFailed;
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
                catch (Exception)
                {
                    error = Errors.FailedToStartTheGame;
                    throw;
                }
            }
            catch (Exception)
            {
                if (error == Errors.NoError)
                    error = Errors.Unkown;
            }
            finally
            {
                // Stopping threads
                stopping = true;
                while (threadCounter > 0)
                    Thread.Sleep(50);

                if (controlSocket != null)
                    controlSocket.Dispose();

                if (error != Errors.NoError)
                {
                    Debug.WriteLine("Error: " + error.ToString());
                    Console.WriteLine(((int)error).ToString());
                }
            }
        }
    }
}
