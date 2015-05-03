using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections;

namespace WormNat2
{
    class WormNat
    {
        private const string ProxyAddress = "proxy.worms2d.info";
        private const int ControlPort = 17018;
        private const int GamePort = 17011;

        private volatile bool Stopping = false;
        private volatile bool CloseGame = false;

        private int ExternalPort = 0;
        private int ConnectionThreadCounter = 0;
        private object ExternalPortLocker = new object();
        private object ConnectionThreadCounterLocker = new object();

        private string GameID = string.Empty;

        private readonly string ServerAddress;
        private readonly string GameExePath;
        private readonly string NickName;
        private readonly string HostName;
        private readonly string PassWord;
        private readonly string ChannelName;
        private readonly string ChannelScheme;
        private readonly string Location;
        private readonly string CC;
        private readonly bool UseWormNat;

        public WormNat(string ServerAddress, string GameExePath, string NickName, string HostName, string PassWord, string ChannelName, string ChannelScheme, string Location, string CC, string UseWormNat)
        {
            this.ServerAddress = ServerAddress;
            this.GameExePath = GameExePath;
            this.NickName = NickName;
            this.HostName = HostName;
            this.PassWord = PassWord;
            this.ChannelName = ChannelName;
            this.ChannelScheme = ChannelScheme;
            this.Location = Location;
            this.CC = CC;
            this.UseWormNat = (UseWormNat == "1");
        }


