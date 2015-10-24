using GreatSnooper.Classes;
using GreatSnooper.Model;

namespace GreatSnooper.Helpers
{
    public static class Countries
    {
        public static MySortedList<Country> CountryList { get; private set; }
        public static Country DefaultCountry { get; private set; }

        public static void Initialize()
        {
            CountryList = new MySortedList<Country>();
            CountryList.Add(new Country("United Kingdom", "GB"));
            CountryList.Add(new Country("Argentina", "AR"));
            CountryList.Add(new Country("Australia", "AU"));
            CountryList.Add(new Country("Austria", "AT"));
            CountryList.Add(new Country("Belgium", "BE"));
            CountryList.Add(new Country("Brazil", "BR"));
            CountryList.Add(new Country("Canada", "CA"));
            CountryList.Add(new Country("Croatia", "HR"));
            CountryList.Add(new Country("Bosnia", "BA"));
            CountryList.Add(new Country("Cyprus", "CY"));
            CountryList.Add(new Country("Czech", "CZ"));
            CountryList.Add(new Country("Denmark", "DK"));
            CountryList.Add(new Country("Finland", "FI"));
            CountryList.Add(new Country("France", "FR"));
            CountryList.Add(new Country("Georgia", "GE"));
            CountryList.Add(new Country("Germany", "DE"));
            CountryList.Add(new Country("Greece", "GR"));
            CountryList.Add(new Country("Hongkong", "HK"));
            CountryList.Add(new Country("Hungary", "HU"));
            CountryList.Add(new Country("Iceland", "IS"));
            CountryList.Add(new Country("India", "IN"));
            CountryList.Add(new Country("Indonesia", "ID"));
            CountryList.Add(new Country("Iran", "IR"));
            CountryList.Add(new Country("Iraq", "IQ"));
            CountryList.Add(new Country("Ireland", "IE"));
            CountryList.Add(new Country("Israel", "IL"));
            CountryList.Add(new Country("Italy", "IT"));
            CountryList.Add(new Country("Japan", "JP"));
            CountryList.Add(new Country("Liechtenstein", "LI"));
            CountryList.Add(new Country("Luxembourg", "LU"));
            CountryList.Add(new Country("Malaysia", "MY"));
            CountryList.Add(new Country("Malta", "MT"));
            CountryList.Add(new Country("Mexico", "MX"));
            CountryList.Add(new Country("Morocco", "MA"));
            CountryList.Add(new Country("Netherlands", "NL"));
            CountryList.Add(new Country("Newzealand", "NZ"));
            CountryList.Add(new Country("Norway", "NO"));
            CountryList.Add(new Country("Poland", "PL"));
            CountryList.Add(new Country("Portugal", "PT"));
            CountryList.Add(new Country("Puertorico", "PR"));
            CountryList.Add(new Country("Romania", "RO"));
            CountryList.Add(new Country("Russia", "RU"));
            CountryList.Add(new Country("Singapore", "SG"));
            CountryList.Add(new Country("South Africa", "ZA"));
            CountryList.Add(new Country("Spain", "ES"));
            CountryList.Add(new Country("Sweden", "SE"));
            CountryList.Add(new Country("Switzerland", "CH"));
            CountryList.Add(new Country("Turkey", "TR"));
            CountryList.Add(new Country("USA", "US"));
            CountryList.Add(new Country("Unknown", "UN"));
            CountryList.Add(new Country("Blank 1", "B1"));
            CountryList.Add(new Country("Blank 2", "B2"));
            CountryList.Add(new Country("Blank 3", "B3"));
            CountryList.Add(new Country("Chile", "CL"));
            CountryList.Add(new Country("Serbia", "RS"));
            CountryList.Add(new Country("Slovenia", "SI"));
            CountryList.Add(new Country("Lebanon", "LB"));
            CountryList.Add(new Country("Moldova", "MO"));
            CountryList.Add(new Country("Ukraine", "UA"));
            CountryList.Add(new Country("Latvia", "LV"));
            CountryList.Add(new Country("Slovakia", "SK"));
            CountryList.Add(new Country("Costa Rica", "CR"));
            CountryList.Add(new Country("Estonia", "EE"));
            CountryList.Add(new Country("China", "CN"));
            CountryList.Add(new Country("Colombia", "CO"));
            CountryList.Add(new Country("Ecuador", "EC"));
            CountryList.Add(new Country("Uruguay", "UY"));
            CountryList.Add(new Country("Venezuela", "VE"));
            CountryList.Add(new Country("Lithuania", "LT"));
            CountryList.Add(new Country("Bulgaria", "BG"));
            CountryList.Add(new Country("Egypt", "EG"));
            CountryList.Add(new Country("Saudi Arabia", "SA"));
            CountryList.Add(new Country("South Korea", "KR"));
            CountryList.Add(new Country("Belarus", "BY"));
            CountryList.Add(new Country("Peru", "PE"));
            CountryList.Add(new Country("Algeria", "DZ"));
            CountryList.Add(new Country("Kazakhstan", "KZ"));
            CountryList.Add(new Country("El Salvador", "SV"));
            CountryList.Add(new Country("Taiwan", "TW"));
            CountryList.Add(new Country("Jamaica", "JM"));
            CountryList.Add(new Country("Guatemala", "GT"));
            CountryList.Add(new Country("Marshall Islands", "MH"));
            CountryList.Add(new Country("Macedonia", "MK"));
            CountryList.Add(new Country("United Arab Emirates", "AE"));

            DefaultCountry = GetCountryByID(49);
        }

        // Get a country by its country code
        public static Country GetCountryByCC(string CountryCode)
        {
            for (int i = 0; i < CountryList.Count; i++)
            {
                if (CountryList[i].CountryCode == CountryCode)
                    return CountryList[i];
            }
            return DefaultCountry;
        }

        // Get a country by its ID (this method is needed, because the countries will be stored in order of their names not in order of their IDs)
        public static Country GetCountryByID(int ID)
        {
            for (int i = 0; i < CountryList.Count; i++)
            {
                if (CountryList[i].ID == ID)
                    return CountryList[i];
            }
            return DefaultCountry;
        }
    }
}
