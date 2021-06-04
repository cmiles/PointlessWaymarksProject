using System;
using System.Globalization;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.ValueConverters
{
    public sealed class BooleanNotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return true;

                case bool b:
                    return !b;

                default:
                    return true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}