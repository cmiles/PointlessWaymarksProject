using PhotoSauce.MagicScaler;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Content
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
            IProgress<string>? progress = null)
        {
            var currentSizes = SrcSetSizeAndQualityList().Select(x => x.size).ToList();

            progress?.Report($"Starting SrcSet Image Cleaning... Current Size List {string.Join(", ", currentSizes)}");

            var currentFiles = PictureAssetProcessing.ProcessImageDirectory(dbEntry);

            if (currentFiles != null)
                foreach (var loopFiles in currentFiles.SrcsetImages.Where(x => x.File != null).ToList())
                    if (!currentSizes.Contains(loopFiles.Width) || deleteAll)
                    {
                        progress?.Report($"  Deleting {loopFiles.File?.FullName}");
                        loopFiles.File?.Delete();
                    }

            var sourceFileReference =
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageContentFile(dbEntry);
            var expectedDisplayWidth = DisplayPictureWidth(sourceFileReference!);

            if (currentFiles?.DisplayPicture != null && currentFiles.DisplayPicture.Width != expectedDisplayWidth ||
                deleteAll)
                currentFiles?.DisplayPicture?.File?.Delete();
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
            IProgress<string>? progress = null)
        {
            var currentSizes = SrcSetSizeAndQualityList().Select(x => x.size).ToList();

            progress?.Report($"Starting SrcSet Photo Cleaning... Current Size List {string.Join(", ", currentSizes)}");

            var currentFiles = PictureAssetProcessing.ProcessPhotoDirectory(dbEntry);

            if (currentFiles != null)
                foreach (var loopFiles in currentFiles.SrcsetImages.Where(x => x.File != null))
                    if (!currentSizes.Contains(loopFiles.Width) || deleteAll)
                    {
                        progress?.Report($"  Deleting {loopFiles.File?.FullName}");
                        loopFiles.File?.Delete();
                    }

            var sourceFileReference =
                UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoContentFile(dbEntry);
            var expectedDisplayWidth = DisplayPictureWidth(sourceFileReference!);

            if (currentFiles?.DisplayPicture != null && currentFiles.DisplayPicture.Width != expectedDisplayWidth ||
                deleteAll)
                currentFiles?.DisplayPicture?.File?.Delete();
        }

        public static async Task<GenerationReturn> CopyCleanResizeImage(ImageContent dbEntry,
            IProgress<string>? progress = null)
        {
            progress?.Report($"Starting Copy, Clean and Resize for {dbEntry.Title}");

            if (string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
                return GenerationReturn.Error($"Image {dbEntry.Title} has no Original File", dbEntry.ContentId);

            var imageDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(dbEntry);

            var syncCopyResults = await FileManagement.CheckImageFileIsInMediaAndContentDirectories(dbEntry).ConfigureAwait(false);

            if (syncCopyResults.HasError) return syncCopyResults;

            CleanDisplayAndSrcSetFilesInImageDirectory(dbEntry, true, progress);

            await ResizeForDisplayAndSrcset(
                new FileInfo(Path.Combine(imageDirectory.FullName, dbEntry.OriginalFileName)),
                false, progress).ConfigureAwait(false);

            return GenerationReturn.Success($"{dbEntry.Title} Copied, Cleaned, Resized", dbEntry.ContentId);
        }

        /// <summary>
        /// </summary>
        /// <param name="dbEntry"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static async Task<GenerationReturn> CopyCleanResizePhoto(PhotoContent dbEntry,
            IProgress<string>? progress = null)
        {
            progress?.Report($"Starting Copy, Clean and Resize for {dbEntry.Title}");

            if (string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
                return GenerationReturn.Error($"Photo {dbEntry.Title} has no Original File", dbEntry.ContentId);

            var photoDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(dbEntry);

            var syncCopyResults = await FileManagement.CheckPhotoFileIsInMediaAndContentDirectories(dbEntry).ConfigureAwait(false);

            if (syncCopyResults.HasError) return syncCopyResults;

            CleanDisplayAndSrcSetFilesInPhotoDirectory(dbEntry, true, progress);

            await ResizeForDisplayAndSrcset(
                new FileInfo(Path.Combine(photoDirectory.FullName, dbEntry.OriginalFileName)),
                false, progress).ConfigureAwait(false);

            return GenerationReturn.Success($"{dbEntry.Title} Copied, Cleaned, Resized", dbEntry.ContentId);
        }

        /// <summary>
        ///     Deletes files from the local content directory of the dbEntry that are supported Photo file types but
        ///     don't match the original file name in the dbEntry. Use with caution and make sure that the PhotoContent
        ///     is current/has the intended values.
        /// </summary>
        /// <param name="dbEntry"></param>
        /// <param name="progress"></param>
        public static void DeleteSupportedPictureFilesInDirectoryOtherThanOriginalFile(PhotoContent dbEntry,
            IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
            {
                progress?.Report("Nothing to delete.");
                return;
            }

            var baseFileNameList = dbEntry.OriginalFileName.Split(".").ToList();
            var baseFileName = string.Join("", baseFileNameList.Take(baseFileNameList.Count - 1));

            var directoryInfo = UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(dbEntry);

            var fileVariants = directoryInfo.GetFiles().Where(x =>
                FolderFileUtility.PictureFileTypeIsSupported(x) && !x.Name.StartsWith($"{baseFileName}--") &&
                x.Name != dbEntry.OriginalFileName).ToList();

            progress?.Report(
                $"Found {fileVariants.Count} Supported Photo File Type files in {directoryInfo.FullName} that don't match the " +
                $"original file name {dbEntry.OriginalFileName} from {dbEntry.Title}");

            fileVariants.ForEach(x => x.Delete());
        }

        /// <summary>
        ///     Deletes files from the local content directory of the dbEntry that are supported Photo file types but
        ///     don't match the original file name in the dbEntry. Use with caution and make sure that the PhotoContent
        ///     is current/has the intended values.
        /// </summary>
        /// <param name="dbEntry"></param>
        /// <param name="progress"></param>
        public static void DeleteSupportedPictureFilesInDirectoryOtherThanOriginalFile(ImageContent dbEntry,
            IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
            {
                progress?.Report("Nothing to delete.");
                return;
            }

            var baseFileNameList = dbEntry.OriginalFileName.Split(".").ToList();
            var baseFileName = string.Join("", baseFileNameList.Take(baseFileNameList.Count - 1));

            var directoryInfo = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(dbEntry);

            var fileVariants = directoryInfo.GetFiles().Where(x =>
                FolderFileUtility.PictureFileTypeIsSupported(x) && !x.Name.StartsWith($"{baseFileName}--")).ToList();

            progress?.Report(
                $"Found {fileVariants.Count} Supported Image File Type files in {directoryInfo.FullName} that don't match the " +
                $"original file name {dbEntry.OriginalFileName} from {dbEntry.Title}");

            fileVariants.ForEach(x => x.Delete());
        }

        public static int DisplayPictureWidth(FileInfo fileToProcess)
        {
            var imageInfo = ImageFileInfo.Load(fileToProcess.FullNameWithLongFilePrefix());

            var originalWidth = imageInfo.Frames[0].Width;

            if (originalWidth > 1200) return 1200;

            if (originalWidth > 800) return 800;

            if (originalWidth > 400) return 400;

            return originalWidth;
        }

        public static async Task<FileInfo?> ResizeForDisplay(FileInfo fileToProcess, bool overwriteExistingFile,
            IProgress<string>? progress = null)
        {
            var displayWidth = DisplayPictureWidth(fileToProcess);

            progress?.Report($"Resize For Display: {displayWidth}, 82");

            return await ResizeWithForDisplayFileName(fileToProcess, displayWidth, 82, overwriteExistingFile, progress).ConfigureAwait(false);
        }

        public static async Task<List<FileInfo>> ResizeForDisplayAndSrcset(PhotoContent dbEntry,
            bool overwriteExistingFiles, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
                throw new ArgumentException(
                    "ResizeForDisplayAndSrcset in PictureResizing was giving a PhotoContentDbEntry with a null or empty OriginalFileName.");

            await FileManagement.CheckPhotoFileIsInMediaAndContentDirectories(dbEntry).ConfigureAwait(false);

            var targetDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(dbEntry);

            var sourceImage = new FileInfo(Path.Combine(targetDirectory.FullName, dbEntry.OriginalFileName));

            return await ResizeForDisplayAndSrcset(sourceImage, overwriteExistingFiles, progress).ConfigureAwait(false);
        }

        public static async Task<List<FileInfo>> ResizeForDisplayAndSrcset(ImageContent dbEntry,
            bool overwriteExistingFiles, IProgress<string>? progress = null)
        {
            await FileManagement.CheckImageFileIsInMediaAndContentDirectories(dbEntry).ConfigureAwait(false);

            var targetDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(dbEntry);

            if (string.IsNullOrWhiteSpace(dbEntry.OriginalFileName))
            {
                var toThrow =
                    new Exception(
                        "The Image DbEntry did not have valid information on the Original File Name for the image");
                toThrow.Data.Add("Post DbEntry", dbEntry.SafeObjectDump());
                throw toThrow;
            }

            var sourceImage = new FileInfo(Path.Combine(targetDirectory.FullName, dbEntry.OriginalFileName));

            return await ResizeForDisplayAndSrcset(sourceImage, overwriteExistingFiles, progress).ConfigureAwait(false);
        }

        public static async Task<List<FileInfo>> ResizeForDisplayAndSrcset(FileInfo originalImage,
            bool overwriteExistingFiles,
            IProgress<string>? progress = null)
        {
            var fullList = new List<FileInfo>
            {
                originalImage, (await ResizeForDisplay(originalImage, overwriteExistingFiles, progress).ConfigureAwait(false))!
            };

            fullList.AddRange(await ResizeForSrcset(originalImage, overwriteExistingFiles, progress).ConfigureAwait(false));

            return fullList;
        }

        public static async Task<List<FileInfo>> ResizeForSrcset(FileInfo fileToProcess, bool overwriteExistingFiles,
            IProgress<string>? progress = null)
        {
            var sizeQualityList = SrcSetSizeAndQualityList().OrderByDescending(x => x.size).ToList();

            var imageInfo = ImageFileInfo.Load(fileToProcess.FullNameWithLongFilePrefix());

            var originalWidth = imageInfo.Frames[0].Width;

            var returnList = new List<FileInfo>();

            var smallestSrcSetSize = sizeQualityList.Min(x => x.size);

            foreach (var (size, quality) in sizeQualityList)
                if (originalWidth >= size || size == smallestSrcSetSize)
                {
                    progress?.Report($"Resize: {size}, {quality}");
                    returnList.Add((await ResizeWithWidthAndHeightFileName(fileToProcess, size, quality,
                        overwriteExistingFiles, progress).ConfigureAwait(false))!);
                }

            return returnList;
        }

        public static async Task<FileInfo?> ResizeWithForDisplayFileName(FileInfo toResize, int width, int quality,
            bool overwriteExistingFiles, IProgress<string>? progress = null)
        {
            if (!toResize.Exists || toResize.Directory == null)
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

            return await resizer.ResizeTo(toResize, width, quality, "For-Display", true, progress).ConfigureAwait(false);
        }


        public static async Task<FileInfo?> ResizeWithWidthAndHeightFileName(FileInfo toResize, int width, int quality,
            bool overwriteExistingFiles, IProgress<string>? progress = null)
        {
            if (!toResize.Exists || toResize.Directory == null)
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

            return await resizer.ResizeTo(toResize, width, quality, "Sized", true, progress).ConfigureAwait(false);
        }

        public static List<(int size, int quality)> SrcSetSizeAndQualityList()
        {
            return new()
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