using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarks.CmsWpfControls.PhotoList
{
    public class PhotoLoadAllowsAllRecentChoiceVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            var isLoadEnum = value is PhotoListContext.PhotoListLoadMode;

            if (!isLoadEnum)
                return Visibility.Collapsed;

            var currentValue = (PhotoListContext.PhotoListLoadMode) value;

            if (currentValue == PhotoListContext.PhotoListLoadMode.All ||
                currentValue == PhotoListContext.PhotoListLoadMode.Recent)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}