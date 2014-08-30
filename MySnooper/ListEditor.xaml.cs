using MahApps.Metro.Controls;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MySnooper
{
    public delegate void RemoveUserDelegate(string name);
    public delegate void AddUserDelegate(string name);

    public partial class ListEditor : MetroWindow
    {
        private SortedObservableCollection<string> MyList;
        private string AddTextStr;
        private Regex NickRegex;
        private Regex NickRegex2;
        public event RemoveUserDelegate RemoveUser;
        public event AddUserDelegate AddUser;

        public ListEditor() { } // Never used, but visual stdio throws an error if not exists
        public ListEditor(SortedObservableCollection<string> MyList, string Title)
        {
            InitializeComponent();

            this.Title = Title;
            this.MyList = MyList;

            AddTextStr = "Add a new user to the list..";
            AddNewUser.Text = AddTextStr;

            NickRegex = new Regex(@"^[a-z`]", RegexOptions.IgnoreCase);
            NickRegex2 = new Regex(@"^[a-z`][a-z0-9`\-]*$", RegexOptions.IgnoreCase);

            Binding b = new Binding();
            b.Source = this.MyList;
            b.Mode = BindingMode.OneWay;
            MyListView.SetBinding(ListView.ItemsSourceProperty, b);
        }

        private void RemoveFromList(object sender, RoutedEventArgs e)
        {
            if (MyListView.SelectedIndex != -1)
            {
                string selected = MyListView.SelectedItem as string;
                RemoveUser(selected);
                MyList.Remove(selected);
            }
        }

        private void NewUserAdd(object sender, KeyEventArgs e)
        {
            var obj = sender as TextBox;
            if (e.Key == Key.Enter && obj.Text.Length > 0)
            {
                string name = obj.Text.Trim();
                if (name.Length > 0)
                {
                    if (!NickRegex.IsMatch(name))
                    {
                        MessageBox.Show("The nickname should begin with a character of the English aplhabet or with ` character!");
                    }
                    else if (!NickRegex2.IsMatch(name))
                    {
                        MessageBox.Show("The nickname contains one or more forbidden characters! Use characters from the English alphabet, numbers or - or `!");
                    }
                    else
                    {
                        bool contains = false;
                        string lname = name.ToLower();
                        for (int i = 0; i < MyList.Count; i++)
                        {
                            if (MyList[i].ToLower() == lname)
                            {
                                contains = true;
                                break;
                            }
                        }
                        if (!contains)
                        {
                            AddUser(name);
                            MyList.Add(name);
                        }
                        obj.Clear();
                    }
                }
            }
        }

        private void NewUserEnter(object sender, KeyboardFocusChangedEventArgs e)
        {
            var obj = sender as TextBox;
            if (obj.Text == AddTextStr)
            {
                obj.Clear();
            }
        }

        private void NewUserLeave(object sender, KeyboardFocusChangedEventArgs e)
        {
            var obj = sender as TextBox;
            if (obj.Text.Trim() == string.Empty)
            {
                obj.Text = AddTextStr;
            }
        }
    }
}
