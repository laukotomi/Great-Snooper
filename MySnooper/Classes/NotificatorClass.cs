using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace MySnooper
{
    public class NotificatorClass
    {
        private static int idCounter = 0;
        private int id;

        public enum MatchTypes { Equal, StartsWith, EndsWith, Contains };

        public Dictionary<string, string> Words { get; private set; }

        public NotificatorClass()
        {
            idCounter++;
            id = idCounter;
            IsEnabled = true;
            MatchType = MatchTypes.Equal;
            Words = new Dictionary<string, string>();
        }

        public bool IsEnabled { get; set; }
        public bool InGameNames { get; set; }
        public bool InHosterNames { get; set; }
        public bool InMessages { get; set; }
        public bool InMessageSenders { get; set; }
        public bool InJoinMessages { get; set; }
        public MatchTypes MatchType { get; set; }

        public string WordsAsText
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var word in Words)
                {
                    sb.Append(word.Value);
                    sb.Append(Environment.NewLine);
                }
                return sb.ToString();
            }
            set
            {
                Words.Clear();
                string[] words = value.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i].Trim();
                    if (word.Length > 0)
                    {
                        Words.Add(word.ToLower(), word);
                    }
                }
            }
        }

        public bool TryMatch(string str)
        {
            switch (MatchType)
            {
                case MatchTypes.Equal:
                    return Words.ContainsKey(str);
                
                case MatchTypes.StartsWith:
                    foreach (var item in Words)
                    {
                        if (str.StartsWith(item.Key))
                            return true;
                    }
                    break;

                case MatchTypes.EndsWith:
                    foreach (var item in Words)
                    {
                        if (str.EndsWith(item.Key))
                            return true;
                    }
                    break;

                case MatchTypes.Contains:
                    foreach (var item in Words)
                    {
                        if (str.Contains(item.Key))
                            return true;
                    }
                    break;
            }
            return false;
        }


        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            NotificatorClass n = obj as NotificatorClass;
            if ((System.Object)n == null)
            {
                return false;
            }

            // Return true if the fields match:
            return id == n.id;
        }

        public bool Equals(NotificatorClass n)
        {
            // If parameter is null return false:
            if ((object)n == null)
            {
                return false;
            }

            // Return true if the fields match:
            return id == n.id;
        }

        public override int GetHashCode()
        {
            return id;
        }

        public static bool operator ==(NotificatorClass a, NotificatorClass b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.id == b.id;
        }

        public static bool operator !=(NotificatorClass a, NotificatorClass b)
        {
            return !(a == b);
        }
    }

    public class EnumBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }
}
