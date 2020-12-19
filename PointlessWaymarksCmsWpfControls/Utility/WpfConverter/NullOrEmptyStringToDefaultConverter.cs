using System;
using System.Globalization;
using System.Windows.Data;

namespace PointlessWaymarksCmsWpfControls.Utility.WpfConverter
{
    public class NullOrWhiteSpaceStringToDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(value?.ToString())) return parameter;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}