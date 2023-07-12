using System.Net;
using Amazon.S3.Transfer;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;
using Polly;

namespace PointlessWaymarks.CloudBackupData.Batch;

public static class CloudTransfer
{
    public static async Task<CloudTransferBatch> CreateBatchInDatabaseFromChanges(IS3AccountInformation accountInformation, BackupJob job)
    {
        var changes = await CreationTools.GetChanges(accountInformation, job);
        return await CreationTools.WriteChangesToDatabase(changes);
    }

    public static async Task CloudUploadsAndDeletes(IS3AccountInformation accountInformation, int cloudTransferBatchId,
      IProgress<string>? progress)
    {
        var context = await CloudBackupContext.CreateInstance();

        var batch = await context.CloudTransferBatches.SingleAsync(x => x.Id == cloudTransferBatchId);

        var stopDateTime = DateTime.Now.AddHours(batch.Job!.MaximumRunTimeInHours);

        var uploads = batch.CloudUploads.Where(x => !x.UploadCompletedSuccessfully).ToList();

        progress?.Report($"Starting Uploads for Batch {batch.Id} - {uploads.Count} Files");

        var transferUtility = new TransferUtility(accountInformation.S3Client());
        var pollyS3RetryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(2 * (retryAttempt + 1)));

        var uploadCount = 0;

        foreach (var upload in uploads)
        {
            uploadCount++;

            progress?.Report($"Upload {uploadCount} of {uploads.Count} - {upload.CloudObjectKey}");

            var localFile = new FileInfo(upload.FileSystemFile);

            if (!localFile.Exists)
            {
                upload.ErrorMessage += $"{DateTime.Now:yyyy-MM-dd--hh:mm}: Local File {localFile.FullName} Not Found;";
                upload.LastUpdatedOn = DateTime.Now;
                await context.SaveChangesAsync();
            }

            var transferRequest =
                await S3Tools.S3TransferUploadRequest(localFile, upload.BucketName, upload.CloudObjectKey);

            try
            {
                await pollyS3RetryPolicy.ExecuteAsync(async () => await transferUtility.UploadAsync(transferRequest));
                upload.LastUpdatedOn = DateTime.Now;
                upload.UploadCompletedSuccessfully = true;
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                upload.ErrorMessage += $"{DateTime.Now:yyyy-MM-dd--hh:mm}: Upload Failed - {e.Message};";
                await context.SaveChangesAsync();

                progress?.Report($"Upload Failed - {e.Message}");
            }

            if (DateTime.Now > stopDateTime)
            {
                progress?.Report($"Ending Batch {batch.Id} based on Maximum Runtime - {uploads.Count} Files");
            }
        }

        var deletes = batch.CloudDeletions.Where(x => !x.DeletionCompletedSuccessfully).ToList();

        var deleteCount = 0;

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
            }

            if (DateTime.Now > stopDateTime)
            {
                progress?.Report($"Ending Batch {batch.Id} based on Maximum Runtime - {uploads.Count} Files");
            }
        }

        progress?.Report($"Batch {batch.Id} - Finished");
    }
}