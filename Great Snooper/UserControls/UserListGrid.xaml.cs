namespace GreatSnooper.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    public partial class UserListGrid : DataGrid
    {
        private ChannelViewModel chvm;
        private bool userListDBColumnChanging;

        public UserListGrid(ChannelViewModel chvm)
        {
            this.chvm = chvm;
            this.DataContext = chvm;
            InitializeComponent();

            if (!Properties.Settings.Default.ShowInfoColumn)
            {
                this.Columns[4].Visibility = System.Windows.Visibility.Collapsed;
            }

            SetUserListDGColumnWidths();
            SetDefaultOrderForGrid();

            foreach (var column in this.Columns)
            {
                DataGridColumn.ActualWidthProperty.AddValueChanged(column, delegate
                {
                    if (userListDBColumnChanging || Mouse.LeftButton == MouseButtonState.Released)
                    {
                        return;
                    }

                    userListDBColumnChanging = true;
                });
            }
        }

        public void SetDefaultOrderForGrid()
        {
            if (this.chvm.Server is WormNetCommunicator)
            {
                string[] order = Properties.Settings.Default.ColumnOrder.Split(new char[] { '|' });
                if (order.Length == 2)
                {
                    ListSortDirection dir = order[1] == "D" ? ListSortDirection.Descending : ListSortDirection.Ascending;
                    SetOrderForDataGrid(order[0], dir);
                }
                else
                {
                    SetOrderForDataGrid(Localizations.GSLocalization.Instance.NickHeaderLabel, ListSortDirection.Ascending);
                }
            }
            else
            {
                var view = CollectionViewSource.GetDefaultView(this.chvm.Users);
                if (view != null)
                {
                    view.SortDescriptions.Clear();
                    view.SortDescriptions.Add(new System.ComponentModel.SortDescription("IsBanned", System.ComponentModel.ListSortDirection.Ascending));
                    view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Group.ID", System.ComponentModel.ListSortDirection.Ascending));
                    view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", ListSortDirection.Ascending));
                }
            }
        }

        public void SetUserListDGColumnWidths()
        {
            string[] settings;
            settings = Properties.Settings.Default.ClientListDGColumns.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            int i = 0;
            foreach (var column in this.Columns)
            {
                if (settings.Length > i)
                {
                    DataGridLengthUnitType type = (i < 2) ? DataGridLengthUnitType.Pixel : DataGridLengthUnitType.Star;
                    column.Width = new DataGridLength(Convert.ToInt32(settings[i]), type);
                }
                i++;
            }
        }

        public void SetUserListDGView()
        {
            var view = CollectionViewSource.GetDefaultView(this.chvm.Users);
            if (view != null)
            {
                if (!Properties.Settings.Default.ShowBannedUsers)
                {
                    if (view.Filter != DefaultBannedView)
                    {
                        view.Filter = DefaultBannedView;
                    }
                }
                else if (view.Filter != null)
                {
                    view.Filter = null;
                }
            }
        }

        private void DataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            this.SelectedItem = null;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.SelectedItem == null)
            {
                return;
            }

            this.OpenChat((User)this.SelectedItem);
        }

        private void DataGrid_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (userListDBColumnChanging)
            {
                userListDBColumnChanging = false;

                // Save column widths
                var sb = new StringBuilder();
                List<int> help = new List<int>()
                {
                    2, 3, 4
                };
                double sum = 0;
                foreach (var idx in help)
                {
                    if (this.Columns[idx].Visibility == System.Windows.Visibility.Visible)
                    {
                        sum += this.Columns[idx].ActualWidth;
                    }
                }

                int i = 0;
                foreach (var column in this.Columns)
                {
                    if (column.Visibility == System.Windows.Visibility.Visible)
                    {
                        if (i < 2)
                        {
                            sb.Append(Convert.ToInt32(column.ActualWidth));
                        }
                        else
                        {
                            sb.Append(Convert.ToInt32((column.ActualWidth / sum) * 100));
                        }
                    }
                    else
                    {
                        sb.Append("10");
                    }

                    if (i + 1 < this.Columns.Count)
                    {
                        sb.Append('|');
                    }
                    i++;
                }

                Properties.Settings.Default.ClientListDGColumns = sb.ToString();
                Properties.Settings.Default.Save();

                foreach (var channel in this.chvm.MainViewModel.AllChannels)
                {
                    ChannelViewModel channelVM = channel as ChannelViewModel;
                    if (channelVM != null)
                    {
                        if (channelVM.UserListDG != null)
                        {
                            channelVM.UserListDG.SetUserListDGColumnWidths();
                        }
                    }
                }
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (this.chvm.Server is GameSurgeCommunicator)
            {
                return;
            }

            e.Handled = true;
            string columnName = e.Column.Header.ToString();
            ListSortDirection dir = ListSortDirection.Ascending;
            if (!e.Column.SortDirection.HasValue || e.Column.SortDirection.Value == System.ComponentModel.ListSortDirection.Descending)
            {
                Properties.Settings.Default.ColumnOrder = columnName + "|A";
            }
            else
            {
                Properties.Settings.Default.ColumnOrder = columnName + "|D";
                dir = ListSortDirection.Descending;
            }
            Properties.Settings.Default.Save();

            foreach (var item in this.chvm.Server.Channels)
            {
                ChannelViewModel channel = item.Value as ChannelViewModel;
                if (channel != null && channel.UserListDG != null)
                {
                    channel.UserListDG.SetOrderForDataGrid(columnName, dir);
                }
            }
        }

        private bool DefaultBannedView(object o)
        {
            return !((User)o).IsBanned;
        }

        private void OpenChat(User u)
        {
            if (u.IsBanned || u.Name == this.chvm.Server.User.Name)
            {
                return;
            }

            // Test if we already have an opened chat with the user
            var oldchvm = this.chvm.MainViewModel.AllChannels.FirstOrDefault(x => x.Name == u.Name && x.Server == this.chvm.Server);
            if (oldchvm != null)
            {
                if (this.chvm.MainViewModel.SelectedChannel != oldchvm)
                {
                    this.chvm.MainViewModel.SelectChannel(oldchvm);
                }
                else
                {
                    this.chvm.MainViewModel.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.chvm.MainViewModel.DialogService.GetView().UpdateLayout();
                        oldchvm.IsTBFocused = true;
                    }));
                }
                return;
            }

            // Make new channel
            var newchvm = new PMChannelViewModel(this.chvm.MainViewModel, this.chvm.Server, u.Name);
            this.chvm.MainViewModel.SelectChannel(newchvm);
        }

        private void SetOrderForDataGrid(string columnName, ListSortDirection direction)
        {
            var view = CollectionViewSource.GetDefaultView(this.chvm.Users);
            if (view != null)
            {
                view.SortDescriptions.Clear();

                view.SortDescriptions.Add(new SortDescription("IsBanned", System.ComponentModel.ListSortDirection.Ascending));
                view.SortDescriptions.Add(new SortDescription("Group.ID", System.ComponentModel.ListSortDirection.Ascending));

                if (columnName != Localizations.GSLocalization.Instance.NickHeaderLabel)
                {
                    if (columnName == Localizations.GSLocalization.Instance.CountryHeaderLabel)
                    {
                        view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Country", direction));
                    }
                    else if (columnName == Localizations.GSLocalization.Instance.RankHeaderLabel)
                    {
                        view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Rank", direction));
                    }
                    else if (columnName == Localizations.GSLocalization.Instance.ClanHeaderLabel)
                    {
                        view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Clan", direction));
                    }
                    else if (columnName == Localizations.GSLocalization.Instance.InfoHeaderLabel)
                    {
                        view.SortDescriptions.Add(new System.ComponentModel.SortDescription("ClientName", direction));
                    }
                    view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
                }
                else
                {
                    view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", direction));
                }
            }

            foreach (var column in this.Columns)
            {
                if (column.Header.ToString() == columnName)
                {
                    column.SortDirection = direction;
                }
                else
                {
                    column.SortDirection = null;
                }
            }
        }
    }
}