namespace GreatSnooper.Converters
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    class BoolToItalicConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((FontStyle)value) == FontStyles.Italic;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var o = (bool?)value;
            if (o.HasValue && o.Value)
            {
                return FontStyles.Italic;
            }
            return FontStyles.Normal;
        }
    }
}