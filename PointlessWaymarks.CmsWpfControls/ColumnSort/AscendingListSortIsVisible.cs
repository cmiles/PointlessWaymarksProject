using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarks.CmsWpfControls.ColumnSort
{
    public sealed class AscendingListSortIsVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return Visibility.Collapsed;

                case ListSortDirection v:
                    return v == ListSortDirection.Ascending ? Visibility.Visible : Visibility.Collapsed;

                default:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
                return v == Visibility.Visible;
            return false;
        }
    }
}