using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.FolderStructureAndGeneratedContent;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.Pictures
{
    public static class PictureResizing
    {
        /// <summary>
        ///     This deletes Image Directory jpeg files that match this programs generated sizing naming conventions. If deleteAll
        ///     is true then all
        ///     files will be deleted - otherwise only files where the width does not match one of the generated widths will be
        ///     deleted.
        /// </summary>
        /// <param name="dbEntry"></param>
        /// <param name="deleteAll"></param>
        /// <param name="progress"></param>
        public static void CleanDisplayAndSrcSetFilesInImageDirectory(ImageContent dbEntry, bool deleteAll,
            IProgress<string> progress)
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
        ///     This deletes Photo Directory jpeg files that match this programs generated sizing naming conventions. If deleteAll
        ///     is true then all
        ///     files will be deleted - otherwise only files where the width does not match one of the generated widths will be
        ///     deleted.
        /// </summary>
        /// <param name="dbEntry"></param>
        /// <param name="deleteAll"></param>
        /// <param name="progress"></param>
        public static void CleanDisplayAndSrcSetFilesInPhotoDirectory(PhotoContent dbEntry, bool deleteAll,
            IProgress<string> progress)
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

        public static async Task<GenerationReturn> CopyCleanResizeImage(ImageContent dbEntry,
            IProgress<string> progress)
        {
            if (dbEntry == null)
                return await GenerationReturn.Error("Null Image Content submitted to Copy Clean and Resize");

            progress?.Report($"Starting Copy, Clean and Resize for {dbEntry.Title}");

            if (string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
                await GenerationReturn.Error($"Image {dbEntry.Title} has no Original File", dbEntry.ContentId);

            var imageDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(dbEntry);

            var syncCopyResults =
                await StructureAndMediaContent.CheckImageFileIsInMediaAndContentDirectories(dbEntry, progress);

            if (!syncCopyResults.HasError) return syncCopyResults;

            CleanDisplayAndSrcSetFilesInImageDirectory(dbEntry, true, progress);

            ResizeForDisplayAndSrcset(new FileInfo(Path.Combine(imageDirectory.FullName, dbEntry.OriginalFileName)),
                false, progress);

            return await GenerationReturn.Success($"{dbEntry.Title} Copied, Cleaned, Resized", dbEntry.ContentId);
        }

        /// <summary>
        /// </summary>
        /// <param name="dbEntry"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static async Task<GenerationReturn> CopyCleanResizePhoto(PhotoContent dbEntry,
            IProgress<string> progress)
        {
            if (dbEntry == null)
                return await GenerationReturn.Error("Null Photo Content submitted to Copy Clean and Resize");

            progress?.Report($"Starting Copy, Clean and Resize for {dbEntry.Title}");

            if (string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
                await GenerationReturn.Error($"Photo {dbEntry.Title} has no Original File", dbEntry.ContentId);

            var photoDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(dbEntry);

            var syncCopyResults =
                await StructureAndMediaContent.CheckPhotoFileIsInMediaAndContentDirectories(dbEntry, progress);

            if (!syncCopyResults.HasError) return syncCopyResults;

            CleanDisplayAndSrcSetFilesInPhotoDirectory(dbEntry, true, progress);

            ResizeForDisplayAndSrcset(new FileInfo(Path.Combine(photoDirectory.FullName, dbEntry.OriginalFileName)),
                false, progress);

            return await GenerationReturn.Success($"{dbEntry.Title} Copied, Cleaned, Resized", dbEntry.ContentId);
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

        public static async Task<List<FileInfo>> ResizeForDisplayAndSrcset(PhotoContent dbEntry,
            bool overwriteExistingFiles, IProgress<string> progress)
        {
            await StructureAndMediaContent.CheckPhotoFileIsInMediaAndContentDirectories(dbEntry, progress);

            var targetDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(dbEntry);

            var sourceImage = new FileInfo(Path.Combine(targetDirectory.FullName, dbEntry.OriginalFileName));

            return ResizeForDisplayAndSrcset(sourceImage, overwriteExistingFiles, progress);
        }

        public static async Task<List<FileInfo>> ResizeForDisplayAndSrcset(ImageContent dbEntry,
            bool overwriteExistingFiles, IProgress<string> progress)
        {
            await StructureAndMediaContent.CheckImageFileIsInMediaAndContentDirectories(dbEntry, progress);

            var targetDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(dbEntry);

            var sourceImage = new FileInfo(Path.Combine(targetDirectory.FullName, dbEntry.OriginalFileName));

            return ResizeForDisplayAndSrcset(sourceImage, overwriteExistingFiles, progress);
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