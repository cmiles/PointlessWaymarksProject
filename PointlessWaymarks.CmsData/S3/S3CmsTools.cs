using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CmsData.S3;

public static class S3CmsTools
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
        
        var isCloudflare = UserSettingsSingleton.CurrentSettings().SiteS3CloudProvider ==
                           S3Providers.Cloudflare.ToString();

        var userBucketName = UserSettingsSingleton.CurrentSettings().SiteS3Bucket;
        var userBucketRegion = UserSettingsSingleton.CurrentSettings().SiteS3BucketRegion;

        if (string.IsNullOrEmpty(userBucketName))
            return (new IsValid(false, "Bucket Name is Empty"), []);

        if (!isCloudflare && string.IsNullOrEmpty(userBucketRegion))
            return (new IsValid(false, "Bucket Region is Empty"), []);

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
        
        var rawDbItems = await searchQuery.ToListAsync();

        var dbItems = rawDbItems.GroupBy(x => x.FileName).Select(x => x.MaxBy(y => y.WrittenOnVersion)).Where(x => x is not null).OrderByDescending(x => x!.WrittenOnVersion).Select(x => x!).ToList();

        if (!dbItems.Any()) return (new IsValid(false, "No Files Found to Upload?"), []);

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

        var s3Items = await transformedItems.Where(x => x.IsInGenerationDirectory && File.Exists(x.WrittenFile))
            .ToAsyncEnumerable().SelectAwait(
                async x => await S3Tools.UploadRequest(new FileInfo(x.WrittenFile),
                    FileInfoInGeneratedSiteToS3Key(
                        new FileInfo(x.WrittenFile)), userBucketName, userBucketRegion,
                    $"From Files Written Log - {x.WrittenOn}")).ToListAsync();


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
        var deDuplicatedItems =
            items.GroupBy(x => new { x.ToUpload.LocalFile.FullName, x.BucketName, x.Region, x.S3Key })
                .Select(x => x.First()).ToList();

        var jsonInfo = JsonSerializer.Serialize(deDuplicatedItems.Select(x =>
            new S3UploadFileEntry(x.ToUpload.LocalFile.FullName, x.S3Key, x.BucketName, x.Region, x.Note)));

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
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        public string FileBase { get; set; } = string.Empty;
        public bool IsInGenerationDirectory { get; set; }
        public string TransformedFile { get; set; } = string.Empty;
        public string WrittenFile { get; set; } = string.Empty;

        public DateTime WrittenOn { get; set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Local
    }
    
    public static IS3AccountInformation AmazonInformationFromSettings()
    {
        var useCloudflare = UserSettingsSingleton.CurrentSettings().SiteS3CloudProvider ==
                            S3Providers.Cloudflare.ToString();
        
        return new S3AccountInformation
        {
            CloudflareAccountId = () => useCloudflare ? CloudCredentials.GetCloudflareAccountId().secret : string.Empty,
            AccessKey = () =>
                useCloudflare
                    ? CloudCredentials.GetCloudflareSiteCredentials().accessKey
                    : CloudCredentials.GetAwsSiteCredentials().accessKey,
            Secret = () =>
                useCloudflare
                    ? CloudCredentials.GetCloudflareSiteCredentials().secret
                    : CloudCredentials.GetAwsSiteCredentials().secret,
            BucketName = () => UserSettingsSingleton.CurrentSettings().SiteS3Bucket,
            BucketRegion = () => UserSettingsSingleton.CurrentSettings().SiteS3BucketRegion,
            FullFileNameForJsonUploadInformation = () =>
                Path.Combine(UserSettingsSingleton.CurrentSettings().LocalScriptsDirectory().FullName,
                    $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---File-Upload-Data.json"),
            FullFileNameForToExcel = () => Path.Combine(FileLocationTools.TempStorageDirectory().FullName,
                $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---{FileAndFolderTools.TryMakeFilenameValid("S3UploadItems")}.xlsx"),
            S3Provider = () => useCloudflare ? S3Providers.Cloudflare : S3Providers.Amazon
        };
    }
}