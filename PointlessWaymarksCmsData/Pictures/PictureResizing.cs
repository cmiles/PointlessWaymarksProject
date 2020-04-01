using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PointlessWaymarksCmsData.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PointlessWaymarksCmsData.Pictures
{
    public static class PictureResizing
    {
        public static (bool, string) CheckFileOriginalFileIsInMediaAndContentDirectories(FileContent dbContent)
        {
            UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllFolders();

            if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName)) return (true, "No Original File to process");

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMasterMediaArchiveFileDirectory().FullName,
                dbContent.OriginalFileName));

            var fileContentDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(dbContent);

            var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, dbContent.OriginalFileName));

            if (!archiveFile.Exists && !contentFile.Exists)
                return (false,
                    $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                    $"there appears to be a file missing for File Title {dbContent.Title} " + $"slug {dbContent.Slug}");

            if (archiveFile.Exists && !contentFile.Exists) archiveFile.CopyTo(contentFile.FullName);

            if (!archiveFile.Exists && contentFile.Exists) contentFile.CopyTo(archiveFile.FullName);

            archiveFile.Refresh();
            contentFile.Refresh();

            var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

            if (bothFilesPresent)
                return (true, $"Archive and Content File Present - {archiveFile.FullName}, {contentFile.FullName}");

            return (false,
                $"There was a problem - Archive File Present: {archiveFile.Exists}, " +
                $"Content File Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}");
        }

        public static (bool, string) CheckImageOriginalFileIsInMediaAndContentDirectories(ImageContent dbContent)
        {
            UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllFolders();

            if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName)) return (true, "No Original File to process");

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMasterMediaArchiveImageDirectory().FullName,
                dbContent.OriginalFileName));

            var imageContentDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(dbContent);

            var contentFile = new FileInfo(Path.Combine(imageContentDirectory.FullName, dbContent.OriginalFileName));

            if (!archiveFile.Exists && !contentFile.Exists)
                return (false,
                    $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                    $"there appears to be a file missing for Image Title {dbContent.Title} " +
                    $"slug {dbContent.Slug}");

            if (archiveFile.Exists && !contentFile.Exists) archiveFile.CopyTo(contentFile.FullName);

            if (!archiveFile.Exists && contentFile.Exists) contentFile.CopyTo(archiveFile.FullName);

            archiveFile.Refresh();
            contentFile.Refresh();

            var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

            if (bothFilesPresent)
                return (true, $"Archive and Content File Present - {archiveFile.FullName}, {contentFile.FullName}");

            return (false,
                $"There was a problem - Archive File Present: {archiveFile.Exists}, " +
                $"Content File Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}");
        }

        public static (bool, string) CheckPhotoOriginalFileIsInMediaAndContentDirectories(PhotoContent dbContent)
        {
            UserSettingsSingleton.CurrentSettings().VerifyOrCreateAllFolders();

            if (string.IsNullOrWhiteSpace(dbContent.OriginalFileName)) return (true, "No Original File to process");

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMasterMediaArchivePhotoDirectory().FullName,
                dbContent.OriginalFileName));

            var photoContentDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(dbContent);

            var contentFile = new FileInfo(Path.Combine(photoContentDirectory.FullName, dbContent.OriginalFileName));

            if (!archiveFile.Exists && !contentFile.Exists)
                return (false,
                    $"Neither {archiveFile.FullName} nor {contentFile.FullName} exists - " +
                    $"there appears to be a file missing for Photo Title {dbContent.Title} " +
                    $"slug {dbContent.Slug}");

            if (archiveFile.Exists && !contentFile.Exists) archiveFile.CopyTo(contentFile.FullName);

            if (!archiveFile.Exists && contentFile.Exists) contentFile.CopyTo(archiveFile.FullName);

            archiveFile.Refresh();
            contentFile.Refresh();

            var bothFilesPresent = archiveFile.Exists && contentFile.Exists;

            if (bothFilesPresent)
                return (true, $"Archive and Content File Present - {archiveFile.FullName}, {contentFile.FullName}");

            return (false,
                $"There was a problem - Archive File Present: {archiveFile.Exists}, " +
                $"Content File Present {contentFile.Exists} - {archiveFile.FullName}; {contentFile.FullName}");
        }

        public static void CleanSrcSetFilesInImageDirectory(ImageContent dbEntry, IProgress<string> progress)
        {
            var currentSizes = SrcSetSizeAndQualityList().Select(x => x.size).ToList();

            progress?.Report($"Starting SrcSet Image Cleaning... Current Size List {string.Join(", ", currentSizes)}");

            var currentFiles = PictureAssetProcessing.ProcessImageDirectory(dbEntry);

            foreach (var loopFiles in currentFiles.SrcsetImages)
                if (!currentSizes.Contains(loopFiles.Width))
                {
                    progress?.Report($"  Deleting {loopFiles.FileName}");
                    loopFiles.File.Delete();
                }
        }

        public static void CleanSrcSetFilesInPhotoDirectory(PhotoContent dbEntry, IProgress<string> progress)
        {
            var currentSizes = SrcSetSizeAndQualityList().Select(x => x.size).ToList();

            progress?.Report($"Starting SrcSet Photo Cleaning... Current Size List {string.Join(", ", currentSizes)}");

            var currentFiles = PictureAssetProcessing.ProcessPhotoDirectory(dbEntry);

            foreach (var loopFiles in currentFiles.SrcsetImages)
                if (!currentSizes.Contains(loopFiles.Width))
                {
                    progress?.Report($"  Deleting {loopFiles.FileName}");
                    loopFiles.File.Delete();
                }
        }

        public static (bool, string) CopyCleanResizeImage(ImageContent dbEntry, IProgress<string> progress)
        {
            progress?.Report($"Starting Copy, Clean and Resize for {dbEntry.Title}");

            if (dbEntry == null || string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
                return (true, "No Image to Process");

            var imageDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(dbEntry);

            var syncCopyResults = CheckImageOriginalFileIsInMediaAndContentDirectories(dbEntry);

            if (!syncCopyResults.Item1) return syncCopyResults;

            CleanSrcSetFilesInImageDirectory(dbEntry, progress);

            ResizeForDisplayAndSrcset(new FileInfo(Path.Combine(imageDirectory.FullName, dbEntry.OriginalFileName)),
                false, progress);

            return (true, $"Reached end of Copy, Clean and Resize for {dbEntry.Title}");
        }

        public static (bool, string) CopyCleanResizePhoto(PhotoContent dbEntry, IProgress<string> progress)
        {
            progress?.Report($"Starting Copy, Clean and Resize for {dbEntry.Title}");

            if (dbEntry == null || string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
                return (true, "No Image to Process");

            var photoDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(dbEntry);

            var syncCopyResults = CheckPhotoOriginalFileIsInMediaAndContentDirectories(dbEntry);

            if (!syncCopyResults.Item1) return syncCopyResults;

            CleanSrcSetFilesInPhotoDirectory(dbEntry, progress);

            ResizeForDisplayAndSrcset(new FileInfo(Path.Combine(photoDirectory.FullName, dbEntry.OriginalFileName)),
                false, progress);

            return (true, $"Reached end of Copy, Clean and Resize for {dbEntry.Title}");
        }

        public static FileInfo ResizeForDisplay(FileInfo fullName, bool overwriteExistingFile,
            IProgress<string> progress)
        {
            int originalWidth;

            using (var originalImage = File.OpenRead(fullName.FullName))
            {
                var imageInfo = Image.Identify(originalImage);
                originalWidth = imageInfo.Width;
            }

            if (originalWidth > 1200)
            {
                progress?.Report("Resize For Display: 1200, 82");

                return ResizeWithForDisplayFileName(fullName, 1200, 82, overwriteExistingFile, progress);
            }

            if (originalWidth > 800)
            {
                progress?.Report("Resize For Display: 800, 82");

                return ResizeWithForDisplayFileName(fullName, 800, 82, overwriteExistingFile, progress);
            }

            if (originalWidth > 400)
            {
                progress?.Report("Resize For Display: 800, 82");

                return ResizeWithWidthAndHeightFileName(fullName, 400, 82, overwriteExistingFile, progress);
            }

            FileInfo newFile;

            using (var image = Image.Load(fullName.FullName))
            {
                progress?.Report("Resize For Display: Natural Size, 82");

                newFile = new FileInfo(Path.Combine(fullName.Directory?.FullName ?? string.Empty,
                    $"{Path.GetFileNameWithoutExtension(fullName.Name)}--For-Display.jpg"));

                if (newFile.Exists && !overwriteExistingFile)
                {
                    progress?.Report($"Found existing file - {newFile.FullName}");
                    return newFile;
                }

                if (newFile.Exists)
                {
                    newFile.Delete();
                    newFile.Refresh();
                }

                using var outImage = File.Create(newFile.FullName);
                image.SaveAsJpeg(outImage, new JpegEncoder {Quality = 82});

                newFile.Refresh();
            }

            return newFile;
        }

        public static List<FileInfo> ResizeForDisplayAndSrcset(FileInfo originalImage, bool overwriteExistingFiles,
            IProgress<string> progress)
        {
            var fullList = new List<FileInfo>
            {
                originalImage, ResizeForDisplay(originalImage, overwriteExistingFiles, progress)
            };

            fullList.AddRange(ResizeForSrcset(originalImage, overwriteExistingFiles, progress));

            return fullList;
        }

        public static List<FileInfo> ResizeForSrcset(FileInfo fullName, bool overwriteExistingFiles,
            IProgress<string> progress)
        {
            var sizeQualityList = SrcSetSizeAndQualityList().OrderByDescending(x => x.size).ToList();

            int originalWidth;

            using (var originalImage = File.OpenRead(fullName.FullName))
            {
                var imageInfo = Image.Identify(originalImage);
                originalWidth = imageInfo.Width;
            }

            var returnList = new List<FileInfo>();

            var smallestSrcSetSize = sizeQualityList.Min(x => x.size);

            foreach (var loopSizeQuality in sizeQualityList)
                if (originalWidth >= loopSizeQuality.size || loopSizeQuality.size == smallestSrcSetSize)
                {
                    progress?.Report($"Resize: {loopSizeQuality.size}, {loopSizeQuality.quality}");
                    returnList.Add(ResizeWithWidthAndHeightFileName(fullName, loopSizeQuality.size,
                        loopSizeQuality.quality, overwriteExistingFiles, progress));
                }

            return returnList;
        }

        public static FileInfo ResizeWithForDisplayFileName(FileInfo toResize, int width, int quality,
            bool overwriteExistingFiles, IProgress<string> progress)
        {
            if (toResize == null || !toResize.Exists)
            {
                progress?.Report("No input to resize?");
                return null;
            }

            progress?.Report($"Display Resizing {toResize.Name} - Width {width} and Quality {quality} - starting");

            string newFile;

            if (!overwriteExistingFiles)
            {
                var possibleExistingFile = toResize.Directory.GetFiles(
                    $"{Path.GetFileNameWithoutExtension(toResize.Name)}--For-Display--{width}w--*h.jpg",
                    SearchOption.TopDirectoryOnly);

                if (possibleExistingFile.Any())
                {
                    progress?.Report(
                        $"Display Resizing {toResize.Name} -  Found existing Width {width} file - {possibleExistingFile.First().Name} - ending resize.");
                    return possibleExistingFile.First();
                }
            }

            using (var image = Image.Load(toResize.FullName))
            {
                var naturalWidth = image.Width;

                var newHeight = (int) (image.Height * ((decimal) width / naturalWidth));

                image.Mutate(ctx => ctx.Resize(width, newHeight, KnownResamplers.Bicubic));

                newFile = Path.Combine(toResize.Directory?.FullName ?? string.Empty,
                    $"{Path.GetFileNameWithoutExtension(toResize.Name)}--For-Display--{image.Width}w--{image.Height}h.jpg");

                var newFileInfo = new FileInfo(newFile);
                if (newFileInfo.Exists) newFileInfo.Delete();

                using var outImage = File.Create(newFile);
                image.SaveAsJpeg(outImage, new JpegEncoder {Quality = quality});
            }

            return new FileInfo(newFile);
        }

        public static FileInfo ResizeWithWidthAndHeightFileName(FileInfo toResize, int width, int quality,
            bool overwriteExistingFiles, IProgress<string> progress)
        {
            if (toResize == null || !toResize.Exists)
            {
                progress?.Report("No input to resize?");
                return null;
            }

            progress?.Report($"Resizing {toResize.Name} - Starting Width {width} and Quality {quality}");

            if (!overwriteExistingFiles)
            {
                var possibleExistingFile = toResize.Directory.GetFiles(
                    $"{Path.GetFileNameWithoutExtension(toResize.Name)}--Sized--{width}w--*.jpg",
                    SearchOption.TopDirectoryOnly);

                if (possibleExistingFile.Any())
                {
                    progress?.Report(
                        $"Resizing {toResize.Name} - Found existing Width {width} file - {possibleExistingFile.First().Name} - ending resize.");
                    return possibleExistingFile.First();
                }
            }

            string newFile;

            using (var image = Image.Load(toResize.FullName))
            {
                var naturalWidth = image.Width;

                var newHeight = (int) (image.Height * ((decimal) width / naturalWidth));

                image.Mutate(ctx => ctx.Resize(width, newHeight, KnownResamplers.Bicubic));

                newFile = Path.Combine(toResize.Directory?.FullName ?? string.Empty,
                    $"{Path.GetFileNameWithoutExtension(toResize.Name)}--Sized--{image.Width}w--{image.Height}h.jpg");

                var newFileInfo = new FileInfo(newFile);
                if (newFileInfo.Exists) newFileInfo.Delete();

                using var outImage = File.Create(newFile);
                image.SaveAsJpeg(outImage, new JpegEncoder {Quality = quality});
            }

            return new FileInfo(newFile);
        }

        public static List<(int size, int quality)> SrcSetSizeAndQualityList()
        {
            return new List<(int size, int quality)>
            {
                (4000, 80),
                (3000, 80),
                (1920, 80),
                (1600, 80),
                (1440, 80),
                (1024, 80),
                (768, 72),
                (640, 72),
                (320, 70),
                (210, 70),
                (100, 70)
            };
        }
    }
}