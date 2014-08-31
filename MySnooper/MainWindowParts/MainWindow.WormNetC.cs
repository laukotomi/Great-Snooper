using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        private IRCCommunicator WormNetC;
        private System.Threading.Thread IrcThread;


        // Offline user notification arrived
        private void OfflineUser(string name)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                WormNetM.OfflineUserPrivChat(name);
            }
            ));
        }

        // Message arrived
        private void MessageToChannel(string clientName, string to, string message, MessageTypes messageType)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                string fromLow = clientName.ToLower();

                Client c;
                if (!WormNetM.Clients.TryGetValue(fromLow, out c))
                {
                    c = new Client(clientName, null, "", 0, false);
                    c.IsBanned = WormNetM.IsBanned(fromLow);
                    c.IsBuddy = WormNetM.IsBuddy(fromLow);
                    c.OnlineStatus = 2;
                }

                Channel ch = null;
                string toLow = to.ToLower();
                foreach (var item in WormNetM.ChannelList)
                {
                    if (item.Value.LowerName == toLow || item.Value.LowerName == fromLow) // message to a channel || private message (we have a private chat tab)
                    {
                        ch = item.Value;
                        break;
                    }
                }

                if (ch == null) // new private message channel
                {
                    ch = new Channel(clientName, "Chat with " + clientName, c);
                    ch.NewMessageAdded += AddNewMessage;
                    WormNetM.ChannelList.Add(ch.LowerName, ch);

                    MakeConnectedLayout(ch);
                    if (!c.IsBanned)
                        AddToChannels(ch);
                }
                else if (!ch.Joined)
                    return;

                bool leagueFound = false;

                if (!ch.IsPrivMsgChannel)
                {
                    bool LookForLeague = SearchHere != null && SearchHere == ch;
                    string[] words = message.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < words.Length; i++)
                    {
                        if (messageType == MessageTypes.Channel && words[i] == GlobalManager.User.Name)
                        {
                            Highlight(ch);
                        }
                        else if (LookForLeague)
                        {
                            string lower = words[i].ToLower();
                            if (FoundUsers.ContainsKey(lower) && !FoundUsers[lower].Contains(c.LowerName))
                            {
                                FoundUsers[lower].Add(c.LowerName);
                                leagueFound = true;
                                this.FlashWindow();

                                if (Properties.Settings.Default.LeagueFoundBeepEnabled && SoundEnabled && LeagueGameFoundBeep != null)
                                {
                                    try
                                    {
                                        LeagueGameFoundBeep.Play();
                                    }
                                    catch (Exception ex)
                                    {
                                        ErrorLog.log(ex);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                else if (Channels.SelectedItem != null && !c.IsBanned)
                {
                    // Private message arrived notification
                    Channel selectedCH = (Channel)((TabItem)Channels.SelectedItem).Tag;
                    if (ch.BeepSoundPlay && (ch != selectedCH || !IsWindowFocused))
                    {
                        ch.NewMessages = true;
                        ch.BeepSoundPlay = false;
                        this.FlashWindow();

                        if (Properties.Settings.Default.PMBeepEnabled && SoundEnabled && PrivateMessageBeep != null)
                        {
                            try
                            {
                                PrivateMessageBeep.Play();
                            }
                            catch (Exception ex)
                            {
                                ErrorLog.log(ex);
                            }
                        }
                    }

                    // Send back away message if needed
                    if (AwayText != string.Empty && !ch.AwaySent && (selectedCH != ch || !IsWindowFocused))
                    {
                        SendMessageToChannel(AwayText, ch);
                        ch.AwaySent = true;
                    }
                }

                ch.AddMessage(c, message, messageType, leagueFound);
            }
            ));
        }

        // When a client left the server
        private void Quitter(string clientName, string message)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                WormNetM.QuittedChannel(clientName, message);
                TusUsers.Remove(clientName.ToLower());
                UpdateDescription();
            }
            ));
        }

        // When a client parts a channel
        private void Parted(string channelName, string clientName)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                Client c = WormNetM.PartedChannel(channelName, clientName); // returns the client if it was removed completely
                if (c != null)
                    TusUsers.Remove(c.LowerName);
                UpdateDescription();
            }
            ));
        }

        // When a client joins a channel
        private void Joined(string channelName, string clientName, string clan)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                bool buddyJoined = false;
                bool userjoined = WormNetM.JoinedChannel(channelName, clientName, clan, ref buddyJoined);
                if (userjoined)
                {
                    GameListForce = true;
                }

                if (buddyJoined && Properties.Settings.Default.BJBeepEnabled && SoundEnabled && BuddyOnlineBeep != null)
                {
                    try
                    {
                        BuddyOnlineBeep.Play();
                    }
                    catch (Exception e)
                    {
                        ErrorLog.log(e);
                    }
                }
                UpdateDescription();

                if (userjoined && Channels.SelectedItem != null)
                {
                    Channel selectedCH = (Channel)((TabItem)Channels.SelectedItem).Tag;
                    Channel ch = WormNetM.ChannelList[channelName];
                    ch.TheTabItem.UpdateLayout();
                    ch.TheTextBox.Focus();
                }
            }
            ));
        }

        private void MakeDisConnectedLayout(Channel ch)
        {
            ch.DisconnectedLayout = (Border)XamlReader.Parse(
               " <Border BorderThickness=\"0,1,0,0\" BorderBrush=\"Gray\">" +
               "     <StackPanel Margin=\"0, 20, 0, 0\">" +
               "         <Border>" +
               "             <Label Content=\"{Binding Path=Description, Mode=OneWay}\" FontSize=\"14\" HorizontalAlignment=\"Center\" />" +
               "         </Border>" +
               "         <Button Content=\"Enter this channel\" Width=\"180\" HorizontalAlignment=\"Center\" Focusable=\"false\" />" +
               "     </StackPanel>" +
               " </Border>"
            , GlobalManager.XamlContext);
            ch.DisconnectedLayout.DataContext = ch;
            Button bt = (Button)((StackPanel)ch.DisconnectedLayout.Child).Children[1];
            bt.Click += Enter_Channel;
            bt.Tag = ch;
        }

        private void MakeConnectedLayout(Channel ch)
        {
            ch.ConnectedLayout = (Border)XamlReader.Parse(
                " <Border>" +
                "     <Grid>" +
                "         <Grid.RowDefinitions>" +
                "             <RowDefinition Height=\"*\" />" +
                "             <RowDefinition Height=\"Auto\" />" +
                "         </Grid.RowDefinitions>" +
                "         <Border Grid.Row=\"0\" BorderThickness=\"0,1\" BorderBrush=\"Gray\">" +
                "             <ScrollViewer Margin=\"0,5\">" +
                "                 <RichTextBox IsReadOnly=\"True\" BorderThickness=\"0\" IsDocumentEnabled=\"True\" Background=\"Transparent\">" +
                "                     <FlowDocument>" +
                "                         <FlowDocument.Resources>" +
                "                             <Style TargetType=\"{x:Type Paragraph}\">" +
                "                                 <Setter Property=\"Margin\" Value=\"0,2\" />" +
                "                             </Style>" +
                "                         </FlowDocument.Resources>" +
                "                     </FlowDocument>" +
                "                 </RichTextBox>" +
                "             </ScrollViewer>" +
                "         </Border>" +
                "         <TextBox Grid.Row=\"1\" Margin=\"0,8,0,2\" TextWrapping=\"Wrap\" MaxLength=\"495\" MinLines=\"1\" MaxLines=\"1\" Background=\"Transparent\" />" +
                "     </Grid>" +
                " </Border>"
            , GlobalManager.XamlContext);

            ScrollViewer sw = (ScrollViewer)((Border)((Grid)ch.ConnectedLayout.Child).Children[0]).Child;
            sw.ScrollChanged += MessageScrollChanged;
            sw.Tag = ch;
            ch.TheRichTextBox = (RichTextBox)sw.Content;
            ch.TheFlowDocument = (FlowDocument)ch.TheRichTextBox.Document;
            TextBox tb = (TextBox)((Grid)ch.ConnectedLayout.Child).Children[1];
            ch.TheTextBox = tb;
            tb.KeyDown += MessageSend;
            tb.PreviewKeyDown += MessagesHistory;
            tb.Tag = ch;
        }

        private void MakeGameListTabItem(Channel ch)
        {
            TabItem ti = new TabItem();
            Border tiConent = (Border)XamlReader.Parse(
                " <Border>" +
                "     <ListBox HorizontalContentAlignment=\"Stretch\" Background=\"Transparent\">" +
                "       <ListBox.ItemTemplate>" +
                "          <DataTemplate>" +
                "              <Grid Background=\"Transparent\">" +
                "                  <Grid.ColumnDefinitions>" +
                "                      <ColumnDefinition Width=\"22\"></ColumnDefinition>" +
                "                      <ColumnDefinition Width=\"22\"></ColumnDefinition>" +
                "                      <ColumnDefinition Width=\"240\"></ColumnDefinition>" +
                "                      <ColumnDefinition Width=\"150\"></ColumnDefinition>" +
                "                  </Grid.ColumnDefinitions>" +
                "                  <Image Grid.Column=\"0\" Source=\"{Binding Path=Locked, Mode=OneWay}\" Width=\"16\" Height=\"16\" Margin=\"0,0,6,0\"></Image>" +
                "                  <Image Grid.Column=\"1\" Source=\"{Binding Path=Country.Flag, Mode=OneWay}\" ToolTip=\"{Binding Path=Country.Name, Mode=OneWay}\" Width=\"22\" Height=\"18\"></Image>" +
                "                  <Label Grid.Column=\"2\" FontSize=\"13\" HorizontalAlignment=\"Left\" Foreground=\"White\" Content=\"{Binding Path=Name, Mode=OneWay}\"></Label>" +
                "                  <Label Grid.Column=\"3\" FontSize=\"13\" HorizontalAlignment=\"Left\" Foreground=\"White\" Content=\"{Binding Path=Hoster, Mode=OneWay}\"></Label>" +
                "              </Grid>" +
                "          </DataTemplate>" +
                "      </ListBox.ItemTemplate>" +
                "      <ListBox.ContextMenu>" +
                "          <ContextMenu>" +
                "              <MenuItem Header=\"Join this game\"></MenuItem>" +
                "              <MenuItem Header=\"Silent join\"></MenuItem>" +
                "              <MenuItem Header=\"Join and close snooper\"></MenuItem>" +
                "              <MenuItem Header=\"Silent join and close snooper\"></MenuItem>" +
                "          </ContextMenu>" +
                "      </ListBox.ContextMenu>" +
                "  </ListBox>" +
                " </Border>"
            , GlobalManager.XamlContext);

            ti.Content = tiConent;

            ListBox lb = (ListBox)tiConent.Child;
            lb.MouseDoubleClick += GameDoubleClick;
            lb.SelectionChanged += NoSelectionChange;
            lb.LostFocus += GameList_LostFocus;
            Binding b = new Binding();
            b.Source = ch.GameList;
            b.Mode = BindingMode.OneWay;
            lb.SetBinding(ListBox.ItemsSourceProperty, b);

            ((MenuItem)lb.ContextMenu.Items[0]).Click += JoinGameClick;
            ((MenuItem)lb.ContextMenu.Items[0]).Tag = lb;
            ((MenuItem)lb.ContextMenu.Items[1]).Click += SilentJoin;
            ((MenuItem)lb.ContextMenu.Items[1]).Tag = lb;
            ((MenuItem)lb.ContextMenu.Items[2]).Click += JoinAndClose;
            ((MenuItem)lb.ContextMenu.Items[2]).Tag = lb;
            ((MenuItem)lb.ContextMenu.Items[3]).Click += SilentJoinAndClose;
            ((MenuItem)lb.ContextMenu.Items[3]).Tag = lb;

            GameList.Items.Add(ti);
        }

        private void MakeUserListTemplate(Channel ch)
        {
            TabItem ti = new TabItem();
            DataGrid dg = (DataGrid)XamlReader.Parse(
                " <DataGrid CanUserResizeRows=\"False\" Margin=\"0\" Padding=\"0\" CanUserAddRows=\"False\" CanUserDeleteRows=\"False\" Background=\"Transparent\" AutoGenerateColumns=\"False\" SelectionMode=\"Single\">" +
                "     <DataGrid.ContextMenu>" +
                "         <ContextMenu>" +
                "             <MenuItem Name=\"Chat\" Header=\"Chat with this user\"></MenuItem>" +
                "             <MenuItem Name=\"Buddy\"></MenuItem>" +
                "             <MenuItem Name=\"Ignore\"></MenuItem>" +
                "         </ContextMenu>" +
                "     </DataGrid.ContextMenu>" +
                "     <DataGrid.Columns>" +
                "         <DataGridTemplateColumn Header=\"C.\" IsReadOnly=\"True\" SortMemberPath=\"Country\" Width=\"32\">" +
                "             <DataGridTemplateColumn.CellTemplate>" +
                "                 <DataTemplate>" +
                "                     <Image HorizontalAlignment=\"Left\" Margin=\"4,0,0,0\" VerticalAlignment=\"Center\" ToolTip=\"{Binding Path=Country.Name, Mode=OneWay}\" Source=\"{Binding Path=Country.Flag, Mode=OneWay}\" Width=\"22\" Height=\"18\"></Image>" +
                "                 </DataTemplate>" +
                "             </DataGridTemplateColumn.CellTemplate>" +
                "         </DataGridTemplateColumn>" +
                "         <DataGridTemplateColumn Header=\"Rank\" IsReadOnly=\"True\" SortMemberPath=\"Rank\" Width=\"58\">" +
                "             <DataGridTemplateColumn.CellTemplate>" +
                "                 <DataTemplate>" +
                "                     <Image HorizontalAlignment=\"Left\" VerticalAlignment=\"Center\" ToolTip=\"{Binding Path=Rank.Name, Mode=OneWay}\" Source=\"{Binding Path=Rank.Picture, Mode=OneWay}\" Width=\"48\" Height=\"17\"></Image>" +
                "                 </DataTemplate>" +
                "             </DataGridTemplateColumn.CellTemplate>" +
                "         </DataGridTemplateColumn>" +
                "         <DataGridTemplateColumn Header=\"Nick\" IsReadOnly=\"True\" SortMemberPath=\"Name\" Width=\"3*\">" +
                "             <DataGridTemplateColumn.CellTemplate>" +
                "                 <DataTemplate>" +
                "                     <TextBlock HorizontalAlignment=\"Left\" VerticalAlignment=\"Center\" Foreground=\"AliceBlue\" FontSize=\"13\" Text=\"{Binding Path=Name, Mode=OneWay}\" Style=\"{DynamicResource ClientNameStyle}\">" +
                "                         <TextBlock.Resources>" +
                "                             <Style x:Key=\"ClientNameStyle\" TargetType=\"TextBlock\">" +
                "                                 <Style.Triggers>" +
                "                                    <DataTrigger Binding=\"{Binding Path=TusActive}\" Value=\"true\">" +
                "                                        <Setter Property=\"TextDecorations\" Value=\"Underline\" />" +
                "                                    </DataTrigger>" +
                "                                 </Style.Triggers>" +
                "                             </Style>" +
                "                         </TextBlock.Resources>" +
                "                     </TextBlock>" +
                "                 </DataTemplate>" +
                "             </DataGridTemplateColumn.CellTemplate>" +
                "         </DataGridTemplateColumn>" +
                "         <DataGridTemplateColumn Header=\"Clan\" IsReadOnly=\"True\" SortMemberPath=\"Clan\" Width=\"2*\">" +
                "             <DataGridTemplateColumn.CellTemplate>" +
                "                 <DataTemplate>" +
                "                     <Label HorizontalAlignment=\"Left\" Foreground=\"AliceBlue\" VerticalAlignment=\"Center\" FontSize=\"11\" Content=\"{Binding Path=Clan, Mode=OneWay}\"></Label>" +
                "                 </DataTemplate>" +
                "             </DataGridTemplateColumn.CellTemplate>" +
                "         </DataGridTemplateColumn>" +
                "     </DataGrid.Columns>" +
                " </DataGrid>"
            , GlobalManager.XamlContext);

            ch.TheDataGrid = dg;
            dg.RowStyle = (Style)UserList.FindResource("DataGridRowStyle");
            dg.ColumnHeaderStyle = (Style)UserList.FindResource("DataGridColumnHeaderStyle");
            ti.Content = dg;
            dg.LostFocus += ClientList_LostFocus;
            dg.MouseDoubleClick += PrivateMessageClick;
            dg.SelectionChanged += NoSelectionChange;
            ((MenuItem)dg.ContextMenu.Items[0]).Click += PrivateMessageClick2;
            ((MenuItem)dg.ContextMenu.Items[1]).Click += AddOrRemoveBuddy;
            ((MenuItem)dg.ContextMenu.Items[2]).Click += AddOrRemoveBan;
            dg.ContextMenuOpening += ContextMenuBuilding;
            dg.ContextMenuClosing += ContextMenuClear;
            Binding b = new Binding();
            b.Source = ch.Clients;
            b.Mode = BindingMode.OneWay;
            dg.SetBinding(DataGrid.ItemsSourceProperty, b);

            UserList.Items.Add(ti);
        }

        private void AddToChannels(Channel ch)
        {
            TabItem ti = new TabItem();
            ti.Tag = ch;
            ti.DataContext = ch;
            ti.Header = ch.Name;
            if (ch.IsPrivMsgChannel)
            {
                ti.Content = ch.ConnectedLayout;
                ti.Style = (Style)Channels.FindResource("PrivMsgTabItem");
            }
            else
            {
                ti.Content = ch.DisconnectedLayout;
                ti.Style = (Style)Channels.FindResource("ChannelTabItem");
            }
            Channels.Items.Add(ti);
            ch.TheTabItem = ti;
        }


        private void ListEnd(SortedDictionary<string, string> channelList)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                foreach (var item in channelList)
                {
                    Channel ch = new Channel(item.Key, item.Value);
                    ch.NewMessageAdded += AddNewMessage;
                    ch.ChannelLeaving += ChannelLeaving;

                    MakeDisConnectedLayout(ch);
                    MakeConnectedLayout(ch);
                    MakeGameListTabItem(ch);
                    MakeUserListTemplate(ch);
                    WormNetM.ChannelList.Add(ch.LowerName, ch);
                    AddToChannels(ch);
                }

                if (Channels.SelectedIndex != 0 && Channels.Items.Count > 0)
                    Channels.SelectedIndex = 0;
            }
            ));

            if (Properties.Settings.Default.AutoJoinAnythingGoes)
                WormNetC.Send("JOIN #AnythingGoes");
        }


        // Information about a client
        private void Client(string channelName, string clientName, CountryClass country, string clan, int rank, bool ClientGreatSnooper)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                WormNetM.AddClient(channelName, clientName, country, clan, rank, ClientGreatSnooper);
                UpdateDescription();
            }
            ));
        }
    }
}
