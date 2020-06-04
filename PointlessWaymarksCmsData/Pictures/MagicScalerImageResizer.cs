using System;
using System.IO;
using PhotoSauce.MagicScaler;

namespace PointlessWaymarksCmsData.Pictures
{
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