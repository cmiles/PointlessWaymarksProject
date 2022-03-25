using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PointlessWaymarks.CmsData.Content;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor;

public class PhotoHasLatLongVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2) return Visibility.Hidden;
        if (values[0] is not double latitude ||
            values[1] is not double longitude) return Visibility.Hidden;
        var latitudeValidation = CommonContentValidation.LatitudeValidation(latitude);
        var longitudeValidation = CommonContentValidation.LongitudeValidation(longitude);
        if (!latitudeValidation.Valid || !longitudeValidation.Valid) return Visibility.Hidden;
        return Visibility.Visible;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}