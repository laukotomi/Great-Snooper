namespace GreatSnooper.Model
{
    using System;

    public class Game : IComparable
    {
        // Constructor
        public Game(uint id, string name, string address, Country country, string hoster, bool locked)
        {
            this.ID = id;
            this.Address = address;
            this.Locked = locked;
            this.Name = name;
            this.Country = country;
            this.Hoster = hoster;
            this.IsAlive = true;
        }

        public string Address
        {
            get;
            private set;
        }

        public Country Country
        {
            get;
            private set;
        }

        public string Hoster
        {
            get;
            private set;
        }

        public uint ID
        {
            get;
            private set;
        }

        public bool IsAlive
        {
            get;
            set;
        }

        public bool Locked
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public int CompareTo(object obj)
        {
            var o = (Game)obj;
            return o.ID.CompareTo(this.ID);
        }
    }
}