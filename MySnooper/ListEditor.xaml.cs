using MahApps.Metro.Controls;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MySnooper
{
    public delegate void ItemRemovedDelegate(string item);
    public delegate void ItemAddedDelegate(string item);

    public partial class ListEditor : MetroWindow
    {
        public enum ListModes { Users, Normal }

        private SortedObservableCollection<string> list;
        private string addTextStr;
        private Regex nickRegex;
        private Regex nickRegex2;
        private ListModes mode;
        public event ItemRemovedDelegate ItemRemoved;
        public event ItemAddedDelegate ItemAdded;

        public ListEditor() { } // Never used, but visual stdio throws an error if not exists
        public ListEditor(SortedObservableCollection<string> list, string title, ListModes mode = ListModes.Users)
        {
            InitializeComponent();

            this.Title = title;
            this.list = list;
            this.mode = mode;

            if (mode == ListModes.Users)
            {
                addTextStr = "Add a new user to the list..";
                AddToListTB.Text = addTextStr;

                nickRegex = new Regex(@"^[a-z`]", RegexOptions.IgnoreCase);
                nickRegex2 = new Regex(@"^[a-z`][a-z0-9`\-]*$", RegexOptions.IgnoreCase);
            }
            else
            {
                addTextStr = "Enter text here..";
                AddToListTB.Text = addTextStr;
            }

            Binding b = new Binding();
            b.Source = this.list;
            b.Mode = BindingMode.OneWay;
            MyListView.SetBinding(ListView.ItemsSourceProperty, b);
        }

        private void RemoveFromList(object sender, RoutedEventArgs e)
        {
            if (MyListView.SelectedIndex != -1)
            {
                string selected = (string)MyListView.SelectedItem;
                list.Remove(selected);
                if (ItemRemoved != null)
                    ItemRemoved(selected);
            }
        }

        private void AddToList(object sender, KeyEventArgs e)
        {
            var obj = sender as TextBox;
            if (e.Key == Key.Enter && obj.Text.Length > 0)
            {
                string str = obj.Text.Trim();
                if (str.Length > 0)
                {
                    if (mode == ListModes.Users)
                    {
                        if (!nickRegex.IsMatch(str))
                        {
                            MessageBox.Show("The nickname should begin with a character of the English aplhabet or with ` character!");
                            return;
                        }
                        else if (!nickRegex2.IsMatch(str))
                        {
                            MessageBox.Show("The nickname contains one or more forbidden characters! Use characters from the English alphabet, numbers or - or `!");
                            return;
                        }
                    }

                    bool contains = false;
                    string lname = str.ToLower();
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].ToLower() == lname)
                        {
                            contains = true;
                            break;
                        }
                    }

                    if (!contains)
                    {
                        list.Add(str);
                        if (ItemAdded != null)
                            ItemAdded(str);
                    }
                    obj.Clear();
                }
            }
        }

        private void AddToListEnter(object sender, KeyboardFocusChangedEventArgs e)
        {
            var obj = sender as TextBox;
            if (obj.Text == addTextStr)
            {
                obj.Clear();
            }
        }

        private void AddToListLeave(object sender, KeyboardFocusChangedEventArgs e)
        {
            var obj = sender as TextBox;
            if (obj.Text.Trim() == string.Empty)
            {
                obj.Text = addTextStr;
            }
        }

        private void MetroWindow_ContentRendered(object sender, System.EventArgs e)
        {
            AddToListTB.Focus();
        }

        private void InformationClicked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "To add item to the list enter text into the textbox and press enter." + Environment.NewLine + "To remove item from the list right click on an item and choose remove.", "Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MyListView_LostFocus(object sender, RoutedEventArgs e)
        {
            var obj = (ListView)sender;
            obj.SelectedIndex = -1;
        }
    }
}
