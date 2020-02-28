using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using PointlessWaymarksCmsData.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PointlessWaymarksCmsData.Pictures
{
    public static class PictureResizing
    {
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

        public static async Task ProcessAndUploadImageFile(FileInfo originalImage, List<string> bucketSubdirectoryList,
            bool overwriteExistingSourceFilesInResize, IProgress<string> progress)
        {
            var fullList = new List<FileInfo>
            {
                originalImage, ResizeForDisplay(originalImage, overwriteExistingSourceFilesInResize, progress)
            };

            fullList.AddRange(ResizeForSrcset(originalImage, overwriteExistingSourceFilesInResize, progress));

            var fileCount = fullList.Count;
            var currentFileCount = 1;

            foreach (var loopFullList in fullList)
            {
                progress?.Report($"Uploading File {currentFileCount} of {fileCount}, {loopFullList.FullName}");
                await UploadFileAsync(loopFullList, bucketSubdirectoryList, progress);
                currentFileCount++;
            }
        }

        public static async Task ProcessAndUploadImageHtmlFile(FileInfo originalImage,
            List<string> bucketSubdirectoryList, bool overwriteExistingSourceFilesInResize, IProgress<string> progress)
        {
            var fullList = new List<FileInfo>
            {
                originalImage, ResizeForDisplay(originalImage, overwriteExistingSourceFilesInResize, progress)
            };

            fullList.AddRange(ResizeForSrcset(originalImage, overwriteExistingSourceFilesInResize, progress));

            var fileCount = fullList.Count;
            var currentFileCount = 1;

            foreach (var loopFullList in fullList)
            {
                progress?.Report($"Uploading File {currentFileCount} of {fileCount}, {loopFullList.FullName}");
                await UploadFileAsync(loopFullList, bucketSubdirectoryList, progress);
                currentFileCount++;
            }
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

            progress?.Report(
                $"Starting Display Image Resizing {toResize.FullName} to Width {width} and Quality {quality}");

            string newFile;

            if (!overwriteExistingFiles)
            {
                progress?.Report("Checking for existing file...");

                var possibleExistingFile = toResize.Directory.GetFiles(
                    $"{Path.GetFileNameWithoutExtension(toResize.Name)}--For-Display--{width}w--*h.jpg",
                    SearchOption.TopDirectoryOnly);

                if (possibleExistingFile.Any())
                    progress?.Report(
                        $"Found existing file at this width - {possibleExistingFile.First().FullName} - ending resize.");
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

            progress?.Report($"Starting Resizing {toResize.FullName} to Width {width} and Quality {quality}");

            if (!overwriteExistingFiles)
            {
                progress?.Report("Checking for existing file...");

                var possibleExistingFile = toResize.Directory.GetFiles(
                    $"{Path.GetFileNameWithoutExtension(toResize.Name)}--Sized--{width}w--*.jpg",
                    SearchOption.TopDirectoryOnly);

                if (possibleExistingFile.Any())
                {
                    progress?.Report(
                        $"Found existing file at this width - {possibleExistingFile.First().FullName} - ending resize.");
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
                (2000, 80),
                (1600, 80),
                (1200, 80),
                (800, 72),
                (500, 72),
                (300, 70),
                (200, 70),
                (100, 70)
            };
        }

        public static async Task UploadFileAsync(FileInfo toUpload, List<string> bucketSubdirectoryList,
            IProgress<string> progress)
        {
            if (bucketSubdirectoryList == null) bucketSubdirectoryList = new List<string>();

            var settings = UserSettingsSingleton.CurrentSettings();

            var finalBucketName = bucketSubdirectoryList.Aggregate(settings.AmazonS3Bucket,
                (current, loopSubDirs) => $@"{current}/{loopSubDirs}");

            progress?.Report($"Upload {toUpload.FullName} to Directory {finalBucketName} Setup");

            var awsCredentials = new BasicAWSCredentials(settings.AmazonS3AccessKey, settings.AmazonS3SecretKey);

            IAmazonS3 client = new AmazonS3Client(awsCredentials);

            var fileTransferUtility = new TransferUtility(client);

            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
            {
                BucketName = finalBucketName,
                FilePath = toUpload.FullName,
                StorageClass = S3StorageClass.Standard,
                PartSize = 6291456, // 6 MB.
                Key = toUpload.Name,
                CannedACL = S3CannedACL.PublicRead
            };

            progress?.Report($"Upload {toUpload.FullName} to Directory {finalBucketName} Uploading...");

            await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
        }
    }
}