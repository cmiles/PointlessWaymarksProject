using System;
using System.Globalization;
using System.Windows.Data;

namespace PointlessWaymarksCmsWpfControls.PhotoList
{
    public class PhotoLoadAllRecentNextActionTextMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            var isLoadEnum = value is PhotoListContext.PhotoListLoadMode;

            if (!isLoadEnum)
                return string.Empty;

            var currentValue = (PhotoListContext.PhotoListLoadMode) value;

            if (currentValue == PhotoListContext.PhotoListLoadMode.All)
                return "Load Recent Only";
            if (currentValue == PhotoListContext.PhotoListLoadMode.Recent)
                return "Load All";

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}