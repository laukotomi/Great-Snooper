using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        public Border MakeDisConnectedLayout(Channel ch)
        {
            Border border = (Border)XamlReader.Parse(
               " <Border BorderThickness=\"0,1,0,0\" BorderBrush=\"Gray\">" +
               "     <StackPanel Margin=\"0, 20, 0, 0\">" +
               "         <Border>" +
               "             <Label Content=\"{Binding Path=Description, Mode=OneTime}\" FontSize=\"14\" HorizontalAlignment=\"Center\" />" +
               "         </Border>" +
               "         <Button Content=\"Enter this channel\" Width=\"180\" HorizontalAlignment=\"Center\" Focusable=\"false\" />" +
               "     </StackPanel>" +
               " </Border>"
            , GlobalManager.XamlContext);
            StackPanel sp = (StackPanel)border.Child;
            Button bt = (Button)sp.Children[1];
            bt.Click += EnterChannel;

            var progressRing = new MahApps.Metro.Controls.ProgressRing();
            progressRing.IsActive = false;
            progressRing.Foreground = Brushes.LightBlue;
            progressRing.Style = (Style)FindResource("ProgressRingStyle");
            progressRing.Tag = "Connecting";
            sp.Children.Add(progressRing);

            return border;
        }

        private void EnterChannel(object sender, RoutedEventArgs e)
        {
            Channel ch = (Channel)((Button)sender).DataContext;
            if (!ch.Joined)
            {
                ch.Loading(true);
                if (ch.Server.IsWormNet)
                    ch.Server.JoinChannel(ch.Name);
                else
                {
                    gameSurgeIsConnected = true;
                    ch.Server.Connect();
                }
            }
            e.Handled = true;
        }

        public Border MakeConnectedLayout()
        {
            int tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Border border = (Border)XamlReader.Parse(
                " <Border>" +
                "     <Grid>" +
                "         <Grid.RowDefinitions>" +
                "             <RowDefinition Height=\"*\" />" +
                "             <RowDefinition Height=\"Auto\" />" +
                "         </Grid.RowDefinitions>" +
                "         <Border Grid.Row=\"0\" BorderThickness=\"0,1\" BorderBrush=\"Gray\">" +
                "             <ScrollViewer Margin=\"0,5\">" +
                "                 <RichTextBox IsUndoEnabled=\"False\" IsReadOnly=\"True\" BorderThickness=\"0\" IsDocumentEnabled=\"True\" Background=\"Transparent\">" +
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
                "         <TextBox Grid.Row=\"1\" Margin=\"0,8,0,2\" MaxLength=\"495\" Background=\"Transparent\" />" +
                "     </Grid>" +
                " </Border>"
            , GlobalManager.XamlContext);

            ScrollViewer sw = (ScrollViewer)((Border)((Grid)border.Child).Children[0]).Child;
            sw.ScrollChanged += MessageScrollChanged;

            TextBox tb = (TextBox)((Grid)border.Child).Children[1];
            tb.KeyDown += MessageSend;
            tb.PreviewKeyDown += MessagesHistory;

            return border;
        }

        public DataGrid MakeUserListTemplate()
        {
            DataGrid dg = (DataGrid)XamlReader.Parse(
                " <DataGrid CanUserResizeRows=\"False\" Margin=\"0\" Padding=\"0\" CanUserAddRows=\"False\" EnableColumnVirtualization=\"True\" EnableRowVirtualization=\"True\" CanUserDeleteRows=\"False\" Background=\"Transparent\" AutoGenerateColumns=\"False\" SelectionMode=\"Single\">" +
                "     <DataGrid.ContextMenu>" +
                "         <ContextMenu>" +
                "             <MenuItem Name=\"Chat\" Header=\"Chat with this user\"></MenuItem>" +
                "             <MenuItem Name=\"Conversation\"></MenuItem>" +
                "             <MenuItem Name=\"Buddy\"></MenuItem>" +
                "             <MenuItem Name=\"Ignore\"></MenuItem>" +
                "             <MenuItem Name=\"TUS\"></MenuItem>" +
                "             <MenuItem Name=\"Info\"></MenuItem>" +
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

            dg.RowStyle = (Style)UserList.FindResource("DataGridRowStyle");
            dg.ColumnHeaderStyle = (Style)UserList.FindResource("DataGridColumnHeaderStyle");
            dg.LostFocus += ClientList_LostFocus;
            dg.MouseDoubleClick += PrivateMessageClick;
            dg.SelectionChanged += NoSelectionChange;
            dg.Sorting += dg_Sorting;
            ((MenuItem)dg.ContextMenu.Items[0]).Click += PrivateMessageClick2;
            ((MenuItem)dg.ContextMenu.Items[1]).Click += AddOrRemoveClientConversation;
            ((MenuItem)dg.ContextMenu.Items[2]).Click += AddOrRemoveBuddy;
            ((MenuItem)dg.ContextMenu.Items[3]).Click += AddOrRemoveBan;
            ((MenuItem)dg.ContextMenu.Items[4]).Click += WiewTusProfile;
            dg.ContextMenuOpening += ContextMenuBuilding;

            return dg;
        }

        void dg_Sorting(object sender, DataGridSortingEventArgs e)
        {
            Channel ch = (Channel)((DataGrid)sender).DataContext;
            if (!ch.Server.IsWormNet)
                return;

            e.Handled = true;
            if (!e.Column.SortDirection.HasValue || e.Column.SortDirection.Value == System.ComponentModel.ListSortDirection.Descending)
            {
                Properties.Settings.Default.ColumnOrder = e.Column.Header.ToString() + "|A";
                e.Column.SortDirection = System.ComponentModel.ListSortDirection.Ascending;
            }
            else
            {
                Properties.Settings.Default.ColumnOrder = e.Column.Header.ToString() + "|D";
                e.Column.SortDirection = System.ComponentModel.ListSortDirection.Descending;
            }
            Properties.Settings.Default.Save();

            foreach (var item in servers[0].ChannelList)
            {
                if (!item.Value.IsPrivMsgChannel)
                {
                    SetOrderForDataGrid(item.Value, e.Column.Header.ToString(), e.Column.SortDirection.Value);
                }
            }
        }

        public void SetOrderForDataGrid(Channel ch, string columnName, System.ComponentModel.ListSortDirection direction)
        {
            if (ch.TheDataGrid.ItemsSource != null)
            {
                var view = CollectionViewSource.GetDefaultView(ch.TheDataGrid.ItemsSource);
                if (view != null)
                {
                    view.SortDescriptions.Clear();
                    if (columnName != "Nick")
                    {
                        switch (columnName)
                        {
                            case "C.":
                                view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Country", direction));
                                break;
                            case "Rank":
                                view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Rank", direction));
                                break;
                            case "Clan":
                                view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Clan", direction));
                                break;
                        }
                        view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
                    }
                    else
                        view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", direction));
                }
            }

            foreach (DataGridColumn column in ch.TheDataGrid.Columns)
            {
                if (column.Header.ToString() == columnName)
                    column.SortDirection = direction;
                else
                    column.SortDirection = null;
            }
        }

        private void ClientList_LostFocus(object sender, RoutedEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            dg.SelectedIndex = -1;
            e.Handled = true;
        }

        private void PrivateMessageClick2(object sender, RoutedEventArgs e)
        {
            Channel ch = (Channel)((MenuItem)sender).DataContext;
            Client c = (Client)((MenuItem)sender).Tag;
            OpenPrivateChat(c, ch.Server);
            e.Handled = true;
        }

        private void AddOrRemoveClientConversation(object sender, RoutedEventArgs e)
        {
            object[] par = (object[])((MenuItem)sender).Tag;
            Channel ch = (Channel)par[1];
            Client c = (Client)par[0];

            if (ch.IsInConversation(c))
            {
                ch.RemoveClientFromConversation(c);
            }
            else
            {
                ch.AddClientToConversation(c);
            }
        }


        // If we open a private chat
        private void PrivateMessageClick(object sender, MouseButtonEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            if (dg.SelectedItem != null)
            {
                Channel ch = (Channel)dg.DataContext;
                Client c = dg.SelectedItem as Client;
                if (c.LowerName != ch.Server.User.LowerName)
                {
                    OpenPrivateChat(c, ch.Server);
                }
            }
            e.Handled = true;
        }


        public TabItem MakeGameListTabItem(Channel channel)
        {
            TabItem ti = new TabItem();
            Grid grid = (Grid)XamlReader.Parse(
                " <Grid>" +
                "  <Grid.RowDefinitions>" +
                "   <RowDefinition Height=\"34\" />" +
                "   <RowDefinition Height=\"3\" />" +
                "   <RowDefinition Height=\"*\" />" +
                "  </Grid.RowDefinitions>" +
                "  <Border Grid.Row=\"0\" BorderThickness=\"1,0,1,1\" BorderBrush=\"Gray\" Background=\"#123456\">" +
                "   <DockPanel>" +
                "    <Canvas DockPanel.Dock=\"Left\" Visibility=\"{Binding Path=CanHostVisibility, Mode=OneWay}\">" +
                "     <StackPanel Canvas.Left=\"0\" Orientation=\"Horizontal\">" +
                "      <Button Content=\"Host a game\" Focusable=\"False\" Height=\"33\" Background=\"Black\" BorderThickness=\"0,0,1,0\" Padding=\"15,0\" />" +
                "      <Button Content=\"Refresh game list\" Focusable=\"False\" Height=\"33\" Background=\"Black\" BorderThickness=\"0,0,1,0\" Padding=\"15,0\" />" +
                "     </StackPanel>" +
                "    </Canvas>" +
                "    <Canvas DockPanel.Dock=\"Right\" Visibility=\"{Binding Path=LeaveChannelVisibility, Mode=OneWay}\">" +
                "     <Button Canvas.Right=\"0\" Height=\"33\" Background=\"Black\" BorderThickness=\"1,0,0,0\" Content=\"Leave this channel\" Focusable=\"False\" Padding=\"15,0\" />" +
                "    </Canvas>" +
                "    <StackPanel Orientation=\"Horizontal\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\">" +
                "     <TextBlock /> " +
                "     <TextBlock Visibility=\"{Binding Path=LeaveChannelVisibility, Mode=OneWay}\" Text=\" | \" /> " +
                "     <TextBlock Visibility=\"{Binding Path=LeaveChannelVisibility, Mode=OneWay}\" /> " +
                "     <TextBlock Visibility=\"{Binding Path=LeaveChannelVisibility, Mode=OneWay}\" Text=\" Users\" /> " +
                "     <TextBlock Visibility=\"{Binding Path=CanHostVisibility, Mode=OneWay}\" Text=\" | \" /> " +
                "     <TextBlock Visibility=\"{Binding Path=CanHostVisibility, Mode=OneWay}\" /> " +
                "     <TextBlock Visibility=\"{Binding Path=CanHostVisibility, Mode=OneWay}\" Text=\" Games\" /> " +
                "    </StackPanel>" +
                "   </DockPanel>" +
                "  </Border>" +
                "  <Border Grid.Row=\"2\">" +
                "   <ListBox HorizontalContentAlignment=\"Stretch\" Background=\"Transparent\">" +
                "    <ListBox.ItemTemplate>" +
                "     <DataTemplate>" +
                "      <Grid Background=\"Transparent\">" +
                "       <Grid.ColumnDefinitions>" +
                "        <ColumnDefinition Width=\"22\" />" +
                "        <ColumnDefinition Width=\"22\" />" +
                "        <ColumnDefinition Width=\"240\" />" +
                "        <ColumnDefinition Width=\"150\" />" +
                "       </Grid.ColumnDefinitions>" +
                "       <Image Grid.Column=\"0\" Source=\"{Binding Path=Locked, Mode=OneWay}\" Width=\"16\" Height=\"16\" Margin=\"0,0,6,0\"></Image>" +
                "       <Image Grid.Column=\"1\" Source=\"{Binding Path=Country.Flag, Mode=OneWay}\" ToolTip=\"{Binding Path=Country.Name, Mode=OneWay}\" Width=\"22\" Height=\"18\"></Image>" +
                "       <Label Grid.Column=\"2\" FontSize=\"13\" HorizontalAlignment=\"Left\" Foreground=\"White\" Content=\"{Binding Path=Name, Mode=OneWay}\"></Label>" +
                "       <Label Grid.Column=\"3\" FontSize=\"13\" HorizontalAlignment=\"Left\" Foreground=\"White\" Content=\"{Binding Path=Hoster, Mode=OneWay}\"></Label>" +
                "      </Grid>" +
                "     </DataTemplate>" +
                "    </ListBox.ItemTemplate>" +
                "    <ListBox.ContextMenu>" +
                "     <ContextMenu>" +
                "      <MenuItem Header=\"Join this game\"></MenuItem>" +
                "      <MenuItem Header=\"Silent join\"></MenuItem>" +
                "      <MenuItem Header=\"Join and close snooper\"></MenuItem>" +
                "      <MenuItem Header=\"Silent join and close snooper\"></MenuItem>" +
                "     </ContextMenu>" +
                "    </ListBox.ContextMenu>" +
                "   </ListBox>" +
                "  </Border>" +
                " </Grid>"
            , GlobalManager.XamlContext);

            ti.Content = grid;
            ti.DataContext = channel;

            DockPanel dp = (DockPanel)((Border)grid.Children[0]).Child;
            StackPanel sp = (StackPanel)((Canvas)dp.Children[0]).Children[0];
            Button hostBt = (Button)sp.Children[0];
            hostBt.Click += GameHosting;

            Button refreshBt = (Button)sp.Children[1];
            refreshBt.Click += RefreshClick;

            Button leaveChannelBt = (Button)((Canvas)dp.Children[1]).Children[0];
            leaveChannelBt.Click += LeaveChannel;

            StackPanel tbsp = (StackPanel)dp.Children[2];

            TextBlock channelNameTB = (TextBlock)tbsp.Children[0];
            channelNameTB.Text = channel.Name;

            TextBlock userListTB = (TextBlock)tbsp.Children[2];
            Binding b1 = new Binding("ClientCount");
            b1.Source = channel;
            b1.Mode = BindingMode.OneWay;
            userListTB.SetBinding(TextBlock.TextProperty, b1);

            TextBlock gameListTB = (TextBlock)tbsp.Children[5];
            Binding b2 = new Binding("GameCount");
            b2.Source = channel;
            b2.Mode = BindingMode.OneWay;
            gameListTB.SetBinding(TextBlock.TextProperty, b2);

            ListBox lb = (ListBox)((Border)grid.Children[1]).Child;
            lb.MouseDoubleClick += GameDoubleClick;
            lb.SelectionChanged += NoSelectionChange;
            lb.LostFocus += GameList_LostFocus;

            ((MenuItem)lb.ContextMenu.Items[0]).Click += JoinGameClick;
            ((MenuItem)lb.ContextMenu.Items[0]).Tag = lb;
            ((MenuItem)lb.ContextMenu.Items[1]).Click += SilentJoin;
            ((MenuItem)lb.ContextMenu.Items[1]).Tag = lb;
            ((MenuItem)lb.ContextMenu.Items[2]).Click += JoinAndClose;
            ((MenuItem)lb.ContextMenu.Items[2]).Tag = lb;
            ((MenuItem)lb.ContextMenu.Items[3]).Click += SilentJoinAndClose;
            ((MenuItem)lb.ContextMenu.Items[3]).Tag = lb;

            return ti;
        }
    }
}
