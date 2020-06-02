using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public class ImageCacheConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = (string) value;

            if (string.IsNullOrWhiteSpace(path))
            {
                return BitmapSource.Create(1, 1, 1, 1, PixelFormats.BlackWhite, null, new byte[] {1}, 1);
            }

            var possibleFile = new FileInfo(path);
            if (!possibleFile.Exists)
            {
                return BitmapSource.Create(1, 1, 1, 1, PixelFormats.BlackWhite, null, new byte[] {1}, 1);
            }

            var uriSource = new Uri(path, UriKind.Absolute);

            try
            {
                // load the image, specify CacheOption so the file is not locked
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = uriSource;
                image.EndInit();
                return image;
            }
            catch
            {
                return BitmapSource.Create(1, 1, 1, 1, PixelFormats.BlackWhite, null, new byte[] {1}, 1);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("Not implemented.");
        }
    }
}