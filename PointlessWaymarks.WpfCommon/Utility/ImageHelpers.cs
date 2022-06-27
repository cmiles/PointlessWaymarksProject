using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhotoSauce.MagicScaler;
using PixelFormats = System.Windows.Media.PixelFormats;

namespace PointlessWaymarks.WpfCommon.Utility;

public static class ImageHelpers
{
    //https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.imaging.bitmapsource.create?view=net-5.0
    public static readonly BitmapSource BlankImage = BitmapSource.Create(8, 8, 96, 96, PixelFormats.Indexed1,
        new BitmapPalette(new List<Color> {Colors.Transparent}), new byte[8], 1);

    public static async Task<BitmapSource> InMemoryThumbnailFromFile(FileInfo file, int width, int quality)
    {
        await using var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
        await using var outStream = new MemoryStream();

        var settings = new ProcessImageSettings {Width = width, EncoderOptions = new JpegEncoderOptions(quality, ChromaSubsampleMode.Default, true) };
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