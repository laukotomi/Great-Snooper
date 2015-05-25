using System;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MySnooper
{
    public class WormageddonWebComm
    {
        private MainWindow mw;
        private string serverAddress;

        // Regex
        // <SCHEME=Pf,Be>
        private Regex SchemeRegex = new Regex(@"^<SCHEME=([^>]+)>$", RegexOptions.IgnoreCase);
        // <GAME GameName Hoster HosterAddress CountryID 1 PasswordNeeded GameID HEXCC>
        private Regex GameRegex = new Regex(@"^<GAME\s(\S*)\s(\S+)\s(\S+)\s(\S+)\s1\s(\S+)\s(\S+)\s([^>]+)>$", RegexOptions.IgnoreCase);

        // Buffers for LoadHostedGames thread
        private byte[] RecvBuffer; // stores the bytes arrived from WormNet server. These bytes will be decoding into RecvMessage or into RecvHTML
        private System.Text.StringBuilder RecvHTML; // stores the encoded messages from the server which will be proceed by the IRC thread

        // Buffers for UI thread
        private byte[] RecvBufferUI = new byte[100];
        private System.Text.StringBuilder RecvHTMLUI;

        // Update game list
        public Task LoadGamesTask { get; private set; }
        public CancellationTokenSource LoadGamesCTS { get; private set; }


        public WormageddonWebComm(MainWindow mw, string ServerAddress)
        {
            this.serverAddress = ServerAddress;
            this.mw = mw;

            RecvBuffer = new byte[1024]; // 1kB
            RecvHTML = new System.Text.StringBuilder(RecvBuffer.Length);

            RecvBufferUI = new byte[100];
            RecvHTMLUI = new System.Text.StringBuilder(RecvBufferUI.Length);

            this.LoadGamesCTS = new CancellationTokenSource();
        }


        // Set the scheme of the channel
    }
}
