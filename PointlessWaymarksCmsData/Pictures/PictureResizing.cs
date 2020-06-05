using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using PointlessWaymarksCmsData.Models;

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

        /// <summary>
        /// This deletes Image Directory jpeg files that match this programs generated sizing naming conventions. If deleteAll is true then all
        /// files will be deleted - otherwise only files where the width does not match one of the generated widths will be
        /// deleted.
        /// </summary>
        /// <param name="dbEntry"></param>
        /// <param name="deleteAll"></param>
        /// <param name="progress"></param>
        public static void CleanDisplayAndSrcSetFilesInImageDirectory(ImageContent dbEntry, bool deleteAll, IProgress<string> progress)
        {
            var currentSizes = SrcSetSizeAndQualityList().Select(x => x.size).ToList();

            progress?.Report($"Starting SrcSet Image Cleaning... Current Size List {string.Join(", ", currentSizes)}");

            var currentFiles = PictureAssetProcessing.ProcessImageDirectory(dbEntry);

            foreach (var loopFiles in currentFiles.SrcsetImages)
                if (!currentSizes.Contains(loopFiles.Width) || deleteAll)
                {
                    progress?.Report($"  Deleting {loopFiles.FileName}");
                    loopFiles.File.Delete();
                }

            currentFiles.DisplayPicture?.File?.Delete();
        }

        /// <summary>
        /// This deletes Photo Directory jpeg files that match this programs generated sizing naming conventions. If deleteAll is true then all
        /// files will be deleted - otherwise only files where the width does not match one of the generated widths will be
        /// deleted.
        /// </summary>
        /// <param name="dbEntry"></param>
        /// <param name="deleteAll"></param>
        /// <param name="progress"></param>
        public static void CleanDisplayAndSrcSetFilesInPhotoDirectory(PhotoContent dbEntry, bool deleteAll, IProgress<string> progress)
        {
            var currentSizes = SrcSetSizeAndQualityList().Select(x => x.size).ToList();

            progress?.Report($"Starting SrcSet Photo Cleaning... Current Size List {string.Join(", ", currentSizes)}");

            var currentFiles = PictureAssetProcessing.ProcessPhotoDirectory(dbEntry);

            foreach (var loopFiles in currentFiles.SrcsetImages)
                if (!currentSizes.Contains(loopFiles.Width) || deleteAll)
                {
                    progress?.Report($"  Deleting {loopFiles.FileName}");
                    loopFiles.File.Delete();
                }

            currentFiles.DisplayPicture?.File?.Delete();
        }

        public static (bool, string) CopyCleanResizeImage(ImageContent dbEntry, IProgress<string> progress)
        {
            progress?.Report($"Starting Copy, Clean and Resize for {dbEntry.Title}");

            if (dbEntry == null || string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
                return (true, "No Image to Process");

            var imageDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(dbEntry);

            var syncCopyResults = CheckImageOriginalFileIsInMediaAndContentDirectories(dbEntry);

            if (!syncCopyResults.Item1) return syncCopyResults;

            CleanDisplayAndSrcSetFilesInImageDirectory(dbEntry, true, progress);

            ResizeForDisplayAndSrcset(new FileInfo(Path.Combine(imageDirectory.FullName, dbEntry.OriginalFileName)),
                false, progress);

            return (true, $"Reached end of Copy, Clean and Resize for {dbEntry.Title}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbEntry"></param>
        /// <param name="deleteAllExisting"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static (bool, string) CopyCleanResizePhoto(PhotoContent dbEntry, bool deleteAllExisting, IProgress<string> progress)
        {
            progress?.Report($"Starting Copy, Clean and Resize for {dbEntry.Title}");

            if (dbEntry == null || string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
                return (true, "No Image to Process");

            var photoDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(dbEntry);

            var syncCopyResults = CheckPhotoOriginalFileIsInMediaAndContentDirectories(dbEntry);

            if (!syncCopyResults.Item1) return syncCopyResults;

            CleanDisplayAndSrcSetFilesInPhotoDirectory(dbEntry, deleteAllExisting, progress);

            ResizeForDisplayAndSrcset(new FileInfo(Path.Combine(photoDirectory.FullName, dbEntry.OriginalFileName)),
                false, progress);

            return (true, $"Reached end of Copy, Clean and Resize for {dbEntry.Title}");
        }

        public static FileInfo ResizeForDisplay(FileInfo fileToProcess, bool overwriteExistingFile,
            IProgress<string> progress)
        {
            int originalWidth;

            using (var sourceImage = Image.FromFile(fileToProcess.FullName))
            {
                originalWidth = sourceImage.Width;
            }

            if (originalWidth > 1200)
            {
                progress?.Report("Resize For Display: 1200, 82");

                return ResizeWithForDisplayFileName(fileToProcess, 1200, 82, overwriteExistingFile, progress);
            }

            if (originalWidth > 800)
            {
                progress?.Report("Resize For Display: 800, 82");

                return ResizeWithForDisplayFileName(fileToProcess, 800, 82, overwriteExistingFile, progress);
            }

            if (originalWidth > 400)
            {
                progress?.Report("Resize For Display: 800, 82");

                return ResizeWithForDisplayFileName(fileToProcess, 400, 82, overwriteExistingFile, progress);
            }

            return ResizeWithForDisplayFileName(fileToProcess, originalWidth, 82, overwriteExistingFile, progress);
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

        public static List<FileInfo> ResizeForSrcset(FileInfo fileToProcess, bool overwriteExistingFiles,
            IProgress<string> progress)
        {
            var sizeQualityList = SrcSetSizeAndQualityList().OrderByDescending(x => x.size).ToList();

            int originalWidth;

            using (var sourceImage = Image.FromFile(fileToProcess.FullName))
            {
                originalWidth = sourceImage.Width;
            }

            var returnList = new List<FileInfo>();

            var smallestSrcSetSize = sizeQualityList.Min(x => x.size);

            foreach (var loopSizeQuality in sizeQualityList)
                if (originalWidth >= loopSizeQuality.size || loopSizeQuality.size == smallestSrcSetSize)
                {
                    progress?.Report($"Resize: {loopSizeQuality.size}, {loopSizeQuality.quality}");
                    returnList.Add(ResizeWithWidthAndHeightFileName(fileToProcess, loopSizeQuality.size,
                        loopSizeQuality.quality, overwriteExistingFiles, progress));
                }

            return returnList;
        }

        public static FileInfo ResizeWithForDisplayFileName(FileInfo toResize, int width, int quality,
            bool overwriteExistingFiles, IProgress<string> progress)
        {
            if (toResize == null || !toResize.Exists || toResize.Directory == null)
            {
                progress?.Report("No input to resize?");
                return null;
            }

            progress?.Report($"Display Resizing {toResize.Name} - Width {width} and Quality {quality} - starting");

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

            var resizer = new MagicScalerImageResizer();

            return resizer.ResizeTo(toResize, width, quality, "For-Display", true, progress);
        }


        public static FileInfo ResizeWithWidthAndHeightFileName(FileInfo toResize, int width, int quality,
            bool overwriteExistingFiles, IProgress<string> progress)
        {
            if (toResize == null || !toResize.Exists || toResize.Directory == null)
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

            var resizer = new MagicScalerImageResizer();

            return resizer.ResizeTo(toResize, width, quality, "Sized", true, progress);
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