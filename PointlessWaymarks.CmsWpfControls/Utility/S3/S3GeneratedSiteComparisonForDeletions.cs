using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.S3;

namespace PointlessWaymarks.CmsWpfControls.Utility.S3;

public class S3GeneratedSiteComparisonForDeletions
{
    public List<string> ErrorMessages { get; } = [];
    public List<string> S3KeysToDelete { get; } = [];

    public static string DirectoryInfoInGeneratedSiteToS3Key(DirectoryInfo directory)
    {
        return directory.FullName
            .Replace($"{UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName}\\", "")
            .Replace("\\", "/") + "/";
    }

    public static async Task<S3GeneratedSiteComparisonForDeletions> RunReport(IProgress<string> progress)
    {
        var returnReport = new S3GeneratedSiteComparisonForDeletions();

        if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().SiteS3Bucket))
        {
            returnReport.ErrorMessages.Add("Amazon S3 Bucket Name not filled?");
            return returnReport;
        }

        var bucket = UserSettingsSingleton.CurrentSettings().SiteS3Bucket;
        var region = UserSettingsSingleton.CurrentSettings().SiteS3BucketEndpoint();

        progress.Report("Getting list of all generated files");

        var allGeneratedFiles =
            new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName)
                .GetFiles("*", SearchOption.AllDirectories).OrderBy(x => x.FullName)
                .Select(S3CmsTools.FileInfoInGeneratedSiteToS3Key).ToList();

        var allGeneratedDirectories =
            new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName)
                .GetDirectories("*", SearchOption.AllDirectories).OrderBy(x => x.FullName)
                .Select(DirectoryInfoInGeneratedSiteToS3Key).ToList();

        if (!allGeneratedFiles.Any() && !allGeneratedDirectories.Any())
        {
            returnReport.ErrorMessages.Add(
                "No generated Files Found? This is unusual... If the Local Directory for the " +
                "generated site has recently changed or the site is new perhaps try generating all HTML?");
            return returnReport;
        }

        progress.Report($"Found {allGeneratedFiles.Count} Files in Generated Site");

        progress.Report("Getting Aws S3 Credentials");

        var (accessKey, secret) = CloudCredentials.GetAwsSiteCredentials();

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secret))
        {
            returnReport.ErrorMessages.Add("Aws Credentials are not entered or valid?");
            return returnReport;
        }

        progress.Report("Setting up for Aws S3 Object Listings");

        var s3Client = new AmazonS3Client(accessKey, secret, region);

        var listRequest = new ListObjectsV2Request { BucketName = bucket };

        var awsObjects = new List<S3Object>();

        var paginator = s3Client.Paginators.ListObjectsV2(listRequest);

        await foreach (var response in paginator.S3Objects)
        {
            if (awsObjects.Count % 1000 == 0) progress.Report($"Aws Object Listing - Added {awsObjects.Count} S3 Objects so far...");

            awsObjects.Add(response);
        }

        progress.Report($"Found {awsObjects.Count} S3 Objects - starting file comparison");

        var totalGeneratedObjects = allGeneratedFiles.Count;
        var objectLoopCount = 0;

        foreach (var loopObject in awsObjects)
        {
            if (++objectLoopCount % 100 == 0)
                progress.Report(
                    $"File Loop vs Aws S3 Objects Comparison - {objectLoopCount} or {totalGeneratedObjects} - {loopObject.Key}");

            if (loopObject.Key.EndsWith("/") && loopObject.Size == 0)
            {
                var directoryMatches = allGeneratedDirectories.Where(x => x == loopObject.Key).ToList();

                if (directoryMatches.Any()) continue;

                returnReport.S3KeysToDelete.Add(loopObject.Key);
            }
            else
            {
                var fileMatches = allGeneratedFiles.Where(x => x == loopObject.Key).ToList();

                if (fileMatches.Any()) continue;

                returnReport.S3KeysToDelete.Add(loopObject.Key);
            }
        }

        progress.Report(
            $"Returning Report - {returnReport.S3KeysToDelete.Count} Files/Directories found to deleted, {returnReport.ErrorMessages.Count} Error.");

        return returnReport;
    }
}