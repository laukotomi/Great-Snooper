namespace GreatSnooper.Classes
{
    using System.Windows.Media;

    public class AccentColorMenuData
    {
        public AccentColorMenuData(string name, Brush colorBrush)
        {
            this.Name = name;
            this.ColorBrush = colorBrush;
        }

        public Brush ColorBrush
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }
    }
}