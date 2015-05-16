using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;


namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        private StringBuilder sb = new StringBuilder();

        // Instant coloring
        private ContextMenu ColorChooser;
        private Dictionary<string, SolidColorBrush> ChoosedColors = new Dictionary<string, SolidColorBrush>();

        // User messages history
        public void MessagesHistory(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                TextBox tb = (TextBox)sender;
                Channel ch = (Channel)tb.DataContext;
                if (ch.UserMessageLoadedIdx == -1)
                {
                    ch.TempMessage = tb.Text;
                }
                tb.Text = ch.LoadNextUserMessage();
                tb.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                TextBox tb = (TextBox)sender;
                Channel ch = (Channel)tb.DataContext;
                string text = ch.LoadPrevUserMessage();
                if (ch.UserMessageLoadedIdx == -1)
                    tb.Text = ch.TempMessage;
                else
                    tb.Text = text;
                tb.SelectAll();
                e.Handled = true;
            }
        }

        // Send a message by the user (action)
        public void MessageSend(object sender, KeyEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (e.Key == Key.Return && tb.Text.Length > 0)
            {
                // Remove non-wormnet characters
                Channel ch = (Channel)tb.DataContext;
                string message = (ch.Server.IsWormNet) ? message = WormNetCharTable.RemoveNonWormNetChars(tb.Text.TrimEnd()) : tb.Text.TrimEnd();

                if (message.Length > 0)
                {
                    ch.UserMessagesAdd(message);
                    ch.UserMessageLoadedIdx = -1;
                    SendMessageToChannel(message, ch, true);
                }

                tb.Clear();
                e.Handled = true;
            }
        }

        // Send a message to a channel (+ user functions)
        public void SendMessageToChannel(string textToSend, Channel channel, bool userMessage = false)
        {
            // Command message
            if (textToSend[0] == '/')
            {
                // Get the command
                int spacePos = textToSend.IndexOf(' ');
                string command, text = string.Empty;
                if (spacePos != -1)
                {
                    command = textToSend.Substring(1, spacePos - 1).ToLower();
                    text = textToSend.Substring(spacePos + 1).Trim();
                }
                else
                    command = textToSend.Substring(1).ToLower();

                // Process the command
                switch (command)
                {
                    case "me":
                        if (text.Length > 0)
                            SendActionMessage(text, channel, userMessage);
                        break;

                    case "msg":
                        if (text.Contains(" "))
                            channel.Server.SendMessage(channel.Name, text);
                        break;

                    case "nick":
                        if (text.Length > 0 && channel != null && channel.Joined && !channel.Server.IsWormNet)
                            channel.Server.NickChange(text);
                        break;

                    case "away":
                        AwayText = (text.Length == 0) ? Properties.Settings.Default.AwayText : text;
                        break;

                    case "back":
                        backText = (text.Length == 0) ? Properties.Settings.Default.BackText : text;
                        AwayText = string.Empty;
                        break;

                    case "ctcp":
                        if (channel != null && channel.Joined && text.Length > 0)
                        {
                            string ctcpCommand = text;
                            string ctcpText = string.Empty;

                            spacePos = text.IndexOf(' ');
                            if (spacePos != -1)
                            {
                                ctcpCommand = text.Substring(0, spacePos);
                                ctcpText = text.Substring(spacePos + 1);
                            }

                            if (channel.IsPrivMsgChannel)
                            {
                                if (channel.Clients.Count == 1)
                                    channel.Server.SendCTCPMessage(channel.Clients[0].Name, ctcpCommand, ctcpText);
                            }
                            else
                                channel.Server.SendCTCPMessage(channel.Name, ctcpCommand, ctcpText);
                        }
                        break;

                    case "gs":
                        var sb = new StringBuilder();
                        var helper = new Dictionary<string, bool>();
                        int count = 0;
                        for (int i = 0; i < Servers.Count; i++)
                        {
                            if (Servers[i].IsRunning)
                            {
                                foreach (var item in Servers[i].Clients)
                                {
                                    if (item.Value.ClientGreatSnooper && !helper.ContainsKey(item.Value.Name))
                                    {
                                        helper.Add(item.Value.Name, true);
                                        if (sb.Length != 0)
                                            sb.Append(", ");
                                        sb.Append(item.Value.Name);
                                        count++;
                                    }
                                }
                            }
                        }
                        MessageBox.Show(this, " Great Snooper is used by " + count + " user(s)! " + sb.ToString(), "Great Snooper check", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case "log":
                    case "logs":
                        System.Diagnostics.Process.Start(GlobalManager.SettingsPath);
                        break;

                    case "news":
                        OpenNewsWindow();
                        break;

                    case "debug":
                        GlobalManager.DebugMode = !GlobalManager.DebugMode;
                        break;
                }
            }
            // Action message
            else if (Properties.Settings.Default.ActionMessageWithGT && textToSend[0] == '>')
            {
                string text = textToSend.Substring(1).Trim();
                if (text.Length > 0)
                    SendActionMessage(text, channel, userMessage);
            }
            // Simple message
            else if (channel != null && channel.Joined)
            {
                string text = textToSend.Trim();
                if (text.Length > 0)
                {
                    if (channel.IsPrivMsgChannel)
                    {
                        if (channel.Clients.Count > 1)
                        {
                            // Broadcast
                            foreach (Client c in channel.Clients)
                            {
                                if (c.OnlineStatus != 0 && !c.IsBanned)
                                    channel.Server.SendCTCPMessage(c.Name, "CMESSAGE", channel.HashName + "|" + text);
                            }
                        }
                        else if (channel.Clients[0].OnlineStatus != 0)
                            channel.Server.SendMessage(channel.Clients[0].Name, text);
                    }
                    else
                        channel.Server.SendMessage(channel.Name, text);

                    channel.AddMessage(channel.Server.User, text, MessageSettings.UserMessage);
                    if (userMessage)
                    {
                        channel.SendAway = false;
                        channel.SendBack = false;
                    }
                }
            }
        }

        private void SendActionMessage(string text, Channel channel, bool userMessage = false)
        {
            if (channel == null || !channel.Joined)
                return;

            if (channel.IsPrivMsgChannel)
            {
                if (channel.Clients.Count > 1)
                {
                    // Broadcast
                    foreach (Client c in channel.Clients)
                    {
                        if (c.OnlineStatus != 0 && !c.IsBanned)
                            channel.Server.SendCTCPMessage(c.Name, "CACTION", channel.HashName + "|" + text);
                    }
                }
                else if (channel.Clients[0].OnlineStatus != 0)
                    channel.Server.SendCTCPMessage(channel.Clients[0].Name, "ACTION", text);
            }
            else
                channel.Server.SendCTCPMessage(channel.Name, "ACTION", text);

            channel.AddMessage(channel.Server.User, text, MessageSettings.ActionMessage);
            if (userMessage)
            {
                channel.SendAway = false;
                channel.SendBack = false;
            }
        }

        bool StopLoading = false;
        // If we scroll upper the messages, then don't scroll. If we scroll to bottom, scroll the messages
        public void MessageScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var obj = sender as ScrollViewer;
            // Keep scrolling
            if (obj.VerticalOffset == obj.ScrollableHeight)
            {
                Channel ch = (Channel)((ScrollViewer)sender).DataContext;
                if (ch.TheFlowDocument.Blocks.Count > GlobalManager.MaxMessagesDisplayed)
                {
                    while (ch.TheFlowDocument.Blocks.Count > GlobalManager.MaxMessagesDisplayed)
                        ch.TheFlowDocument.Blocks.Remove(ch.TheFlowDocument.Blocks.FirstBlock);

                    ch.MessagesLoadedFrom = ch.Messages.Count - GlobalManager.MaxMessagesDisplayed;
                }

                obj.ScrollToEnd();
            }
            // Load older messages
            else if (obj.VerticalOffset == 0)
            {
                if (!StopLoading)
                {
                    Channel ch = (Channel)((ScrollViewer)sender).DataContext;
                    if (ch.MessagesLoadedFrom != 0)
                    {
                        StopLoading = true;
                        Block first = ch.TheFlowDocument.Blocks.FirstBlock;
                        int loaded = LoadMessages(ch, GlobalManager.NumOfOldMessagesToBeLoaded);
                        double plus = first.Padding.Top + first.Padding.Bottom + first.Margin.Bottom + first.Margin.Top;
                        double sum = 0;
                        Block temp = ch.TheFlowDocument.Blocks.FirstBlock;
                        for (int i = 0; i < loaded; i++)
                        {
                            double maxFontSize = 0;
                            // Get the biggest font size int the paragraph
                            Inline temp2 = ((Paragraph)temp).Inlines.FirstInline;
                            while (temp2 != null)
                            {
                                if (maxFontSize < temp2.FontSize)
                                    maxFontSize = temp.FontSize;
                                temp2 = temp2.NextInline;
                            }
                            sum += maxFontSize + plus;
                            temp = temp.NextBlock;
                            if (temp == null)
                                break;
                        }

                        obj.ScrollToVerticalOffset(sum);
                    }
                }
            }
            else
            {
                StopLoading = false;
            }
            e.Handled = true;
        }

        private int LoadMessages(Channel ch, int count, bool clear = false)
        {
            if (clear)
                ch.TheFlowDocument.Blocks.Clear();

            // select the index from which the messages will be loaded
            int loadFrom = (clear) ? ch.Messages.Count - 1 : ch.MessagesLoadedFrom - 1;

            // load the message backwards
            int k = 0, i = loadFrom;
            for (; i >= 0 && k < count; i--)
            {
                if (!ch.Messages[i].Sender.IsBanned || Properties.Settings.Default.ShowBannedMessages)
                {
                    bool insert = ch.TheFlowDocument.Blocks.Count != 0;
                    if (AddNewMessage(ch, ch.Messages[i], insert))
                    {
                        k++;
                        ch.MessagesLoadedFrom = i;
                    }
                }
            }

            // If we reached last message then we set MessagesLoadedFrom to 0, so this function won't be called again
            if (i == -1)
                ch.MessagesLoadedFrom = 0;

            return k;
        }

        public bool AddNewMessage(Channel ch, MessageClass message, bool insert = false)
        {
            if (Properties.Settings.Default.ChatMode && (
                message.Style.Type == MessageTypes.Part ||
                message.Style.Type == MessageTypes.Join ||
                message.Style.Type == MessageTypes.Quit)
            )
            return false;
                
            try
            {
                Paragraph p = new Paragraph();
                p.Margin = new Thickness(0, 2, 0, 2);
                MessageSettings.LoadSettingsFor(p, message.Style);
                p.Tag = message;
                p.MouseRightButtonDown += InstantColorMenu;

                SolidColorBrush b;
                if (ChoosedColors.TryGetValue(message.Sender.LowerName, out b))
                    p.Foreground = b;
                else if (message.Sender.Group.ID != int.MaxValue)
                    p.Foreground = message.Sender.Group.TextColor;

                // Time when the message arrived
                if (Properties.Settings.Default.MessageTime)
                {
                    Run word = new Run(message.Time.ToString("T") + " ");
                    //MessageSettings.LoadSettingsFor(word, MessageSettings.MessageTimeStyle);
                    p.Inlines.Add(word);
                }

                // Sender of the message
                Run nick = (message.Style.Type == MessageTypes.Action) ? new Run(message.Sender.Name + " ") : new Run(message.Sender.Name + ": ");
                nick.FontWeight = FontWeights.Bold;
                p.Inlines.Add(nick);

                if (insert)
                    ch.TheFlowDocument.Blocks.InsertBefore(ch.TheFlowDocument.Blocks.FirstBlock, p);
                else
                    ch.TheFlowDocument.Blocks.Add(p);

                p = new Paragraph();
                p.Margin = new Thickness(20, 2, 0, 2);

                // Message content
                string[] words = message.Message.Split(' ');
                Uri uri = null;
                sb.Clear(); // this StringBuilder is for minimizing the number on Runs in a paragraph
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i] == ch.Server.User.Name && message.Sender != ch.Server.User) // highlight messsage
                    {
                        // Flush the sb content
                        if (sb.Length > 0)
                        {
                            p.Inlines.Add(new Run(sb.ToString()));
                            sb.Clear();
                        }

                        Italic word = new Italic(new Run(words[i]));
                        p.Inlines.Add(word);
                    }
                    else if (message.HighlightWord == words[i])
                    {
                        // Flush the sb content
                        if (sb.Length > 0)
                        {
                            p.Inlines.Add(new Run(sb.ToString()));
                            sb.Clear();
                        }

                        Run word = new Run(words[i]);
                        MessageSettings.LoadSettingsFor(word, MessageSettings.LeagueFoundMessage);
                        p.Inlines.Add(word);
                    }
                    else if (
                        ( // Links
                        words[i].Length > 6 && words[i].IndexOf("ftp://", 0, 6, StringComparison.OrdinalIgnoreCase) == 0 ||
                        words[i].Length > 7 && words[i].IndexOf("http://", 0, 7, StringComparison.OrdinalIgnoreCase) == 0 ||
                        words[i].Length > 8 && words[i].IndexOf("https://", 0, 8, StringComparison.OrdinalIgnoreCase) == 0
                        ) && Uri.TryCreate(words[i], UriKind.RelativeOrAbsolute, out uri)
                    )
                    {
                        // Flush the sb content
                        if (sb.Length > 0)
                        {
                            p.Inlines.Add(new Run(sb.ToString()));
                            sb.Clear();
                        }

                        Hyperlink word = new Hyperlink(new Run(words[i]));
                        MessageSettings.LoadSettingsFor(word, MessageSettings.HyperLinkStyle);
                        word.NavigateUri = new Uri(words[i]);
                        word.RequestNavigate += OpenURLInBrowser;
                        p.Inlines.Add(word);
                    }
                    else
                    {
                        sb.Append(words[i]);
                    }
                    if (i + 1 < words.Length)
                        sb.Append(' ');
                }

                // Flush the sb content
                if (sb.Length > 0)
                {
                    p.Inlines.Add(new Run(sb.ToString()));
                }

                // Insert the new paragraph
                ch.TheFlowDocument.Blocks.Add(p);

                while (ch.TheFlowDocument.Blocks.Count > GlobalManager.MaxMessagesInMemory)
                {
                    ch.TheFlowDocument.Blocks.Remove(ch.TheFlowDocument.Blocks.FirstBlock);
                }

                return true;
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }
            return false;
        }

        private void InstantColorMenu(object sender, MouseButtonEventArgs e)
        {
            Channel ch = (Channel)((TabItem)Channels.SelectedItem).DataContext;
            if (!ch.TheRichTextBox.Selection.IsEmpty)
                return;

            if (ColorChooser == null)
            {
                ColorChooser = new ContextMenu();

                var def = new MenuItem() { Header = "Default", Foreground = MessageSettings.ChannelMessage.Color, FontWeight = FontWeights.Bold, FontSize = 12 };
                def.Click += RemoveInstantColor;
                ColorChooser.Items.Add(def);

                string[] goodcolors = { "Aquamarine", "Bisque", "BlueViolet", "BurlyWood", "CadetBlue", "Chocolate", "CornflowerBlue", "Gold", "Pink", "Plum", "GreenYellow", "Sienna", "Violet" };
                // populate colors drop down (will work with other kinds of list controls)
                Type colors = typeof(Colors);
                PropertyInfo[] colorInfo = colors.GetProperties(BindingFlags.Public | BindingFlags.Static);
                int found = 0;
                foreach (PropertyInfo info in colorInfo)
                {
                    for (int i = 0; i < goodcolors.Length; i++)
                    {
                        if (info.Name == goodcolors[i])
                        {
                            var color = new SolidColorBrush((Color)info.GetValue(null, null));
                            color.Freeze();
                            var item = new MenuItem() { Header = info.Name, Foreground = color, FontWeight = FontWeights.Bold, FontSize = 12 };
                            item.Click += InstantColorChoosed;
                            ColorChooser.Items.Add(item);

                            found++;
                            break;
                        }
                    }
                    if (found == goodcolors.Length)
                        break;
                }
            }

            Paragraph obj = (Paragraph)sender;
            ColorChooser.Tag = (MessageClass)obj.Tag;
            obj.ContextMenu = ColorChooser;
            obj.ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void RemoveInstantColor(object sender, RoutedEventArgs e)
        {
            MenuItem obj = (MenuItem)sender;
            Client c = ((MessageClass)((ContextMenu)obj.Parent).Tag).Sender;

            ChoosedColors.Remove(c.LowerName);
            ChangeMessageColorForClient(c, null);
        }

        private void InstantColorChoosed(object sender, RoutedEventArgs e)
        {
            MenuItem obj = (MenuItem)sender;
            Client c = ((MessageClass)((ContextMenu)obj.Parent).Tag).Sender;
            SolidColorBrush color = (SolidColorBrush)obj.Foreground;

            if (ChoosedColors.ContainsKey(c.LowerName))
                ChoosedColors[c.LowerName] = color;
            else
                ChoosedColors.Add(c.LowerName, color);

            ChangeMessageColorForClient(c, color);
        }

        private void ChangeMessageColorForClient(Client c, SolidColorBrush color)
        {
            for (int i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].IsRunning)
                {
                    foreach (var item in Servers[i].ChannelList)
                    {
                        Channel ch = item.Value;
                        if (ch.Joined && ch.TheFlowDocument.Blocks.Count > 0)
                        {
                            Paragraph p = (Paragraph)ch.TheFlowDocument.Blocks.FirstBlock;
                            while (p != null)
                            {
                                MessageClass msg = (MessageClass)p.Tag;
                                if (msg.Sender == c)
                                    p.Foreground = (color != null) ? color : msg.Style.Color;
                                p = (Paragraph)p.NextBlock;
                            }
                        }
                    }
                }
            }
        }

        private void OpenURLInBrowser(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.Uri.ToString());
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
            e.Handled = true;
        }
    }
}
