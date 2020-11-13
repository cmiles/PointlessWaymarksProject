#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.S3Uploads;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public class AwsS3GeneratedSiteComparison
    {
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public List<S3Upload> FileSizeMismatches { get; set; } = new List<S3Upload>();
        public List<S3Upload> MissingFiles { get; set; } = new List<S3Upload>();
        public List<S3Object> S3ObjectsNotInGeneratedSite { get; set; } = new List<S3Object>();


        public static string FileInfoInGeneratedSiteToS3Key(FileInfo file)
        {
            return file.FullName.Replace($"{UserSettingsSingleton.CurrentSettings().LocalSiteRootDirectory}\\", "")
                .Replace("\\", "/");
        }

        public static async Task<AwsS3GeneratedSiteComparison> FilesInGeneratedDirectoryButNotInS3(
            IProgress<string>? progress)
        {
            var returnReport = new AwsS3GeneratedSiteComparison();

            if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().SiteS3Bucket))
            {
                returnReport.ErrorMessages.Add("Amazon S3 Bucket Name not filled?");
                return returnReport;
            }

            var bucket = UserSettingsSingleton.CurrentSettings().SiteS3Bucket;

            progress?.Report("Getting list of all generated files");

            var allGeneratedFiles = new DirectoryInfo(UserSettingsSingleton.CurrentSettings().LocalSiteRootDirectory)
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

            var s3Client = new AmazonS3Client(accessKey, secret);

            var listRequest = new ListObjectsV2Request {BucketName = bucket};

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

                var loopFileKey = FileInfoInGeneratedSiteToS3Key(loopFile);

                var matches = awsObjects.Where(x => !x.Key.EndsWith("/") && x.Size != 0 && x.Key == loopFileKey)
                    .ToList();

                if (matches.Count > 1)
                {
                    returnReport.ErrorMessages.Add(
                        $"Unexpected Condition - {matches.Count} matches found for {loopFile.FullName} - {string.Join(", ", matches.Select(x => x.Key))}");
                }
                else if (matches.Count == 1)
                {
                    if (loopFile.Length != matches.First().Size)
                        returnReport.FileSizeMismatches.Add(new S3Upload(loopFile, matches.First().Key, bucket,
                            "S3 File Size Mismatch"));
                    matches.ForEach(x => awsObjects.Remove(x));
                }
                else
                {
                    returnReport.MissingFiles.Add(new S3Upload(loopFile, FileInfoInGeneratedSiteToS3Key(loopFile),
                        bucket, "File Missing on S3"));
                }
            }

            returnReport.S3ObjectsNotInGeneratedSite = awsObjects.ToList();

            progress?.Report(
                $"Returning Report - {returnReport.MissingFiles.Count} Missing Files, {returnReport.ErrorMessages.Count} Error " +
                $"Messages, {returnReport.FileSizeMismatches.Count} File Size Mismatches, {returnReport.S3ObjectsNotInGeneratedSite.Count} " +
                "objects not in generated site.");

            return returnReport;
        }
    }
}