namespace GreatSnooper.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Localizations;
    using GreatSnooper.Model;
    using GreatSnooper.Windows;

    public class LogChannelViewModel : AbstractChannelViewModel
    {
        private static Regex logChannelClosedRegex = new Regex(@"^(?<date>\d+\-\d+\-\d+ \d+:\d+:\d+) Channel closed\.$", RegexOptions.Compiled);
        private static Regex logMessageRegex = new Regex(@"^\((?<type>\w+)\) (?<date>\d+\-\d+\-\d+ \d+:\d+:\d+) (?<sender>[^:]+):(?<text>.*)", RegexOptions.Compiled);

        public LogChannelViewModel(MainViewModel mainViewModel, IRCCommunicator server, string channelName)
            : base(mainViewModel, server)
        {
            this.Joined = true;
            this.Disabled = true;
            this.Name = "Log: " + channelName;

            var mainWindow = (MainWindow)mainViewModel.DialogService.GetView();
            _tabitem = new TabItem();
            _tabitem.DataContext = this;
            _tabitem.Style = (Style)mainWindow.FindResource("logChannelTabItem");
            _tabitem.ApplyTemplate();
            _tabitem.Content = ConnectedLayout;

            server.Channels.Add(this.Name, this);
            mainViewModel.CreateChannel(this);

            try
            {
                string path = GlobalManager.SettingsPath + @"\Logs\" + channelName;
                if (Directory.Exists(path))
                {
                    DirectoryInfo logDirectory = new DirectoryInfo(path);
                    List<string> oldMessages = new List<string>();
                    bool done = false;
                    foreach (FileInfo file in logDirectory.GetFiles().OrderByDescending(f => f.LastWriteTime))
                    {
                        using (StreamReader sr = new StreamReader(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                        {
                            string[] lines = sr.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = lines.Length - 1; i >= 0; i--)
                            {
                                string line = lines[i];
                                if (!line.StartsWith("---"))
                                {
                                    oldMessages.Add(line);
                                    if (oldMessages.Count == GlobalManager.MaxMessagesInMemory)
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                            }
                            if (done)
                            {
                                break;
                            }
                        }
                    }

                    for (int i = oldMessages.Count - 1; i >= 0; i--)
                    {
                        Match m = logMessageRegex.Match(oldMessages[i]);
                        if (m.Success)
                        {
                            Message.MessageTypes messageType;
                            DateTime time;
                            if (Enum.TryParse(m.Groups["type"].Value, true, out messageType)
                                && DateTime.TryParseExact(m.Groups["date"].Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                            {
                                this.AddMessage(
                                    new Message(
                                        this,
                                        UserHelper.GetUser(this.Server, m.Groups["sender"].Value),
                                        m.Groups["text"].Value,
                                        MessageSettings.GetByMessageType(messageType),
                                        time,
                                        true));
                            }
                            continue;
                        }
                        m = logChannelClosedRegex.Match(oldMessages[i]);
                        if (m.Success)
                        {
                            DateTime time;
                            if (DateTime.TryParseExact(m.Groups["date"].Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                            {
                                this.AddMessage(
                                    new Message(
                                        this,
                                        GlobalManager.SystemUser,
                                        GSLocalization.Instance.EndOfConversation,
                                        MessageSettings.SystemMessage,
                                        time,
                                        true));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }

        public override void ClearUsers()
        {
        }

        public override TabItem GetLayout()
        {
            return _tabitem;
        }

        public override void ProcessMessage(IRCTasks.MessageTask msgTask)
        {
        }

        public override void SendActionMessage(string message)
        {
        }

        public override void SendCTCPMessage(string ctcpCommand, string ctcpText, User except = null)
        {
        }

        public override void SendMessage(string message)
        {
        }

        public override void SendNotice(string message)
        {
        }

        public override void SetLoading(bool loading = true)
        {
        }
    }
}
