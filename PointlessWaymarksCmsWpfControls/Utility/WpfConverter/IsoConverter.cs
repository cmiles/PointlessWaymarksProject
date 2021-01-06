using System;
using System.Globalization;
using System.Windows.Data;

namespace PointlessWaymarksCmsWpfControls.Utility.WpfConverter
{
    public class IsoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(value?.ToString())) return "(No Iso)";

            return $"ISO: {value}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}