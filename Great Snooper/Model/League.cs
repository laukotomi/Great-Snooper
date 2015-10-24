
namespace GreatSnooper.Model
{
    public class League
    {
        #region Properties
        public string Name { get; private set; }
        public string ShortName { get; private set; }
        #endregion

        public League(string name, string shortName)
        {
            this.Name = name;
            this.ShortName = shortName;
        }
    }
}
