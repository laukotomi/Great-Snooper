﻿using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Localizations;
using GreatSnooper.Model;
using GreatSnooper.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace GreatSnooper.ViewModel
{
    public class LogChannelViewModel : AbstractChannelViewModel
    {
        #region Static
        private static Regex logMessageRegex = new Regex(@"^\((?<type>\w+)\) (?<date>\d+\-\d+\-\d+ \d+:\d+:\d+) (?<sender>[^:]+):(?<text>.*)", RegexOptions.Compiled);
        private static Regex logChannelClosedRegex = new Regex(@"^(?<date>\d+\-\d+\-\d+ \d+:\d+:\d+) Channel closed\.$", RegexOptions.Compiled);
        #endregion

        public LogChannelViewModel(MainViewModel mainViewModel, AbstractCommunicator server, string channelName)
            : base(mainViewModel, server)
        {
            this.Joined = true;
            this.Disabled = true;
            this.Name = "Log: " + channelName;

            var mainWindow = (MainWindow)mainViewModel.DialogService.GetView();
            tabitem = new TabItem();
            tabitem.DataContext = this;
            tabitem.Style = (Style)mainWindow.ChannelsTabControl.FindResource("channelTabItem");
            tabitem.ApplyTemplate();
            tabitem.Content = ConnectedLayout;

            server.Channels.Add(this.Name, this);

            //this.GenerateHeader();
            mainViewModel.Channels.Add(this);

            DirectoryInfo logDirectory = new DirectoryInfo(GlobalManager.SettingsPath + @"\Logs\" + channelName);
            if (logDirectory.Exists)
            {
                List<string> oldMessages = new List<string>();
                bool done = false;
                foreach (FileInfo file in logDirectory.GetFiles().OrderByDescending(f => f.LastWriteTime))
                {
                    string[] lines = File.ReadAllLines(file.FullName);
                    for (int i = lines.Length - 1; i >= 0; i--)
                    {
                        string line = lines[i];
                        if (!string.IsNullOrEmpty(line) && !line.StartsWith("---"))
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
                        break;
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
                                    UserHelper.GetUser(this.Server, m.Groups["sender"].Value),
                                    m.Groups["text"].Value,
                                    MessageSettings.GetByMessageType(messageType),
                                    time,
                                    true
                                )
                            );
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
                                    GlobalManager.SystemUser,
                                    GSLocalization.Instance.EndOfConversation,
                                    MessageSettings.SystemMessage,
                                    time,
                                    true
                                )
                            );
                        }
                    }
                }
            }
        }

        public override void ClearUsers()
        {

        }

        public override TabItem GetLayout()
        {
            return tabitem;
        }

        public override void ProcessMessage(IRCTasks.MessageTask msgTask)
        {

        }

        public override void SendActionMessage(string message, bool userMessage = false)
        {

        }

        public override void SendCTCPMessage(string ctcpCommand, string ctcpText, User except = null)
        {

        }

        public override void SendMessage(string message, bool userMessage = false)
        {

        }

        public override void SendNotice(string message, bool userMessage = false)
        {

        }

        public override void SetLoading(bool loading = true)
        {

        }

        public override void Log(int count, bool makeEnd = false)
        {

        }
    }
}
