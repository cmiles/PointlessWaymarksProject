using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using PhotoSauce.MagicScaler;
using PixelFormats = System.Windows.Media.PixelFormats;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public static class ImageHelpers
    {
        public static BitmapSource BlankImage =
            BitmapSource.Create(1, 1, 1, 1, PixelFormats.BlackWhite, null, new byte[] { 1 }, 1);

        public static async Task<BitmapSource> InMemoryThumbnailFromFile(FileInfo file, int width, int quality)
        {
            await using var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
            await using var outStream = new MemoryStream();

            var settings = new ProcessImageSettings { Width = width, JpegQuality = quality };
            MagicImageProcessor.ProcessImage(fileStream, outStream, settings);

            outStream.Position = 0;

            var uiImage = new BitmapImage();
            uiImage.BeginInit();
            uiImage.CacheOption = BitmapCacheOption.OnLoad;
            uiImage.StreamSource = outStream;
            uiImage.EndInit();
            uiImage.Freeze();

            return uiImage;
        }
    }
}