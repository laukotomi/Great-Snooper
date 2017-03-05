namespace GreatSnooper.Converters
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    class BoolToBoldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((FontWeight)value) == FontWeights.Bold;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var o = (bool?)value;
            if (o.HasValue && o.Value)
            {
                return FontWeights.Bold;
            }
            return FontWeights.Normal;
        }
    }
}