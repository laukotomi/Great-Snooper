using System.Collections.Generic;
using System.Windows.Media;
using GreatSnooper.Helpers;
using GreatSnooper.Model;

namespace GreatSnooper.Classes
{
    public class InstantColors
    {
        private Dictionary<User, SolidColorBrush> _instantColors;

        public InstantColors()
        {
            this._instantColors = new Dictionary<User, SolidColorBrush>();
        }

        public bool TryGetValue(User user, out SolidColorBrush brush)
        {
            return this._instantColors.TryGetValue(user, out brush);
        }

        public void Add(User user, SolidColorBrush color)
        {
            this._instantColors[user] = color;
            UserHelper.UpdateMessageStyle(user);
        }

        public void Remove(User user)
        {
            this._instantColors.Remove(user);
            UserHelper.UpdateMessageStyle(user);
        }
    }
}
