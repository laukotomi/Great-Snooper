using System;
using System.Collections.Generic;
using System.Windows.Media;
using GreatSnooper.Helpers;
using GreatSnooper.Model;

namespace GreatSnooper.Services
{
    public class InstantColors
    {
        #region Singleton
        private static readonly Lazy<InstantColors> lazy =
            new Lazy<InstantColors>(() => new InstantColors());

        public static InstantColors Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        private InstantColors()
        {
            this._instantColors = new Dictionary<User, SolidColorBrush>();
        }
        #endregion

        private Dictionary<User, SolidColorBrush> _instantColors;

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
