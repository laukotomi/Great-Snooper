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
        private SortedDictionary<Client, Brush> ChoosedColors = new SortedDictionary<Client, Brush>();

        // User messages history
        private void MessagesHistory(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                var obj = sender as TextBox;
                Channel ch = (Channel)obj.Tag;
                if (ch.UserMessageLoadedIdx == -1)
                {
                    ch.TempMessage = obj.Text;
                }
                obj.Text = ch.LoadNextUserMessage();
                obj.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                var obj = sender as TextBox;
                Channel ch = (Channel)obj.Tag;
                string text = ch.LoadPrevUserMessage();
                if (ch.UserMessageLoadedIdx == -1)
                    obj.Text = ch.TempMessage;
                else
                    obj.Text = text;
                obj.SelectAll();
                e.Handled = true;
            }
        }

        // Send a message by the user (action)
        private void MessageSend(object sender, KeyEventArgs e)
        {
            var obj = sender as TextBox;
            if (e.Key == Key.Return && obj.Text.Length > 0)
            {
                // Remove non-wormnet characters
                string message = WormNetCharTable.RemoveNonWormNetChars(obj.Text.TrimEnd());
                if (message.Length > 0)
                {
                    Channel ch = (Channel)obj.Tag;
                    ch.UserMessagesAdd(message);
                    ch.UserMessageLoadedIdx = -1;
                    SendMessageToChannel(message, ch);
                }

                obj.Clear();
                e.Handled = true;
            }
        }

        // Send a message to a channel (+ user functions)
        private void SendMessageToChannel(string textToSend, Channel channel)
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
                if (command == "me" && text.Length > 0)
                {
                    SendActionMessage(text, channel);
                }
                else if (command == "away")
                {
                    AwayText = (text.Length == 0) ? "No reason specified." : text;
                }
                else if (command == "back")
                {
                    if (AwayText == string.Empty)
                        return;

                    AwayText = string.Empty;
                    string backText = (text.Length == 0) ? Properties.Settings.Default.BackText : text;
                    foreach (var item in WormNetM.ChannelList)
                    {
                        if (item.Value.IsPrivMsgChannel && item.Value.AwaySent)
                        {
                            item.Value.AwaySent = false;

                            if (Properties.Settings.Default.SendBack)
                                SendMessageToChannel(backText, item.Value);
                        }
                    }
                }
                else if (command == "gs")
                {
                    var sb = new System.Text.StringBuilder();
                    int count = 0;
                    foreach (var item in WormNetM.Clients)
                    {
                        if (item.Value.ClientGreatSnooper)
                        {
                            if (sb.Length != 0)
                                sb.Append(", ");
                            sb.Append(item.Value.Name);
                            count++;
                        }
                    }
                    MessageBox.Show(this, " Great Snooper is used by " + count + " user(s)! " + sb.ToString(), "Great Snooper check", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (command == "log")
                {
                    string settingsPath = System.IO.Directory.GetParent(System.IO.Directory.GetParent(System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).FullName).FullName;
                    System.Diagnostics.Process.Start(settingsPath);
                }
                else if (command == "news")
                    OpenNewsWindow();
            }
            // Action message
            else if (textToSend[0] == '>')
            {
                string text = textToSend.Substring(1).Trim();
                if (text.Length > 0)
                    SendActionMessage(text, channel);
            }
            // Simple message
            else if (channel != null && channel.Joined)
            {
                string text = textToSend.Trim();
                if (text.Length > 0)
                {
                    WormNetC.Send("PRIVMSG " + channel.Name + " :" + text);
                    channel.AddMessage(GlobalManager.User, text, MessageTypes.User);
                }
            }
        }

        private void SendActionMessage(string text, Channel channel)
        {
            if (channel == null || !channel.Joined || text.Length == 0) return;

            WormNetC.Send("PRIVMSG " + channel.Name + " :" + "\x01" + "ACTION " + text + "\x01");
            channel.AddMessage(GlobalManager.User, text, MessageTypes.Action);
        }

        bool StopLoading = false;
        // If we scroll upper the messages, then don't scroll. If we scroll to bottom, scroll the messages
        private void MessageScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var obj = sender as ScrollViewer;
            if (obj.VerticalOffset == obj.ScrollableHeight)
            {
                Channel ch = (Channel)((ScrollViewer)sender).Tag;
                while (ch.TheFlowDocument.Blocks.Count > GlobalManager.MaxMessagesDisplayed)
                {
                    ch.TheFlowDocument.Blocks.Remove(ch.TheFlowDocument.Blocks.FirstBlock);
                    if (ch.MessagesLoadedFrom + 1 + GlobalManager.MaxMessagesDisplayed < GlobalManager.MaxMessagesInMemory)
                        ch.MessagesLoadedFrom++;
                }
                obj.ScrollToEnd();
            }
            else if (obj.VerticalOffset == 0) // Load older messages
            {
                if (!StopLoading)
                {
                    Channel ch = (Channel)((ScrollViewer)sender).Tag;
                    Block first = ch.TheFlowDocument.Blocks.FirstBlock;
                    if (ch.MessagesLoadedFrom != 0)
                    {
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
                    StopLoading = true;
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

            int loadFrom = (clear) ? ch.Messages.Count - 1 : ch.MessagesLoadedFrom - 1;

            int k = 0, i = loadFrom;
            for (; i >= 0 && k < count; i--)
            {
                if (!ChatMode || ChatMode && ch.Messages[i].MessageType != MessageTypes.Part &&
                    ch.Messages[i].MessageType != MessageTypes.Join &&
                    ch.Messages[i].MessageType != MessageTypes.Quit)
                {
                    k++;

                    if (ch.TheFlowDocument.Blocks.Count == 0)
                        AddNewMessage(ch, ch.Messages[i], false);
                    else
                        AddNewMessage(ch, ch.Messages[i], true);

                    ch.MessagesLoadedFrom = i;
                }
            }

            return k;
        }

        private void AddNewMessage(Channel ch, MessageClass message, bool insert = false, bool LeagueFound = false)
        {
            if (ChatMode && (
                message.MessageType == MessageTypes.Part ||
                message.MessageType == MessageTypes.Join ||
                message.MessageType == MessageTypes.Quit) ||
                message.Sender.IsBanned
            )
            return;
                
            try
            {
                Paragraph p = new Paragraph();
                p.Margin = new Thickness(0, 2, 0, 2);
                MessageSettings.LoadSettingsFor(p, MessageSettings.Settings[message.MessageType]);
                p.TextDecorations = MessageSettings.Settings[message.MessageType].textdecorations;
                p.Tag = message;
                p.MouseRightButtonDown += InstantColorMenu;

                Brush b;
                if (ChoosedColors.TryGetValue(message.Sender, out b))
                    p.Foreground = b;

                // Time when the message arrived
                if (Properties.Settings.Default.MessageTime)
                {
                    Run word = new Run(message.Time.ToString("T") + " ");
                    MessageSettings.LoadSettingsFor(word, MessageSettings.MessageTime);
                    word.TextDecorations = MessageSettings.MessageTime.textdecorations;
                    p.Inlines.Add(word);
                }

                // Sender of the message
                if (message.MessageType == MessageTypes.Action)
                {
                    Run nick = new Run(message.Sender.Name + " ");
                    nick.FontWeight = FontWeights.Bold;
                    p.Inlines.Add(nick);
                }
                else
                {
                    Run nick = new Run(message.Sender.Name + ": ");
                    nick.FontWeight = FontWeights.Bold;
                    p.Inlines.Add(nick);
                }

                // Message content
                string[] words = message.Message.Split(' ');
                Uri uri = null;
                sb.Clear(); // this StringBuilder is for minimizing the number on Runs in a paragraph
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i] == GlobalManager.User.Name && message.Sender != GlobalManager.User) // highlight messsage
                    {
                        // Flush the sb content
                        if (sb.Length > 0)
                        {
                            p.Inlines.Add(new Run(sb.ToString()));
                            sb.Clear();
                        }

                        Run word = new Run(words[i]);
                        word.FontStyle = FontStyles.Italic;
                        p.Inlines.Add(word);
                    }
                    else if (
                        ( // Links
                        words[i].Length > 7 && words[i].IndexOf("http://", 0, 7, StringComparison.OrdinalIgnoreCase) == 0 ||
                        words[i].Length > 8 && words[i].IndexOf("https://", 0, 8, StringComparison.OrdinalIgnoreCase) == 0 ||
                        words[i].Length > 6 && words[i].IndexOf("ftp://", 0, 6, StringComparison.OrdinalIgnoreCase) == 0
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
                        MessageSettings.LoadSettingsFor(word, MessageSettings.Hyperlink);
                        word.TextDecorations = MessageSettings.Hyperlink.textdecorations;
                        word.NavigateUri = new Uri(words[i]);
                        word.RequestNavigate += OpenURLInBrowser;
                        word.Unloaded += HyperlinkUnloaded;
                        p.Inlines.Add(word);
                    }
                    else
                    {
                        if (LeagueFound && FoundUsers.ContainsKey(words[i].ToLower()))
                        {
                            // Flush the sb content
                            if (sb.Length > 0)
                            {
                                p.Inlines.Add(new Run(sb.ToString()));
                                sb.Clear();
                            }

                            Run word = new Run(words[i]);
                            MessageSettings.LoadSettingsFor(word, MessageSettings.LeagueFound);
                            word.TextDecorations = MessageSettings.LeagueFound.textdecorations;
                            p.Inlines.Add(word);
                        }
                        else
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
                if (insert)
                    ch.TheFlowDocument.Blocks.InsertBefore(ch.TheFlowDocument.Blocks.FirstBlock, p);
                else
                    ch.TheFlowDocument.Blocks.Add(p);

                while (ch.TheFlowDocument.Blocks.Count > GlobalManager.MaxMessagesInMemory)
                {
                    ch.TheFlowDocument.Blocks.Remove(ch.TheFlowDocument.Blocks.FirstBlock);
                }
            }
            catch (Exception e)
            {
                ErrorLog.log(e);
            }
        }

        private void InstantColorMenu(object sender, MouseButtonEventArgs e)
        {
            Channel ch = (Channel)((TabItem)Channels.SelectedItem).Tag;
            if (!ch.TheRichTextBox.Selection.IsEmpty)
                return;

            if (ColorChooser == null)
            {
                ColorChooser = new ContextMenu();

                var def = new MenuItem() { Header = "Default", Foreground = MessageSettings.Settings[MessageTypes.Channel].color, FontWeight = FontWeights.Bold, FontSize = 12 };
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
                            var item = new MenuItem() { Header = info.Name, Foreground = new SolidColorBrush((Color)info.GetValue(null, null)), FontWeight = FontWeights.Bold, FontSize = 12 };
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
            ChoosedColors.Remove(c);

            foreach (var item in WormNetM.ChannelList)
            {
                Channel ch = item.Value;
                if (ch.TheFlowDocument.Blocks.Count > 0)
                {
                    Paragraph p = (Paragraph)ch.TheFlowDocument.Blocks.FirstBlock;
                    while (p != null)
                    {
                        MessageClass msg = (MessageClass)p.Tag;
                        if (msg.Sender == c)
                            p.Foreground = MessageSettings.Settings[msg.MessageType].color;
                        p = (Paragraph)p.NextBlock;
                    }
                }
            }
        }

        private void InstantColorChoosed(object sender, RoutedEventArgs e)
        {
            MenuItem obj = (MenuItem)sender;
            Client c = ((MessageClass)((ContextMenu)obj.Parent).Tag).Sender;
            Brush b;
            if (ChoosedColors.TryGetValue(c, out b))
                ChoosedColors[c] = b;
            else
                ChoosedColors.Add(c, obj.Foreground);

            foreach (var item in WormNetM.ChannelList)
            {
                Channel ch = item.Value;
                if (ch.TheFlowDocument.Blocks.Count > 0)
                {
                    Paragraph p = (Paragraph)ch.TheFlowDocument.Blocks.FirstBlock;
                    while (p != null)
                    {
                        MessageClass msg = (MessageClass)p.Tag;
                        if (msg.Sender == c)
                            p.Foreground = obj.Foreground;
                        p = (Paragraph)p.NextBlock;
                    }
                }
            }
        }


        // Unload subscribed hyperlink events
        private void HyperlinkUnloaded(object sender, RoutedEventArgs e)
        {
            var obj = sender as Hyperlink;
            obj.RequestNavigate -= OpenURLInBrowser;
            obj.Unloaded -= HyperlinkUnloaded;
            e.Handled = true;
        }

        private void OpenURLInBrowser(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.Uri.ToString());
            }
            catch (Exception ex)
            {
                ErrorLog.log(ex);
            }
            e.Handled = true;
        }
    }
}
