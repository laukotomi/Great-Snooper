namespace MySnooper
{
    public static class CountriesClass
    {
        // The countries object
        public static SortedObservableCollection<CountryClass> Countries = new SortedObservableCollection<CountryClass>()
        {
            new CountryClass("United Kingdom", "GB"),
            new CountryClass("Argentina", "AR"),
            new CountryClass("Australia", "AU"),
            new CountryClass("Austria", "AT"),
            new CountryClass("Belgium", "BE"),
            new CountryClass("Brazil", "BR"),
            new CountryClass("Canada", "CA"),
            new CountryClass("Croatia", "HR"),
            new CountryClass("Bosnia", "BA"),
            new CountryClass("Cyprus", "CY"),
            new CountryClass("Czech", "CZ"),
            new CountryClass("Denmark", "DK"),
            new CountryClass("Finland", "FI"),
            new CountryClass("France", "FR"),
            new CountryClass("Georgia", "GE"),
            new CountryClass("Germany", "DE"),
            new CountryClass("Greece", "GR"),
            new CountryClass("Hongkong", "HK"),
            new CountryClass("Hungary", "HU"),
            new CountryClass("Iceland", "IS"),
            new CountryClass("India", "IN"),
            new CountryClass("Indonesia", "ID"),
            new CountryClass("Iran", "IR"),
            new CountryClass("Iraq", "IQ"),
            new CountryClass("Ireland", "IE"),
            new CountryClass("Israel", "IL"),
            new CountryClass("Italy", "IT"),
            new CountryClass("Japan", "JP"),
            new CountryClass("Liechtenstein", "LI"),
            new CountryClass("Luxembourg", "LU"),
            new CountryClass("Malaysia", "MY"),
            new CountryClass("Malta", "MT"),
            new CountryClass("Mexico", "MX"),
            new CountryClass("Morocco", "MA"),
            new CountryClass("Netherlands", "NL"),
            new CountryClass("Newzealand", "NZ"),
            new CountryClass("Norway", "NO"),
            new CountryClass("Poland", "PL"),
            new CountryClass("Portugal", "PT"),
            new CountryClass("Puertorico", "PR"),
            new CountryClass("Romania", "RO"),
            new CountryClass("Russia", "RU"),
            new CountryClass("Singapore", "SG"),
            new CountryClass("South Africa", "ZA"),
            new CountryClass("Spain", "ES"),
            new CountryClass("Sweden", "SE"),
            new CountryClass("Switzerland", "CH"),
            new CountryClass("Turkey", "TR"),
            new CountryClass("USA", "US"),
            new CountryClass("Unknown", "UN"),
            new CountryClass("Blank 1", "B1"),
            new CountryClass("Blank 2", "B2"),
            new CountryClass("Blank 3", "B3"),
            new CountryClass("Chile", "CL"),
            new CountryClass("Serbia", "RS"),
            new CountryClass("Slovenia", "SI"),
            new CountryClass("Lebanon", "LB"),
            new CountryClass("Moldova", "MO"),
            new CountryClass("Ukraine", "UA"),
            new CountryClass("Latvia", "LV"),
            new CountryClass("Slovakia", "SK"),
            new CountryClass("Costa Rica", "CR"),
            new CountryClass("Estonia", "EE"),
            new CountryClass("China", "CN"),
            new CountryClass("Colombia", "CO"),
            new CountryClass("Ecuador", "EC"),
            new CountryClass("Uruguay", "UY"),
            new CountryClass("Venezuela", "VE"),
            new CountryClass("Lithuania", "LT"),
            new CountryClass("Bulgaria", "BG"),
            new CountryClass("Egypt", "EG"),
            new CountryClass("Saudi Arabia", "SA"),
            new CountryClass("South Korea", "KR"),
            new CountryClass("Belarus", "BY"),
            new CountryClass("Peru", "PE"),
            new CountryClass("Algeria", "DZ"),
            new CountryClass("Kazakhstan", "KZ"),
            new CountryClass("El Salvador", "SV"),
            new CountryClass("Taiwan", "TW"),
            new CountryClass("Jamaica", "JM"),
            new CountryClass("Guatemala", "GT"),
            new CountryClass("Marshall Islands", "MH"),
            new CountryClass("Macedonia", "MK"),
            new CountryClass("United Arab Emirates", "AE")
        };

        // Get a country by its country code
        public static CountryClass GetCountryByCC(string CountryCode)
        {
            for (int i = 0; i < Countries.Count; i++)
            {
                if (Countries[i].CountryCode == CountryCode)
                    return Countries[i];
            }
            return GetCountryByID(49);
        }

        // Get a country by its ID (this method is needed, because the countries will be stored in order of their names not in order of their IDs)
        public static CountryClass GetCountryByID(int ID)
        {
            for (int i = 0; i < Countries.Count; i++)
            {
                if (Countries[i].ID == ID)
                    return Countries[i];
            }
            return GetCountryByID(49);
        }
    }
}
