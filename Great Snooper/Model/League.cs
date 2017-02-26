namespace GreatSnooper.Model
{
    public class League
    {
        public League(string name, string shortName)
        {
            this.Name = name;
            this.ShortName = shortName;
        }

        public string Name
        {
            get;
            private set;
        }

        public string ShortName
        {
            get;
            private set;
        }
    }
}