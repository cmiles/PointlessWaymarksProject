using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CmsData.S3;

public static class S3Tools
{
    /// <summary>
    ///     Returns an S3 key for a file inside/from the generated site
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static string FileInfoInGeneratedSiteToS3Key(FileInfo file)
    {
        return file.FullName
            .Replace($"{UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName}\\", "")
            .Replace("\\", "/");
    }

    public static async Task<(IsValid validUploadList, List<S3UploadRequest> uploadItems)>
        FilesSinceLastUploadToUploadList(
            IProgress<string> progress)
    {
        progress.Report("Checking Bucket Details in Settings");

        var userBucketName = UserSettingsSingleton.CurrentSettings().SiteS3Bucket;
        var userBucketRegion = UserSettingsSingleton.CurrentSettings().SiteS3BucketRegion;

        if (string.IsNullOrEmpty(userBucketName))
            return (new IsValid(false, "Bucket Name is Empty"), new List<S3UploadRequest>());

        if (string.IsNullOrEmpty(userBucketRegion))
            return (new IsValid(false, "Bucket Region is Empty"), new List<S3UploadRequest>());

        progress.Report("Setting Up Db");

        var db = await Db.Context();

        var sinceDate = db.GenerationFileTransferScriptLogs.OrderByDescending(x => x.WrittenOnVersion)
            .FirstOrDefault()?.WrittenOnVersion;

        var generationDirectory =
            new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName)
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

        if (!dbItems.Any()) return (new IsValid(false, "No Files Found to Upload?"), new List<S3UploadRequest>());

        var transformedItems = new List<FilesWrittenLog>();

        progress.Report($"Processing {dbItems.Count} items for display");
        foreach (var loopDbItems in dbItems.Where(x => !string.IsNullOrWhiteSpace(x.FileName)).ToList())
        {
            var directory =
                new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName);
            var fileBase = loopDbItems.FileName!.Replace(directory.FullName, string.Empty);
            var isInGenerationDirectory = loopDbItems.FileName.StartsWith(generationDirectory);
            var transformedFileName = $"{userBucketName}{fileBase}".Replace("\\", "/");

            transformedItems.Add(new FilesWrittenLog
            {
                FileBase = fileBase,
                TransformedFile = transformedFileName,
                WrittenOn = loopDbItems.WrittenOnVersion.ToLocalTime(),
                WrittenFile = loopDbItems.FileName,
                IsInGenerationDirectory = isInGenerationDirectory
            });
        }

        var s3Items = transformedItems.Where(x => x.IsInGenerationDirectory && File.Exists(x.WrittenFile)).Select(
            x => new S3UploadRequest(new FileInfo(x.WrittenFile),
                FileInfoInGeneratedSiteToS3Key(
                    new FileInfo(x.WrittenFile)), userBucketName, userBucketRegion,
                $"From Files Written Log - {x.WrittenOn}")).ToList();


        return (new IsValid(true, string.Empty), s3Items);
    }

    /// <summary>
    ///     Saves S3Uploader Items to the specified json file and writes an entry into the GenerationFileTransferScriptLogs
    /// </summary>
    /// <param name="items"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static async Task S3UploaderItemsToS3UploaderJsonFile(List<S3UploadRequest> items, string fileName)
    {
        var jsonInfo = JsonSerializer.Serialize(items.Select(x =>
            new S3UploadFileEntry(x.ToUpload.FullName, x.S3Key, x.BucketName, x.Region, x.Note)));

        var file = new FileInfo(fileName);

        await File.WriteAllTextAsync(file.FullName, jsonInfo);

        await Db.SaveGenerationFileTransferScriptLog(new GenerationFileTransferScriptLog
        {
            FileName = file.FullName,
            WrittenOnVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime()
        });
    }

    private class FilesWrittenLog
    {
        public string FileBase { get; set; } = string.Empty;
        public bool IsInGenerationDirectory { get; set; }
        public string TransformedFile { get; set; } = string.Empty;
        public string WrittenFile { get; set; } = string.Empty;
        public DateTime WrittenOn { get; set; }
    }
}