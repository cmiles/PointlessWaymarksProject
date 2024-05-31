using System.IO;
using Amazon.S3.Model;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.S3;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CmsWpfControls.Utility.S3;

public class S3GeneratedSiteComparisonForAdditionsAndChanges
{
    public List<string> ErrorMessages { get; } = [];
    public List<S3UploadRequest> FileSizeMismatches { get; } = [];
    public List<S3UploadRequest> MissingFiles { get; } = [];
    
    
    public static async Task<S3GeneratedSiteComparisonForAdditionsAndChanges> RunReport(
        IS3AccountInformation s3Account,
        IProgress<string>? progress)
    {
        var returnReport = new S3GeneratedSiteComparisonForAdditionsAndChanges();
        
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
        
        progress?.Report($"Found {allGeneratedFiles.Count} Files in Generated Site");
        
        progress?.Report("Setting up for S3 Object Listings");
        
        var s3Client = s3Account.S3Client();
        
        var listRequest = new ListObjectsV2Request { BucketName = bucket };
        
        var awsObjects = new List<S3Object>();

        //5/31/2024 - The code commented out below has been tested with Amazon S3 and Cloudflare R2 but fails
        //with Wasabi. Hoping the 'straight' ListObjectsV2Async version below will help with compatibility
        //more S3 providers.
        //var paginator = s3Client.Paginators.ListObjectsV2(listRequest);
        //await foreach (var response in paginator.S3Objects)
        //{
        //    if (awsObjects.Count % 1000 == 0)
        //        progress?.Report($"S3 Object Listing - Added {awsObjects.Count} S3 Objects so far...");
        //    awsObjects.Add(response);
        //}

        ListObjectsV2Response response;
        do
        {
            response = await s3Client.ListObjectsV2Async(listRequest);
            
            foreach (var entry in response.S3Objects)
            {
                if (awsObjects.Count % 1000 == 0)
                    progress?.Report($"S3 Object Listing - Added {awsObjects.Count} S3 Objects so far...");
                
                awsObjects.Add(entry);
            }
            
            listRequest.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated);

        progress?.Report($"Found {awsObjects.Count} S3 Objects - starting file comparison");
        
        var totalGeneratedFiles = allGeneratedFiles.Count;
        var fileLoopCount = 0;
        
        foreach (var loopFile in allGeneratedFiles)
        {
            if (++fileLoopCount % 100 == 0)
                progress?.Report(
                    $"File Loop vs S3 Objects Comparison - {fileLoopCount} or {totalGeneratedFiles} - {loopFile.FullName}");
            
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
                            s3Account.ServiceUrl(), "S3 File Size Mismatch"));
                    matches.ForEach(x => awsObjects.Remove(x));
                    break;
                }
                default:
                    returnReport.MissingFiles.Add(await S3Tools.UploadRequest(loopFile,
                        S3CmsTools.FileInfoInGeneratedSiteToS3Key(loopFile),
                        bucket, s3Account.ServiceUrl(), "File Missing on S3"));
                    break;
            }
        }
        
        progress?.Report(
            $"Returning Report - {returnReport.MissingFiles.Count} Missing Files, {returnReport.ErrorMessages.Count} Error " +
            $"Messages, {returnReport.FileSizeMismatches.Count} File Size Mismatches.");
        
        return returnReport;
    }
}