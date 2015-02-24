namespace MySnooper
{
    public static class CountriesClass
    {
        // The countries object
        public static SortedObservableCollection<CountryClass> Countries = new SortedObservableCollection<CountryClass>();

        public static CountryClass DefaultCountry { get; private set; }

        public static void Initialize()
        {
            Countries.Add(new CountryClass("United Kingdom", "GB"));
            Countries.Add(new CountryClass("Argentina", "AR"));
            Countries.Add(new CountryClass("Australia", "AU"));
            Countries.Add(new CountryClass("Austria", "AT"));
            Countries.Add(new CountryClass("Belgium", "BE"));
            Countries.Add(new CountryClass("Brazil", "BR"));
            Countries.Add(new CountryClass("Canada", "CA"));
            Countries.Add(new CountryClass("Croatia", "HR"));
            Countries.Add(new CountryClass("Bosnia", "BA"));
            Countries.Add(new CountryClass("Cyprus", "CY"));
            Countries.Add(new CountryClass("Czech", "CZ"));
            Countries.Add(new CountryClass("Denmark", "DK"));
            Countries.Add(new CountryClass("Finland", "FI"));
            Countries.Add(new CountryClass("France", "FR"));
            Countries.Add(new CountryClass("Georgia", "GE"));
            Countries.Add(new CountryClass("Germany", "DE"));
            Countries.Add(new CountryClass("Greece", "GR"));
            Countries.Add(new CountryClass("Hongkong", "HK"));
            Countries.Add(new CountryClass("Hungary", "HU"));
            Countries.Add(new CountryClass("Iceland", "IS"));
            Countries.Add(new CountryClass("India", "IN"));
            Countries.Add(new CountryClass("Indonesia", "ID"));
            Countries.Add(new CountryClass("Iran", "IR"));
            Countries.Add(new CountryClass("Iraq", "IQ"));
            Countries.Add(new CountryClass("Ireland", "IE"));
            Countries.Add(new CountryClass("Israel", "IL"));
            Countries.Add(new CountryClass("Italy", "IT"));
            Countries.Add(new CountryClass("Japan", "JP"));
            Countries.Add(new CountryClass("Liechtenstein", "LI"));
            Countries.Add(new CountryClass("Luxembourg", "LU"));
            Countries.Add(new CountryClass("Malaysia", "MY"));
            Countries.Add(new CountryClass("Malta", "MT"));
            Countries.Add(new CountryClass("Mexico", "MX"));
            Countries.Add(new CountryClass("Morocco", "MA"));
            Countries.Add(new CountryClass("Netherlands", "NL"));
            Countries.Add(new CountryClass("Newzealand", "NZ"));
            Countries.Add(new CountryClass("Norway", "NO"));
            Countries.Add(new CountryClass("Poland", "PL"));
            Countries.Add(new CountryClass("Portugal", "PT"));
            Countries.Add(new CountryClass("Puertorico", "PR"));
            Countries.Add(new CountryClass("Romania", "RO"));
            Countries.Add(new CountryClass("Russia", "RU"));
            Countries.Add(new CountryClass("Singapore", "SG"));
            Countries.Add(new CountryClass("South Africa", "ZA"));
            Countries.Add(new CountryClass("Spain", "ES"));
            Countries.Add(new CountryClass("Sweden", "SE"));
            Countries.Add(new CountryClass("Switzerland", "CH"));
            Countries.Add(new CountryClass("Turkey", "TR"));
            Countries.Add(new CountryClass("USA", "US"));
            Countries.Add(new CountryClass("Unknown", "UN"));
            Countries.Add(new CountryClass("Blank 1", "B1"));
            Countries.Add(new CountryClass("Blank 2", "B2"));
            Countries.Add(new CountryClass("Blank 3", "B3"));
            Countries.Add(new CountryClass("Chile", "CL"));
            Countries.Add(new CountryClass("Serbia", "RS"));
            Countries.Add(new CountryClass("Slovenia", "SI"));
            Countries.Add(new CountryClass("Lebanon", "LB"));
            Countries.Add(new CountryClass("Moldova", "MO"));
            Countries.Add(new CountryClass("Ukraine", "UA"));
            Countries.Add(new CountryClass("Latvia", "LV"));
            Countries.Add(new CountryClass("Slovakia", "SK"));
            Countries.Add(new CountryClass("Costa Rica", "CR"));
            Countries.Add(new CountryClass("Estonia", "EE"));
            Countries.Add(new CountryClass("China", "CN"));
            Countries.Add(new CountryClass("Colombia", "CO"));
            Countries.Add(new CountryClass("Ecuador", "EC"));
            Countries.Add(new CountryClass("Uruguay", "UY"));
            Countries.Add(new CountryClass("Venezuela", "VE"));
            Countries.Add(new CountryClass("Lithuania", "LT"));
            Countries.Add(new CountryClass("Bulgaria", "BG"));
            Countries.Add(new CountryClass("Egypt", "EG"));
            Countries.Add(new CountryClass("Saudi Arabia", "SA"));
            Countries.Add(new CountryClass("South Korea", "KR"));
            Countries.Add(new CountryClass("Belarus", "BY"));
            Countries.Add(new CountryClass("Peru", "PE"));
            Countries.Add(new CountryClass("Algeria", "DZ"));
            Countries.Add(new CountryClass("Kazakhstan", "KZ"));
            Countries.Add(new CountryClass("El Salvador", "SV"));
            Countries.Add(new CountryClass("Taiwan", "TW"));
            Countries.Add(new CountryClass("Jamaica", "JM"));
            Countries.Add(new CountryClass("Guatemala", "GT"));
            Countries.Add(new CountryClass("Marshall Islands", "MH"));
            Countries.Add(new CountryClass("Macedonia", "MK"));
            Countries.Add(new CountryClass("United Arab Emirates", "AE"));

            DefaultCountry = GetCountryByID(49);
        }

        // Get a country by its country code
        public static CountryClass GetCountryByCC(string CountryCode)
        {
            for (int i = 0; i < Countries.Count; i++)
            {
                if (Countries[i].CountryCode == CountryCode)
                    return Countries[i];
            }
            return DefaultCountry;
        }

        // Get a country by its ID (this method is needed, because the countries will be stored in order of their names not in order of their IDs)
        public static CountryClass GetCountryByID(int ID)
        {
            for (int i = 0; i < Countries.Count; i++)
            {
                if (Countries[i].ID == ID)
                    return Countries[i];
            }
            return DefaultCountry;
        }
    }
}
