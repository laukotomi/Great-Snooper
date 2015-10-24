using System;

namespace GreatSnooper.Model
{
    public class Game : IComparable
    {
        #region Properties
        public bool IsAlive { get; set; }

        public uint ID { get; private set; }
        public string Address { get; private set; }
        public string Name { get; private set; }
        public Country Country { get; private set; }
        public string Hoster { get; private set; }
        public bool Locked { get; private set; }
        #endregion

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

        public int CompareTo(object obj)
        {
            var o = (Game)obj;
            return o.ID.CompareTo(this.ID);
        }
    }
}
