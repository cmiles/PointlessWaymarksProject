using System.Net;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;
using Polly;
using Serilog;

namespace PointlessWaymarks.CloudBackupData.Batch;

public static class CloudTransfer
{
    public static async Task<CloudUploadAndDeleteRunInformation> CloudUploadAndDelete(
        IS3AccountInformation accountInformation, int cloudTransferBatchId, DateTime? startTime,
        IProgress<string>? progress)
    {
        var context = await CloudBackupContext.CreateInstance();

        var batch = await context.CloudTransferBatches.SingleAsync(x => x.Id == cloudTransferBatchId);

        var startDateTime = startTime ?? DateTime.Now;

        var stopDateTime = startDateTime.AddHours(batch.Job!.MaximumRunTimeInHours);

        var uploads = batch.CloudUploads.Where(x => !x.UploadCompletedSuccessfully).ToList();

        var totalUploadEstimatedLength = uploads.Sum(x => x.FileSize);

        progress?.Report(
            $"Starting Uploads for Batch {batch.Id} - {uploads.Count} Files, {FileAndFolderTools.GetBytesReadable(totalUploadEstimatedLength)}");

        var transferUtility = new TransferUtility(accountInformation.S3Client());
        var pollyS3RetryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(2 * (retryAttempt + 1)));

        var uploadCount = 0;

        var totalUploadedLength = 0L;
        var totalUploadedSeconds = 0D;

        var uploadErrorCount = 0;

        foreach (var upload in uploads)
        {
            uploadCount++;

            var localFile = new FileInfo(upload.FileSystemFile);

            if (!localFile.Exists)
            {
                upload.ErrorMessage += $"{DateTime.Now:yyyy-MM-dd--hh:mm}: Local File {localFile.FullName} Not Found;";
                upload.LastUpdatedOn = DateTime.Now;
                await context.SaveChangesAsync();
                uploadErrorCount++;
                continue;
            }

            var uploadLength = new FileInfo(upload.FileSystemFile).Length;
            var estimateCurrentUpload = "Estimated Completion Unknown...";
            var estimateTotalUpload = "Estimated Completion Unknown...";

            if (totalUploadedSeconds > 0 && totalUploadedLength > 0 && uploadLength > 0)
            {
                var currentSecondsPerLength = totalUploadedSeconds / totalUploadedLength;

                var currentUploadEstimatedSeconds = uploadLength * currentSecondsPerLength;

                var estimateCurrentUploadCompleteIn = currentUploadEstimatedSeconds switch
                {
                    < 60 => $"{currentUploadEstimatedSeconds:N2} Seconds",
                    < 360 => $"{currentUploadEstimatedSeconds / 60D:N2} Minutes",
                    < 86400 => $"{currentUploadEstimatedSeconds / 360D:N2} Hours",
                    _ => $"{currentUploadEstimatedSeconds / 86400D:N2} Days"
                };

                estimateCurrentUpload =
                    $"Estimated Completion in {estimateCurrentUploadCompleteIn} ({DateTime.Now.AddSeconds(uploadLength * currentSecondsPerLength):h:mm:ss tt})";

                var currentFinishEstimate =
                    DateTime.Now.AddSeconds(totalUploadEstimatedLength * currentSecondsPerLength);

                var totalUploadEstimatedSeconds = totalUploadEstimatedLength * currentSecondsPerLength;

                var estimateCompleteIn = totalUploadEstimatedSeconds switch
                {
                    < 60 => $"{totalUploadEstimatedSeconds:N2} Seconds",
                    < 360 => $"{totalUploadEstimatedSeconds / 60D:N2} Minutes",
                    < 86400 => $"{totalUploadEstimatedSeconds / 360D:N2} Hours",
                    _ => $"{totalUploadEstimatedSeconds / 86400D:N2} Days"
                };

                estimateTotalUpload =
                    $"Estimated Completion in {estimateCompleteIn} ({DateTime.Now.AddSeconds(totalUploadEstimatedLength * currentSecondsPerLength):G}){(currentFinishEstimate > stopDateTime ? $" - this run will stop at {stopDateTime:G} ({batch.Job!.MaximumRunTimeInHours} Hour Max)" : string.Empty)}";
            }

            var uploadStartTime = DateTime.Now;

            if (uploadCount == 1 || uploadCount == 2 || uploadCount == 5 || uploadCount == 10 || uploadCount % 15 == 0)
                progress?.Report(
                    $"Upload {uploadCount} of {uploads.Count}, {FileAndFolderTools.GetBytesReadable(totalUploadedLength)} of {FileAndFolderTools.GetBytesReadable(totalUploadEstimatedLength - totalUploadedLength)} - {estimateTotalUpload}");

            progress?.Report(
                $"Upload {uploadCount} - {upload.CloudObjectKey} - {FileAndFolderTools.GetBytesReadable(uploadLength)} - {estimateCurrentUpload}");

            var transferRequest =
                await S3Tools.S3TransferUploadRequest(localFile, upload.BucketName, upload.CloudObjectKey);

            try
            {
                await pollyS3RetryPolicy.ExecuteAsync(async () => await transferUtility.UploadAsync(transferRequest));
                upload.LastUpdatedOn = DateTime.Now;
                upload.UploadCompletedSuccessfully = true;
                upload.ErrorMessage = string.Empty;

                var cacheEntry = await context.CloudCacheFiles.SingleOrDefaultAsync(x =>
                    x.JobId == batch.JobId && x.Bucket == upload.BucketName &&
                    x.CloudObjectKey == upload.CloudObjectKey);

                if (cacheEntry != null)
                {
                    cacheEntry.LastEditOn = DateTime.Now;
                    cacheEntry.Note += $"{DateTime.Now:s} Upload;";
                }
                else
                {
                    cacheEntry = new CloudCacheFile
                    {
                        Bucket = upload.BucketName,
                        CloudObjectKey = upload.CloudObjectKey,
                        JobId = batch.JobId,
                        LastEditOn = DateTime.Now,
                        Note = $"{DateTime.Now:s} Upload;"
                    };

                    context.CloudCacheFiles.Add(cacheEntry);
                }

                var localMetadata = await S3Tools.LocalFileAndMetadata(localFile);

                cacheEntry.FileHash = localMetadata.UploadMetadata.FileSystemHash;
                cacheEntry.FileSize = localFile.Length;
                cacheEntry.FileSystemDateTime = localMetadata.UploadMetadata.LastWriteTime;

                await context.SaveChangesAsync();

                var elapsed = DateTime.Now.Subtract(uploadStartTime);
                totalUploadedLength += uploadLength;
                totalUploadedSeconds += elapsed.TotalSeconds;
            }
            catch (Exception e)
            {
                upload.ErrorMessage += $"{DateTime.Now:yyyy-MM-dd--hh:mm}: Upload Failed - {e.Message};";
                await context.SaveChangesAsync();

                progress?.Report($"Upload Failed - {e.Message}");

                uploadErrorCount++;
            }

            if (DateTime.Now > stopDateTime)
            {
                progress?.Report($"Ending Batch {batch.Id} based on Maximum Runtime - {uploads.Count} Files");
                return new CloudUploadAndDeleteRunInformation
                {
                    DeleteCount = 0,
                    Ended = DateTime.Now,
                    EndedBecauseOfMaxRuntime = true,
                    Started = startDateTime,
                    UploadCount = uploadCount,
                    UploadedSize = totalUploadedLength,
                    UploadSeconds = totalUploadedSeconds,
                    UploadErrorCount = uploadErrorCount
                };
            }
        }

