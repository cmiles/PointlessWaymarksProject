using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Transfer;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.S3;
using PointlessWaymarks.CommonTools;
using Polly;
using Serilog;

namespace PointlessWaymarks.Task.PublishSiteToAmazonS3;

public class PublishSiteToAmazonS3
{
    public async System.Threading.Tasks.Task Publish(string settingsFile)
    {
        var notifier = (await WindowsNotificationBuilders.NewNotifier(PublishSiteToAmazonS3Settings.ProgramShortName))
            .SetAutomationLogoNotificationIconUrl().SetErrorReportAdditionalInformationMarkdown(
                FileAndFolderTools.ReadAllText(
                    Path.Combine(AppContext.BaseDirectory, "README.md")));

        if (string.IsNullOrWhiteSpace(settingsFile))
        {
            Log.Error("Settings File Not Specified");
            await notifier.Error("Settings File Not Specified");
            return;
        }

        settingsFile = settingsFile.Trim();

        var settingsFileInfo = new FileInfo(settingsFile);

        if (!settingsFileInfo.Exists)
        {
            Log.ForContext("settingsFile", settingsFile).Error("Settings File Does Not Exist?");
            await notifier.Error($"Settings File {settingsFile} Does Not Exist?");
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
                await notifier.Error("Invalid Settings File",
                    $"Settings file {settingsFile} deserialized into a null object");
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
            await notifier.Error("Site Settings File Not Found",
                "The site settings file {settings.PointlessWaymarksSiteSettingsFileFullName} was specified but not found?");
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
            await notifier.Error("Upload Failure",
                $"Generating HTML appears to have succeeded but creating an upload failed: {toUpload.validUploadList.Explanation}");
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

        var progressList = new List<(bool sucess, S3UploadRequest uploadRequest)>();
        var exceptionList = new List<Exception>();

        var s3RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (exception, _, retryCount, _) =>
            {
                Log.ForContext("exception", exception.ToString()).Verbose(exception,
                    "S3 Upload Retry {retryCount} - {exceptionMessage}", retryCount,
                    exception.Message);
            });

        int progressCount = 0;

        foreach (var loopUpload in toUpload.uploadItems)
        {
            if(progressCount++ %10 == 0) consoleProgress.Report($"   S3 Upload Progress - {progressCount} of {toUpload.uploadItems.Count}");

            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = bucket,
                FilePath = loopUpload.ToUpload.FullName,
                Key = loopUpload.S3Key
            };

            try
            {
                await s3RetryPolicy.ExecuteAsync(async () => await fileTransferUtility.UploadAsync(uploadRequest));
                Log.Verbose($"S3 Upload Completed - {loopUpload.ToUpload.FullName} to {loopUpload.S3Key}");
                progressList.Add((true, loopUpload));
            }
            catch (Exception e)
            {
                exceptionList.Add(e);
                progressList.Add((false, loopUpload));
                Log.ForContext("loopUpload", loopUpload.SafeObjectDump()).Error(e,
                    $"Amazon S3 Upload Failed - {loopUpload.ToUpload.FullName} to {loopUpload.S3Key}");
            }
        }

        if (progressList.All(x => x.sucess))
        {
            notifier.Message($"{UserSettingsSingleton.CurrentSettings().SiteName} Published! {progressList.Count} Files Uploaded to S3.");
        }
        else
        {
            var failures = progressList.Where(x => !x.sucess).Select(x => x.uploadRequest).ToList();
            var successList = progressList.Where(x => x.sucess).Select(x => x.uploadRequest).ToList();


            var failureFile = Path.Combine(UserSettingsSingleton.CurrentSettings().LocalScriptsDirectory().FullName,
                $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---Upload-Failures.json");

            await S3Tools.S3UploaderItemsToS3UploaderJsonFile(failures, failureFile);

            var failureBody = new StringBuilder();
            failureBody.AppendLine(
                $"<p>{failures.Count} Upload Failed - {successList.Count} Uploads Succeeded. This can leave a site in an unpredictable state with a mix of old and new files/content...</p>");
            failureBody.AppendLine(
                "<p>The failures are listed below and have also been saved as a file that you can open in the Pointless Waymarks CMS -- File Log Tab, Written Files Tab, S3 Menu -> Open Uploader Json File -- and try Uploading these files again.</p>");
            failureBody.AppendLine($"<p>{failureFile}</p>");

            failureBody.AppendLine("<p>Failed Uploads:<p>");
            failureBody.AppendLine("<ul>");

            failures.ForEach(x => failureBody.AppendLine($"<li>{WebUtility.HtmlEncode(x.ToUpload.FullName)}</li>"));
            failureBody.AppendLine("</ul>");

            failureBody.AppendLine("<br><p>Successful Uploads:<p>");
            failureBody.AppendLine("<ul>");
            successList.ForEach(x => failureBody.AppendLine($"<li>{WebUtility.HtmlEncode(x.ToUpload.FullName)}</li>"));
            failureBody.AppendLine("</ul>");


            failureBody.AppendLine(
                exceptionList.Count > 10 ? "<br><p>First 10 Exceptions:<p>" : "<br><p>Exceptions:<p>");
            failureBody.AppendLine("<br><p>Exceptions<p>");
            failureBody.AppendLine("<ul>");
            exceptionList.Take(10).ToList()
                .ForEach(x => failureBody.AppendLine($"<li>{WebUtility.HtmlEncode(x.Message)}</li>"));
            failureBody.AppendLine("</ul>");

            await notifier.Error($"{siteSettings.SiteName} Upload Failure", failureBody.ToString());
        }
    }
}