        private void ConnectionThread(int ProxyPort)
        {
            Console.WriteLine("Client joined on port: " + ProxyPort);

            lock (ConnectionThreadCounterLocker)
            {
                ConnectionThreadCounter++;
            }

            try
            {
                using (Socket GameSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                using (Socket ProxySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    byte[] Buffer = new byte[4096];
                    int Bytes;

                    /*
                        * Connect to the game
                        * */
                    IAsyncResult result = GameSocket.BeginConnect("127.0.0.1", GamePort, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(5000, true);
                    if (!success)
                        throw new SocketException();

                    /*
                        * Connect to the proxy
                        * */
                    ProxySocket.Connect(Dns.GetHostAddresses(ProxyAddress), ProxyPort);

                    while (true)
                    {
                        if (ProxySocket.Poll(5000, SelectMode.SelectRead) && ProxySocket.Available == 0)
                            break; // Connection lost
                        if (ProxySocket.Available > 0)
                        {
                            Bytes = ProxySocket.Receive(Buffer);
                            GameSocket.Send(Buffer, 0, Bytes, SocketFlags.None); ;
                        }


                        if (GameSocket.Poll(5000, SelectMode.SelectRead) && GameSocket.Available == 0)
                            break;
                        if (GameSocket.Available > 0)
                        {
                            Bytes = GameSocket.Receive(Buffer);
                            ProxySocket.Send(Buffer, 0, Bytes, SocketFlags.None);
                        }
                    }
                }
            }
            catch (SocketException) { }

            lock (ConnectionThreadCounterLocker)
            {
                ConnectionThreadCounter--;
            }

            Console.WriteLine("Client released (" + ProxyPort + ")");
        }

        private void ControlThread()
        {
            try
            {
                using (Socket ControlSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    byte[] Buffer = new byte[2];

                    ControlSocket.Connect(Dns.GetHostAddresses(ProxyAddress), ControlPort);
                    if (ControlSocket.Receive(Buffer) != 2)
                        return;

                    lock (ExternalPortLocker)
                    {
                        ExternalPort = Buffer[1] * 256 + Buffer[0];
                    }
                    //Console.WriteLine("External port is: " + ExternalPort);

                    while (true)
                    {
                        if (Stopping)
                            break;

                        if (ControlSocket.Poll(5000, SelectMode.SelectRead) && ControlSocket.Available == 0)
                            break;
                        if (ControlSocket.Available >= 2)
                        {
                            if (ControlSocket.Receive(Buffer) == 2)
                            {
                                Thread t = new Thread(() => ConnectionThread(Buffer[1] * 256 + Buffer[0]));
                                t.Start();
                            }
                        }
                    }
                }
            }
            catch (SocketException) { }

            lock (ExternalPortLocker)
            {
                ExternalPort = 0;
            }

            Console.WriteLine("ControlThread stopped.");
        }

        private void CloseGameThread()
        {
            while (true)
            {
                if (CloseGame)
                    break;

                IntPtr hwnd = NativeMethods.FindWindow("Worms2D", null);
                if (hwnd != IntPtr.Zero)
                {
                    break;
                }
                Thread.Sleep(2000);
            }


            try
            {
                string sURL = "http://" + ServerAddress + ":80/wormageddonweb/Game.asp?Cmd=Close&GameID=" + GameID + "&Name=" + HostName + "&HostID=&GuestID=&GameType=0";
                HttpWebRequest wrGETURL = (HttpWebRequest)WebRequest.Create(sURL);
                wrGETURL.Method = "HEAD";
                wrGETURL.AllowAutoRedirect = false;
                wrGETURL.UserAgent = "T17Client/1.2";
                wrGETURL.Headers.Add("UserServerIdent", "2");
                wrGETURL.Proxy = null;
                using (WebResponse response = wrGETURL.GetResponse()) { }
                Console.WriteLine("Game closed.");
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to close the game!");
            }
        }

        public void Start()
        {
            string sURL;
            Thread t;

            // Join to the proxy server in an other thread
            if (UseWormNat)
            {
                t = new Thread(ControlThread);
                t.Start();

                // Wait until the thread gets the external port from the proxy server
                while (true)
                {
                    lock (ExternalPortLocker)
                    {
                        if (ExternalPort != 0)
                            break;
                    }
                    Thread.Sleep(50);
                }

                // Since we got the port that we use on the proxy server, we can register the game to the game list with the address of the proxy and the given external port
                sURL = "http://" + ServerAddress + ":80/wormageddonweb/Game.asp?Cmd=Create&Name=" + HostName + "&HostIP=" + ProxyAddress + ":" + ExternalPort + "&Nick=" + NickName + "&Pwd=" + PassWord + "&Chan=" + ChannelName + "&Loc=" + Location + "&Type=" + CC;
            }
            // Basic way to host
            else
            {
                string localIP = "";
                // Create the game and get its ID
                HttpWebRequest wrGETURL = (HttpWebRequest)WebRequest.Create("http://bot.whatismyipaddress.com");
                wrGETURL.AllowAutoRedirect = false;
                wrGETURL.Proxy = null;
                using (WebResponse response = wrGETURL.GetResponse())
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    localIP = stream.ReadToEnd();
                }

                sURL = "http://" + ServerAddress + ":80/wormageddonweb/Game.asp?Cmd=Create&Name=" + HostName + "&HostIP=" + localIP + ":17011&Nick=" + NickName + "&Pwd=" + PassWord + "&Chan=" + ChannelName + "&Loc=" + Location + "&Type=" + CC;
            }


            try
            {
                // Create the game and get its ID
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
                        Console.WriteLine("1");
                        GameID = response.Headers["SetGameId"].Substring(2);
                        Console.WriteLine("Game id: " + GameID);
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("The Create-Game request failed!");
            }


            // If we hosted too many games too fast, then we may get banned for a while
            if (GameID == string.Empty)
            {
                Console.WriteLine("GameID is missing!");

                if (UseWormNat)
                {
                    // Ask and wait for the ConrtolThread to stop itself
                    Stopping = true;
                    while (true)
                    {
                        lock (ExternalPortLocker)
                        {
                            if (ExternalPort == 0)
                                break;
                        }
                        Thread.Sleep(10);
                    }
                }
                return;
            }


            // Start a Thread that will be responsible to close the game if needed
            Thread t2 = new Thread(CloseGameThread);
            t2.Start();


            // Start W:A with the proper GameID and Scheme
            Console.WriteLine("Starting the game...");
            try
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.FileName = GameExePath;
                p.StartInfo.Arguments = @"wa://?gameid=" + GameID + "&scheme=" + ChannelScheme + "&pass=" + PassWord;
                if (p.Start())
                {
                    while (true)
                    {
                        if (p.HasExited)
                            break;
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception) { }


            // (Stopping threads further on..)
            // Ask and wait for the CloseGameThread to stop itself and close the game
            if (t2.IsAlive)
            {
                CloseGame = true;
                while (t2.IsAlive)
                {
                    Thread.Sleep(10);
                }
            }


            if (UseWormNat)
            {
                // Ask and wait for the ConrtolThread to stop itself
                Stopping = true;
                while (true)
                {
                    lock (ExternalPortLocker)
                    {
                        if (ExternalPort == 0)
                            break;
                    }
                    Thread.Sleep(10);
                }

                // Wait until the connection threads stop
                while (true)
                {
                    lock (ConnectionThreadCounterLocker)
                    {
                        if (ConnectionThreadCounter == 0)
                            break;
                    }
                    Thread.Sleep(10);
                }
            }
        }
    }
}