        var deletes = batch.CloudDeletions.Where(x => !x.DeletionCompletedSuccessfully).ToList();

        var deleteCount = 0;
        var deleteErrorCount = 0;

        progress?.Report($"Starting Deletes for Batch {batch.Id} - {deletes.Count} Files");

        foreach (var delete in deletes)
        {
            deleteCount++;
            progress?.Report($"Delete {deleteCount} of {deletes.Count} - {delete.CloudObjectKey}");
            try
            {
                await pollyS3RetryPolicy.ExecuteAsync(async () =>
                    await accountInformation.S3Client()
                        .DeleteObjectAsync(delete.BucketName, delete.CloudObjectKey));
                delete.LastUpdatedOn = DateTime.Now;
                delete.DeletionCompletedSuccessfully = true;

                var cacheEntry = await context.CloudCacheFiles.SingleOrDefaultAsync(x =>
                    x.JobId == batch.JobId && x.Bucket == delete.BucketName &&
                    x.CloudObjectKey == delete.CloudObjectKey);

                if (cacheEntry != null) context.CloudCacheFiles.Remove(cacheEntry);

                await context.SaveChangesAsync();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                delete.LastUpdatedOn = DateTime.Now;
                delete.DeletionCompletedSuccessfully = true;
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                delete.ErrorMessage += $"{DateTime.Now:yyyy-MM-dd--hh:mm}: Delete Failed - {e.Message};";
                await context.SaveChangesAsync();

                progress?.Report($"Delete Failed - {e.Message}");

                deleteErrorCount++;
            }

            if (DateTime.Now > stopDateTime)
            {
                progress?.Report($"Ending Batch {batch.Id} based on Maximum Runtime - {uploads.Count} Files");
                return new CloudUploadAndDeleteRunInformation
                {
                    DeleteCount = deleteCount,
                    Ended = DateTime.Now,
                    EndedBecauseOfMaxRuntime = true,
                    Started = startDateTime,
                    UploadCount = uploadCount,
                    UploadedSize = totalUploadedLength,
                    UploadSeconds = totalUploadedSeconds,
                    UploadErrorCount = uploadErrorCount,
                    DeleteErrorCount = deleteErrorCount
                };
            }
        }

        progress?.Report($"Batch {batch.Id} - Finished");

        return new CloudUploadAndDeleteRunInformation
        {
            DeleteCount = deleteCount,
            Ended = DateTime.Now,
            EndedBecauseOfMaxRuntime = false,
            Started = startDateTime,
            UploadCount = uploadCount,
            UploadedSize = totalUploadedLength,
            UploadSeconds = totalUploadedSeconds,
            UploadErrorCount = uploadErrorCount,
            DeleteErrorCount = deleteErrorCount
        };
    }

    public static async Task<CloudTransferBatch> CreateBatchInDatabaseFromCloudAndLocalScan(
        IS3AccountInformation accountInformation, BackupJob job, IProgress<string> progress)
    {
        Log.Information("Creating new Batch based on Cloud and Local Scan");

        var changes = await CreationTools.GetChangesBasedOnCloudAndLocalScan(accountInformation, job.Id, progress);
        return await CreationTools.WriteChangesToDatabase(changes);
    }

    public static async Task<CloudTransferBatch> CreateBatchInDatabaseFromCloudCacheFilesAndLocalScan(
        IS3AccountInformation accountInformation, BackupJob job, IProgress<string> progress)
    {
        Log.Information("Creating new Batch based on Cloud Cache Files and Local Scan");

        var changes = await CreationTools.GetChangesBasedOnCloudCacheFilesAndLocalScan(accountInformation, job.Id, progress);
        return await CreationTools.WriteChangesToDatabase(changes);
    }
}