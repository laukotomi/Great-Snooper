namespace GreatSnooper.Classes
{
    using System;

    using GreatSnooper.Helpers;
    using GreatSnooper.Model;

    public class LanguageData : IComparable
    {
        public LanguageData(string enName, string name, string cc, string cultureName)
        {
            this.Name = string.Format("{0} ({1})", enName, name);
            this.Country = Countries.GetCountryByCC(cc.ToUpper());
            this.CultureName = cultureName;
        }

        public Country Country
        {
            get;
            private set;
        }

        public string CultureName
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
            var o = (LanguageData)obj;
            return this.Name.CompareTo(o.Name);
        }
    }
}