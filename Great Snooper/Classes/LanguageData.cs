using GreatSnooper.Helpers;
using GreatSnooper.Model;
using System;

namespace GreatSnooper.Classes
{
    public class LanguageData : IComparable
    {
        #region Properties
        public string Name { get; private set; }
        public string CultureName { get; private set; }
        public Country Country { get; private set; }
        #endregion

        public LanguageData(string enName, string name, string cc, string cultureName)
        {
            this.Name = string.Format("{0} ({1})", enName, name);
            this.Country = Countries.GetCountryByCC(cc.ToUpper());
            this.CultureName = cultureName;
        }

        public int CompareTo(object obj)
        {
            var o = (LanguageData)obj;
            return this.Name.CompareTo(o.Name);
        }
    }
}
