using System;
using System.Collections.ObjectModel;
using System.Text;

namespace MySnooper
{
    /// <summary>  
    /// A Sorted ObservableCollection.  
    /// - Sorts on Insert.  
    /// - Requires that T implements IComparable.  
    /// </summary>  
    /// <typeparam name="T">The type held within collection</typeparam>  
    public class SortedObservableCollection<T> : ObservableCollection<T>
        where T : IComparable
    {
        public new int Add(T item)
        {
            int i = 0;
            for (; i < Count; i++)
            {
                switch (Math.Sign(this[i].CompareTo(item)))
                {
                    case 0:
                    case 1:
                        Insert(i, item);
                        return i;
                    case -1:
                        break;
                }
            }
            base.Add(item);
            return i;
        }

        public void AddList(System.Collections.Generic.IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
                Add(list[i]);
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Count; i++)
            {
                sb.Append(this[i]);
                sb.Append(','); 
            }
            return sb.ToString();
        }

        public void DeSerialize(string str)
        {
            string[] servers = str.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var server in servers)
                Add((T)(object)server);
        }
    }
}
