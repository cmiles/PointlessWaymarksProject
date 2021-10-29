using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.FilesWrittenLogList;
using PointlessWaymarks.CmsWpfControls.Utility.Aws;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.S3Uploads
{
    public static class S3UploadHelpers
    {
        public static async Task<(IsValid validUploadList, List<S3Upload> uploadItems)>
            FilesSinceLastUploadToRunningUploadWindow(IProgress<string> progress)
        {
            progress.Report("Checking Bucket Details in Settings");

            var userBucketName = UserSettingsSingleton.CurrentSettings().SiteS3Bucket;
            var userBucketRegion = UserSettingsSingleton.CurrentSettings().SiteS3BucketRegion;

            if (string.IsNullOrEmpty(userBucketName))
                return (new IsValid(false, "Bucket Name is Empty"), new List<S3Upload>());

            if (string.IsNullOrEmpty(userBucketRegion))
                return (new IsValid(false, "Bucket Region is Empty"), new List<S3Upload>());

            progress.Report("Setting Up Db");

            var db = await Db.Context();

            var sinceDate = db.GenerationFileTransferScriptLogs.OrderByDescending(x => x.WrittenOnVersion)
                .FirstOrDefault()?.WrittenOnVersion;

            var generationDirectory = new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName)
                .FullName;

            progress.Report($"Filtering for Generation Directory: {generationDirectory}");

            var searchQuery = db.GenerationFileWriteLogs.Where(
                x => x.FileName != null && x.FileName.StartsWith(generationDirectory));

            if (sinceDate != null)
            {
                progress.Report($"Filtering by Date and Time {sinceDate:F}");
                searchQuery = searchQuery.Where(x => x.WrittenOnVersion >= sinceDate);
            }

            var dbItems = await searchQuery.OrderByDescending(x => x.WrittenOnVersion).ToListAsync();

            if (!dbItems.Any()) return (new IsValid(false, "No Files Found to Upload?"), new List<S3Upload>());

            var transformedItems = new List<FilesWrittenLogListListItem>();

            progress.Report($"Processing {dbItems.Count} items for display");
            foreach (var loopDbItems in dbItems.Where(x => !string.IsNullOrWhiteSpace(x.FileName)).ToList())
            {
                var directory = new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName);
                var fileBase = loopDbItems.FileName!.Replace(directory.FullName, string.Empty);
                var isInGenerationDirectory = loopDbItems.FileName.StartsWith(generationDirectory);
                var transformedFileName = $"{userBucketName}{fileBase}".Replace("\\", "/");

                transformedItems.Add(new FilesWrittenLogListListItem
                {
                    FileBase = fileBase,
                    TransformedFile = transformedFileName,
                    WrittenOn = loopDbItems.WrittenOnVersion.ToLocalTime(),
                    WrittenFile = loopDbItems.FileName,
                    IsInGenerationDirectory = isInGenerationDirectory
                });
            }

            var s3Items = transformedItems.Where(x => x.IsInGenerationDirectory && File.Exists(x.WrittenFile)).Select(
                x =>
                    new S3Upload(new FileInfo(x.WrittenFile),
                        AwsS3GeneratedSiteComparisonForAdditionsAndChanges.FileInfoInGeneratedSiteToS3Key(
                            new FileInfo(x.WrittenFile)), userBucketName, userBucketRegion,
                        $"From Files Written Log - {x.WrittenOn}")).ToList();


            return (new IsValid(true, string.Empty), s3Items);
        }

        public static async Task GenerateChangedHtmlAndStartUpload(StatusControlContext statusContext)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            await HtmlGenerationGroups.GenerateChangedToHtml(statusContext.ProgressTracker());
            var toUpload = await FilesSinceLastUploadToRunningUploadWindow(statusContext.ProgressTracker());

            await S3UploaderItemsToS3UploaderJsonFile(toUpload.uploadItems, Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalScriptsDirectory().FullName,
                $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---File-Upload-Data.json"));

            if (!toUpload.validUploadList.Valid)
            {
                await statusContext.ShowMessageWithOkButton("Upload Failure",
                    $"Generating HTML appears to have succeeded but creating an upload failed: {toUpload.validUploadList.Explanation}");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();
            new S3UploadsWindow(toUpload.uploadItems, true).Show();
        }

        /// <summary>
        ///     Saves S3Uploader Items to the specified json file and writes an entry into the GenerationFileTransferScriptLogs
        /// </summary>
        /// <param name="items"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task S3UploaderItemsToS3UploaderJsonFile(List<S3Upload> items, string fileName)
        {
            var jsonInfo = JsonSerializer.Serialize(items.Select(x =>
                new S3UploadFileRecord(x.ToUpload.FullName, x.S3Key, x.BucketName, x.Region, x.Note)));

            var file = new FileInfo(fileName);

            await File.WriteAllTextAsync(file.FullName, jsonInfo);

            await Db.SaveGenerationFileTransferScriptLog(new GenerationFileTransferScriptLog
            {
                FileName = file.FullName,
                WrittenOnVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime()
            });
        }
    }
}