using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TheLemmonWorkshopWpfControls.Utility
{
    namespace PictureHelper02.Controls.ImageLoader
    {
        public static class ImageResizing
        {
            public static async Task ProcessAndUploadImageFile(FileInfo originalImage,
                List<string> bucketSubdirectoryList, IProgress<string> progress)
            {
                var fullList = new List<FileInfo> {originalImage, ResizeForDisplay(originalImage, progress)};

                fullList.AddRange(ResizeForSrcset(originalImage, progress));

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
                List<string> bucketSubdirectoryList, IProgress<string> progress)
            {
                var fullList = new List<FileInfo> {originalImage, ResizeForDisplay(originalImage, progress)};

                fullList.AddRange(ResizeForSrcset(originalImage, progress));

                var fileCount = fullList.Count;
                var currentFileCount = 1;

                foreach (var loopFullList in fullList)
                {
                    progress?.Report($"Uploading File {currentFileCount} of {fileCount}, {loopFullList.FullName}");
                    await UploadFileAsync(loopFullList, bucketSubdirectoryList, progress);
                    currentFileCount++;
                }
            }

            public static FileInfo ResizeForDisplay(FileInfo fullName, IProgress<string> progress)
            {
                int originalWidth;

                using (var originalImage = File.OpenRead(fullName.FullName))
                {
                    var imageInfo = Image.Identify(originalImage);
                    originalWidth = imageInfo.Width;
                }

                if (originalWidth > 1200)
                {
                    progress.Report("Resize For Display: 1200, 82");

                    return ResizeWithForDisplayFileName(fullName, 1200, 82);
                }

                if (originalWidth > 800)
                {
                    progress.Report("Resize For Display: 800, 82");

                    return ResizeWithForDisplayFileName(fullName, 800, 82);
                }

                if (originalWidth > 400)
                {
                    //If we reach here we have a pretty small original image, add the original
                    //to the for display list and write the 400 version with higher quality
                    progress.Report("Resize For Display: 800, 82");

                    return ResizeWithWidthAndHeightFileName(fullName, 400, 82);
                }

                string newFile;

                using (var image = Image.Load(fullName.FullName))
                {
                    newFile = Path.Combine(fullName.Directory?.FullName ?? string.Empty,
                        $"{Path.GetFileNameWithoutExtension(fullName.Name)}--For-Display.jpg");

                    using var outImage = File.Create(newFile);
                    image.SaveAsJpeg(outImage, new JpegEncoder {Quality = 82});
                }

                return new FileInfo(newFile);
            }

            public static List<FileInfo> ResizeForDisplayAndSrcset(FileInfo originalImage, IProgress<string> progress)
            {
                var fullList = new List<FileInfo> {originalImage, ResizeForDisplay(originalImage, progress)};

                fullList.AddRange(ResizeForSrcset(originalImage, progress));

                return fullList;
            }

            public static List<FileInfo> ResizeForSrcset(FileInfo fullName, IProgress<string> progress)
            {
                int originalWidth;

                using (var originalImage = File.OpenRead(fullName.FullName))
                {
                    var imageInfo = Image.Identify(originalImage);
                    originalWidth = imageInfo.Width;
                }

                var returnList = new List<FileInfo>();

                if (originalWidth > 4000)
                {
                    progress.Report("Resize: 4000, 80");
                    returnList.Add(ResizeWithWidthAndHeightFileName(fullName, 4000, 80));
                }

                if (originalWidth > 3000)
                {
                    progress.Report("Resize: 3000, 80");
                    returnList.Add(ResizeWithWidthAndHeightFileName(fullName, 3000, 80));
                }

                if (originalWidth > 2000)
                {
                    progress.Report("Resize: 2000, 80");

                    returnList.Add(ResizeWithWidthAndHeightFileName(fullName, 2000, 80));
                }

                if (originalWidth > 1600)
                {
                    progress.Report("Resize: 1600, 80");

                    returnList.Add(ResizeWithWidthAndHeightFileName(fullName, 1600, 80));
                }

                if (originalWidth > 1200)
                {
                    progress.Report("Resize: 1200, 88");

                    returnList.Add(ResizeWithWidthAndHeightFileName(fullName, 1200, 80));
                }

                if (originalWidth > 800)
                {
                    progress.Report("Resize: 800, 72");

                    returnList.Add(ResizeWithWidthAndHeightFileName(fullName, 800, 72));
                }

                if (originalWidth > 400)
                {
                    progress.Report("Resize: 400, 72");

                    returnList.Add(ResizeWithWidthAndHeightFileName(fullName, 400, 72));
                }

                return returnList;
            }

            public static FileInfo ResizeWithForDisplayFileName(FileInfo toResize, int width, int quality)
            {
                if (toResize == null || !toResize.Exists) return null;

                string newFile;

                using (var image = Image.Load(toResize.FullName))
                {
                    var naturalWidth = image.Width;

                    var newHeight = (int) (image.Height * ((decimal) width / naturalWidth));
                    newFile = Path.Combine(toResize.Directory?.FullName ?? string.Empty,
                        $"{Path.GetFileNameWithoutExtension(toResize.Name)}--For-Display.jpg");

                    image.Mutate(ctx => ctx.Resize(width, newHeight, KnownResamplers.Bicubic));

                    using var outImage = File.Create(newFile);
                    image.SaveAsJpeg(outImage, new JpegEncoder {Quality = quality});
                }

                return new FileInfo(newFile);
            }

            public static FileInfo ResizeWithWidthAndHeightFileName(FileInfo toResize, int width, int quality)
            {
                if (toResize == null || !toResize.Exists) return null;

                string newFile;

                using (var image = Image.Load(toResize.FullName))
                {
                    var naturalWidth = image.Width;

                    var newHeight = (int) (image.Height * ((decimal) width / naturalWidth));

                    image.Mutate(ctx => ctx.Resize(width, newHeight, KnownResamplers.Bicubic));

                    newFile = Path.Combine(toResize.Directory?.FullName ?? string.Empty,
                        $"{Path.GetFileNameWithoutExtension(toResize.Name)}--{image.Width}w--{image.Height}h.jpg");

                    using var outImage = File.Create(newFile);
                    image.SaveAsJpeg(outImage, new JpegEncoder {Quality = quality});
                }

                return new FileInfo(newFile);
            }

            public static async Task UploadFileAsync(FileInfo toUpload, List<string> bucketSubdirectoryList,
                IProgress<string> progress)
            {
                if (bucketSubdirectoryList == null) bucketSubdirectoryList = new List<string>();

                var settings = UserSettingsUtilities.ReadSettings();

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
}