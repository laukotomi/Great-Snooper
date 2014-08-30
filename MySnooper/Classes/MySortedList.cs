using System;
using System.Collections.Generic;

namespace MySnooper
{
    public class MySortedList<T> : List<T>
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
    }
}
