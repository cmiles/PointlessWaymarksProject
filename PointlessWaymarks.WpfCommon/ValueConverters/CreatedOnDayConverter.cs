using System;
using System.Globalization;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.ValueConverters
{
    public class CreatedOnDayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var createdOn = value as DateTime?;
            if (createdOn == null) return "No Created On Date";
            return $"Created On {createdOn:yyyy-MM-dd}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}