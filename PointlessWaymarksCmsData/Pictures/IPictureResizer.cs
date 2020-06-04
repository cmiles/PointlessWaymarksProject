using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AngleSharp.Text;
using PhotoSauce.MagicScaler;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PointlessWaymarksCmsData.Pictures
{
    public interface IPictureResizer
    {
        FileInfo ResizeTo(FileInfo toResize, int width, int quality, string imageTypeString, bool addSizeString,
            IProgress<string> progress);
    }

    public class ImageSharpImageResizer : IPictureResizer
    {
        public FileInfo ResizeTo(FileInfo toResize, int width, int quality, string imageTypeString, bool addSizeString,
            IProgress<string> progress)
        {
            if (!toResize.Exists) return null;

            using var image = Image.Load(toResize.FullName);

            var naturalWidth = image.Width;

            var newHeight = (int) (image.Height * ((decimal) width / naturalWidth));

            image.Mutate(ctx => ctx.Resize(width, newHeight, KnownResamplers.Bicubic));

            var newFile = Path.Combine(toResize.Directory?.FullName ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(toResize.Name)}--{imageTypeString}{(addSizeString ? $"--{image.Width}w--{image.Height}h" : string.Empty)}.jpg");

            var newFileInfo = new FileInfo(newFile);
            if (newFileInfo.Exists) newFileInfo.Delete();

            using var outImage = File.Create(newFile);
            image.SaveAsJpeg(outImage, new JpegEncoder {Quality = quality});

            return new FileInfo(newFile);
        }
    }

    public class MagicScalerImageResizer : IPictureResizer
    {
        public FileInfo ResizeTo(FileInfo toResize, int width, int quality, string imageTypeString, bool addSizeString,
            IProgress<string> progress)
        {
            if (!toResize.Exists) return null;

            var newFile = Path.Combine(toResize.Directory?.FullName ?? string.Empty, $"{Guid.NewGuid()}.jpg");

            var newFileInfo = new FileInfo(newFile);
            if (newFileInfo.Exists) newFileInfo.Delete();

            var settings = new ProcessImageSettings {Width = 400, JpegQuality = quality};

            using var outStream = new FileStream(newFileInfo.FullName, FileMode.Create);
            var results = MagicImageProcessor.ProcessImage(toResize.FullName, outStream, settings);

            outStream.Dispose();

            var finalFileName = Path.Combine(toResize.Directory?.FullName ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(toResize.Name)}--{imageTypeString}{(addSizeString ? $"--{width}w--{results.Settings.Height}h" : string.Empty)}.jpg");

            File.Move(newFileInfo.FullName, finalFileName);

            newFileInfo = new FileInfo(finalFileName);

            return newFileInfo;
        }
    }
}