using System.Collections.Concurrent;
using System.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.S3;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CmsWpfControls.Utility.S3;

public class S3GeneratedSiteComparisonWithHashesForAdditionsAndChanges
{
    public List<string> ErrorMessages { get; } = [];
    public List<S3UploadRequest> FileHashMismatches { get; } = [];
    public List<S3UploadRequest> MissingFiles { get; } = [];

    public static async Task<S3GeneratedSiteComparisonWithHashesForAdditionsAndChanges> RunReport(
        IS3AccountInformation s3Account,
        IProgress<string>? progress)
    {
        var returnReport = new S3GeneratedSiteComparisonWithHashesForAdditionsAndChanges();

        if (string.IsNullOrWhiteSpace(s3Account.BucketName()))
        {
            returnReport.ErrorMessages.Add("S3 Bucket Name is empty?");
            return returnReport;
        }

        var bucket = s3Account.BucketName();
        var serviceUrl = s3Account.ServiceUrl();

        if (string.IsNullOrWhiteSpace(bucket))
        {
            returnReport.ErrorMessages.Add("S3 Bucket is empty?");
            return returnReport;
        }

        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            returnReport.ErrorMessages.Add("S3 Service URL is empty?");
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

        var fileMetadataContainer = new ConcurrentBag<S3LocalFileAndMetadata>();

        progress?.Report(
            $"File Metadata - {allGeneratedFiles.Count} Files to Process - {DateTime.Now:t}");

        var counter = 0;

        await Parallel.ForEachAsync(allGeneratedFiles, async (info, _) =>
        {
            var frozenCounter = Interlocked.Increment(ref counter);

            if (frozenCounter % 5000 == 0 || frozenCounter == 1)
                progress?.Report(
                    $"File Metadata - {frozenCounter} of {allGeneratedFiles.Count} Files Complete - {DateTime.Now:t}");

            fileMetadataContainer.Add(await S3Tools.LocalFileAndMetadata(info));
        });

        var fileMetadataList = fileMetadataContainer.ToList();

        progress?.Report($"Found {fileMetadataList.Count} Files in Generated Site");

        var awsObjects = await S3Tools.ListS3Items(s3Account, "", progress ?? new ConsoleProgress(), CancellationToken.None);

        progress?.Report($"Found {awsObjects.Count} S3 Objects - starting file comparison");

        var totalGeneratedFiles = fileMetadataList.Count;
        var fileLoopCount = 0;

        foreach (var loopFile in fileMetadataList)
        {
            if (++fileLoopCount % 100 == 0)
                progress?.Report(
                    $"File Loop vs S3 Objects Comparison - {fileLoopCount} of {totalGeneratedFiles} - {loopFile.LocalFile.FullName}");

            if (!File.Exists(loopFile.LocalFile.FullName))
            {
                progress?.Report($"File {loopFile.LocalFile.FullName} No Longer Exists - skipping...");
                continue;
            }

            var loopFileKey = S3CmsTools.FileInfoInGeneratedSiteToS3Key(loopFile.LocalFile);

            var matches = awsObjects
                .Where(x => !x.Key.EndsWith("/") && x.Metadata.FileSize != 0 && x.Key == loopFileKey)
                .ToList();

            switch (matches.Count)
            {
                case > 1:
                    returnReport.ErrorMessages.Add(
                        $"Unexpected Condition - {matches.Count} matches found for {loopFile.LocalFile.FullName} - {string.Join(", ", matches.Select(x => x.Key))}");
                    break;
                case 1:
                {
                    if (loopFile.UploadMetadata.FileSystemHash != matches.First().Metadata.FileSystemHash)
                        returnReport.FileHashMismatches.Add(new S3UploadRequest(loopFile,
                            matches.First().Key,
                            bucket,
                            s3Account.ServiceUrl(), "S3 Hash Mismatch"));
                    matches.ForEach(x => awsObjects.Remove(x));
                    break;
                }
                default:
                    returnReport.MissingFiles.Add(await S3Tools.UploadRequest(loopFile.LocalFile,
                        S3CmsTools.FileInfoInGeneratedSiteToS3Key(loopFile.LocalFile),
                        bucket, s3Account.ServiceUrl(), "File Missing on S3"));
                    break;
            }
        }

        progress?.Report(
            $"Returning Report - {returnReport.MissingFiles.Count} Missing Files, {returnReport.ErrorMessages.Count} Error " +
            $"Messages, {returnReport.FileHashMismatches.Count} File Hash Mismatches.");

        return returnReport;
    }
}