namespace GreatSnooper.Classes
{
    using System;
    using System.Collections.Generic;

    public class MySortedList<T> : List<T>
        where T : IComparable
    {
        public MySortedList()
        : base()
        {
        }

        public MySortedList(IEnumerable<T> collection)
        : base(collection)
        {
        }

        public static MySortedList<string> CreateFrom(string serialized)
        {
            string[] list = serialized.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return new MySortedList<string>(list);
        }

        public new int Add(T item)
        {
            int i = 0;
            for (; i < Count; i++)
            {
                switch (Math.Sign(this[i].CompareTo(item)))
                {
                case 0:
                case 1:
                    this.Insert(i, item);
                    return i;
                case -1:
                    break;
                }
            }
            base.Add(item);
            return i;
        }

        public new bool Contains(T item)
        {
            return this.BinarySearch(item) != -1;
        }

        public new void Remove(T item)
        {
            int idx = this.BinarySearch(item);
            if (idx != -1)
            {
                this.RemoveAt(idx);
            }
        }

        private new int BinarySearch(T item)
        {
            int imax = this.Count - 1;
            int imin = 0;

            // continue searching while [imin,imax] is not empty
            while (imax >= imin)
            {
                // calculate the midpoint for roughly equal partition
                int imid = imin + (imax - imin) / 2;
                switch (Math.Sign(this[imid].CompareTo(item)))
                {
                case 0:
                    // key found at index imid
                    return imid;
                case -1:
                    imin = imid + 1;
                    break;
                case 1:
                    imax = imid - 1;
                    break;
                }
            }

            // key was not found
            return -1;
        }
    }
}