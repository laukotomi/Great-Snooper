using System.Windows.Media;

namespace GreatSnooper.Classes
{
    public class AccentColorMenuData
    {
        #region Properties
        public string Name { get; private set; }
        public Brush ColorBrush { get; private set; }
        #endregion

        public AccentColorMenuData(string name, Brush colorBrush)
        {
            this.Name = name;
            this.ColorBrush = colorBrush;
        }
    }
}