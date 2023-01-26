using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Toolkit.Uwp.Notifications;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.S3;
using PointlessWaymarks.CommonTools;
using Serilog;

namespace PointlessWaymarks.Task.PublishSiteToAmazonS3;

public class PublishSiteToAmazonS3
{
    public async System.Threading.Tasks.Task Publish(string settingsFile)
    {
        if (string.IsNullOrWhiteSpace(settingsFile))
        {
            Log.Error("Settings File is Null or Whitespace?");
            return;
        }

        settingsFile = settingsFile.Trim();

        var settingsFileInfo = new FileInfo(settingsFile);

        if (!settingsFileInfo.Exists)
        {
            Log.Error($"Settings File {settingsFile} Does Not Exist?");
            return;
        }

        PublishSiteToAmazonS3Settings? settings;
        try
        {
            var settingsFileJsonString = await File.ReadAllTextAsync(settingsFileInfo.FullName);
            var tryReadSettings =
                JsonSerializer.Deserialize<PublishSiteToAmazonS3Settings>(settingsFileJsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tryReadSettings == null)
            {
                Log.Error("Settings file {settingsFile} deserialized into a null object - is the format correct?",
                    settingsFile);
                return;
            }

            settings = tryReadSettings;

            Log.ForContext("settings",
                settings.Dump()).Information($"Using settings from {settingsFileInfo.FullName}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception reading settings file {settingsFile}", settingsFile);
            return;
        }

        var siteSettingsFileInfo = new FileInfo(settings.PointlessWaymarksSiteSettingsFileFullName);

        if (!siteSettingsFileInfo.Exists)
        {
            Log.Error(
                $"The site settings file {settings.PointlessWaymarksSiteSettingsFileFullName} was specified but not found?");
            return;
        }

        var consoleProgress = new ConsoleProgress();

        UserSettingsUtilities.SettingsFileFullName = siteSettingsFileInfo.FullName;
        var siteSettings = await UserSettingsUtilities.ReadFromCurrentSettingsFile(consoleProgress);
        siteSettings.VerifyOrCreateAllTopLevelFolders();

        await UserSettingsUtilities.EnsureDbIsPresent(consoleProgress);

        await HtmlGenerationGroups.GenerateChangedToHtml(consoleProgress);
        var toUpload = await S3Tools.FilesSinceLastUploadToUploadList(consoleProgress);

        if (!toUpload.validUploadList.Valid)
        {
            Log.Error(
                $"Upload Failure - Generating HTML appears to have succeeded but creating an upload failed: {toUpload.validUploadList.Explanation}");
            return;
        }

        await S3Tools.S3UploaderItemsToS3UploaderJsonFile(toUpload.uploadItems,
            Path.Combine(UserSettingsSingleton.CurrentSettings().LocalScriptsDirectory().FullName,
                $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---File-Upload-Data.json"));

        var bucket = UserSettingsSingleton.CurrentSettings().SiteS3Bucket;
        var region = UserSettingsSingleton.CurrentSettings().SiteS3BucketEndpoint();
        var (accessKey, secret) = AwsCredentials.GetAwsSiteCredentials();

        var s3Client = new AmazonS3Client(accessKey, secret, region);

        var fileTransferUtility = new TransferUtility(s3Client);

        var progressList = new List<(bool sucess, string filename)>();
        foreach (var loopUpload in toUpload.uploadItems)
        {
            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = bucket,
                FilePath = loopUpload.ToUpload.FullName,
                Key = loopUpload.S3Key
            };

            try
            {
                await fileTransferUtility.UploadAsync(uploadRequest);
                Log.Verbose($"S3 Upload Completed - {loopUpload.ToUpload.FullName} to {loopUpload.S3Key}");
                progressList.Add((true, loopUpload.ToUpload.FullName));
            }
            catch (Exception e)
            {
                progressList.Add((false, loopUpload.ToUpload.FullName));
                Log.ForContext("loopUpload", loopUpload.SafeObjectDump()).Error(e,
                    $"Amazon S3 Upload Failed - {loopUpload.ToUpload.FullName} to {loopUpload.S3Key}");
            }
        }

        if (progressList.All(x => x.sucess))
            new ToastContentBuilder()
                .AddHeader(AppDomain.CurrentDomain.FriendlyName,
                    $"{UserSettingsSingleton.CurrentSettings().SiteName} Uploaded!", new ToastArguments())
                .AddText($"{progressList.Count} Files Uploaded to Amazon S3")
                .Show();
        else
            new ToastContentBuilder()
                .AddHeader(AppDomain.CurrentDomain.FriendlyName,
                    $"{UserSettingsSingleton.CurrentSettings().SiteName} Upload Failure", new ToastArguments())
                .AddText($"{progressList.Count(x => !x.sucess)} Files Failed to Upload")
                .Show();
    }
}