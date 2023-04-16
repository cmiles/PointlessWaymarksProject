#nullable enable
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.S3;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CmsWpfControls.Utility.Aws;

public class AwsS3GeneratedSiteComparisonForAdditionsAndChanges
{
    public List<string> ErrorMessages { get; } = new();
    public List<S3UploadRequest> FileSizeMismatches { get; } = new();
    public List<S3UploadRequest> MissingFiles { get; } = new();


    public static async Task<AwsS3GeneratedSiteComparisonForAdditionsAndChanges> RunReport(
        IProgress<string>? progress)
    {
        var returnReport = new AwsS3GeneratedSiteComparisonForAdditionsAndChanges();

        if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().SiteS3Bucket))
        {
            returnReport.ErrorMessages.Add("Amazon S3 Bucket Name not filled?");
            return returnReport;
        }

        var bucket = UserSettingsSingleton.CurrentSettings().SiteS3Bucket;
        var region = UserSettingsSingleton.CurrentSettings().SiteS3BucketEndpoint();

        if (region == null)
        {
            returnReport.ErrorMessages.Add("Amazon S3 Bucket Endpoint (region) not filled?");
            return returnReport;
        }

        progress?.Report("Getting list of all generated files");

        var allGeneratedFiles =
            new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName)
                .GetFiles("*", SearchOption.AllDirectories).OrderBy(x => x.FullName).ToList();

        if (!allGeneratedFiles.Any())
        {
            returnReport.ErrorMessages.Add(
                "No generated Files Found? This is unusual... If the Local Directory for the " +
                "generated site has recently changed or the site is new perhaps try generating all HTML?");
            return returnReport;
        }

        progress?.Report($"Found {allGeneratedFiles.Count} Files in Generated Site");

        progress?.Report("Getting Aws S3 Credentials");

        var (accessKey, secret) = AwsCredentials.GetAwsSiteCredentials();

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secret))
        {
            returnReport.ErrorMessages.Add("Aws Credentials are not entered or valid?");
            return returnReport;
        }

        progress?.Report("Setting up for Aws S3 Object Listings");

        var s3Client = new AmazonS3Client(accessKey, secret, region);

        var listRequest = new ListObjectsV2Request { BucketName = bucket };

        var awsObjects = new List<S3Object>();

        ListObjectsV2Response listResponse;

        var loopNumber = 0;

        do
        {
            progress?.Report($"Aws Object Listing Loop {++loopNumber}");

            listResponse = await s3Client.ListObjectsV2Async(listRequest);

            progress?.Report($"Adding {listResponse.S3Objects.Count} S3 Objects to List...");

            listResponse.S3Objects.ForEach(x => awsObjects.Add(x));

            // Set the marker property
            listRequest.ContinuationToken = listResponse.NextContinuationToken;
        } while (listResponse.IsTruncated);

        progress?.Report($"Found {awsObjects.Count} S3 Objects - starting file comparison");

        var totalGeneratedFiles = allGeneratedFiles.Count;
        var fileLoopCount = 0;

        foreach (var loopFile in allGeneratedFiles)
        {
            if (++fileLoopCount % 100 == 0)
                progress?.Report(
                    $"File Loop vs Aws S3 Objects Comparison - {fileLoopCount} or {totalGeneratedFiles} - {loopFile.FullName}");

            var loopFileKey = S3CmsTools.FileInfoInGeneratedSiteToS3Key(loopFile);

            var matches = awsObjects.Where(x => !x.Key.EndsWith("/") && x.Size != 0 && x.Key == loopFileKey)
                .ToList();

            switch (matches.Count)
            {
                case > 1:
                    returnReport.ErrorMessages.Add(
                        $"Unexpected Condition - {matches.Count} matches found for {loopFile.FullName} - {string.Join(", ", matches.Select(x => x.Key))}");
                    break;
                case 1:
                {
                    if (loopFile.Length != matches.First().Size)
                        returnReport.FileSizeMismatches.Add(await S3Tools.UploadRequest(loopFile, matches.First().Key,
                            bucket,
                            region.SystemName, "S3 File Size Mismatch"));
                    matches.ForEach(x => awsObjects.Remove(x));
                    break;
                }
                default:
                    returnReport.MissingFiles.Add(await S3Tools.UploadRequest(loopFile,
                        S3CmsTools.FileInfoInGeneratedSiteToS3Key(loopFile),
                        bucket, region.SystemName, "File Missing on S3"));
                    break;
            }
        }

        progress?.Report(
            $"Returning Report - {returnReport.MissingFiles.Count} Missing Files, {returnReport.ErrorMessages.Count} Error " +
            $"Messages, {returnReport.FileSizeMismatches.Count} File Size Mismatches.");

        return returnReport;
    }
}