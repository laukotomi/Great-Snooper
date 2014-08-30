using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

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
                        base.Insert(i, item);
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
    }
}
