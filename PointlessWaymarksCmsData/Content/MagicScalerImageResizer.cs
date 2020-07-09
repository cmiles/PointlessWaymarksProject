using System;
using System.IO;
using PhotoSauce.MagicScaler;
using PhotoSauce.MagicScaler.Transforms;

namespace PointlessWaymarksCmsData.Content
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

            var settings = new ProcessImageSettings {Width = width, JpegQuality = quality};

            using var outStream = new FileStream(newFileInfo.FullName, FileMode.Create);
            var results = MagicImageProcessor.ProcessImage(toResize.FullName, outStream, settings);

            outStream.Dispose();

            var finalFileName = Path.Combine(toResize.Directory?.FullName ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(toResize.Name)}--{imageTypeString}{(addSizeString ? $"--{width}w--{results.Settings.Height}h" : string.Empty)}.jpg");

            File.Move(newFileInfo.FullName, finalFileName);

            newFileInfo = new FileInfo(finalFileName);

            return newFileInfo;
        }

        public FileInfo Rotate(FileInfo toRotate, PhotoSauce.MagicScaler.Orientation orientation)
        {
            if (!toRotate.Exists) return null;

            var newFile = Path.Combine(toRotate.Directory?.FullName ?? string.Empty, $"{Guid.NewGuid()}.jpg");

            var newFileInfo = new FileInfo(newFile);
            if (newFileInfo.Exists) newFileInfo.Delete();

            using var pl = MagicImageProcessor.BuildPipeline(toRotate.FullName, new ProcessImageSettings());
            pl.AddTransform(new OrientationTransform(orientation));
            using var outStream = new FileStream(newFileInfo.FullName, FileMode.Create);

            pl.WriteOutput(outStream);

            pl.Dispose();
            outStream.Dispose();
            
            var finalFileName = toRotate.FullName;

            toRotate.Delete();

            File.Move(newFileInfo.FullName, finalFileName);

            newFileInfo = new FileInfo(finalFileName);

            return newFileInfo;
        }
    }
}