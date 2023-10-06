using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
public partial class BatchListListItem
{
    public int CloudFileCount { get; set; }
    public long CloudFileSize { get; set; }
    public required CloudTransferBatch DbBatch { get; set; }
    public int DeletesCompleteCount { get; set; }
    public long DeletesCompleteSize { get; set; }
    public int DeletesCount { get; set; }
    public int DeletesNotCompletedCount { get; set; }
    public long DeletesNotCompletedSize { get; set; }
    public long DeletesSize { get; set; }
    public int DeletesWithErrorNoteCount { get; set; }
    public long DeletesWithErrorNoteSize { get; set; }
    public int LocalFileCount { get; set; }
    public long LocalFileSize { get; set; }
    public decimal SizeCompletedPercentage { get; set; }
    public int UploadCount { get; set; }
    public int UploadsCompleteCount { get; set; }
    public long UploadsCompleteSize { get; set; }
    public long UploadSize { get; set; }
    public int UploadsNotCompletedCount { get; set; }
    public long UploadsNotCompletedSize { get; set; }
    public int UploadsWithErrorNoteCount { get; set; }
    public long UploadsWithErrorNoteSize { get; set; }

    public static async Task<BatchListListItem> CreateInstance(CloudTransferBatch batch)
    {
        var context = await CloudBackupContext.CreateInstance();

        var toReturn = new BatchListListItem
        {
            DbBatch = batch,
            LocalFileCount = await context.FileSystemFiles.CountAsync(x => x.CloudTransferBatchId == batch.Id),
            LocalFileSize = await context.FileSystemFiles.Where(x => x.CloudTransferBatchId == batch.Id).SumAsync(x => x.FileSize),
            CloudFileCount = await context.CloudFiles.CountAsync(x => x.CloudTransferBatchId == batch.Id),
            CloudFileSize = await context.CloudFiles.Where(x => x.CloudTransferBatchId == batch.Id).SumAsync(x => x.FileSize),
            UploadCount = await context.CloudUploads.CountAsync(x => x.CloudTransferBatchId == batch.Id),
            UploadSize = await context.CloudUploads.Where(x => x.CloudTransferBatchId == batch.Id).SumAsync(x => x.FileSize),
            UploadsCompleteCount = await context.CloudUploads.CountAsync(x =>
                x.CloudTransferBatchId == batch.Id && x.UploadCompletedSuccessfully),
            UploadsCompleteSize = await context.CloudUploads
                .Where(x => x.CloudTransferBatchId == batch.Id && x.UploadCompletedSuccessfully)
                .SumAsync(x => x.FileSize),
            UploadsNotCompletedCount = await context.CloudUploads.CountAsync(x =>
                x.CloudTransferBatchId == batch.Id && !x.UploadCompletedSuccessfully),
            UploadsNotCompletedSize = await context.CloudUploads
                .Where(x => x.CloudTransferBatchId == batch.Id && !x.UploadCompletedSuccessfully)
                .SumAsync(x => x.FileSize),
            UploadsWithErrorNoteCount = await context.CloudUploads.CountAsync(x =>
                x.CloudTransferBatchId == batch.Id && !string.IsNullOrWhiteSpace(x.ErrorMessage)),
            UploadsWithErrorNoteSize = await context.CloudUploads
                .Where(x => x.CloudTransferBatchId == batch.Id && !string.IsNullOrWhiteSpace(x.ErrorMessage))
                .SumAsync(x => x.FileSize),
            DeletesCount = await context.CloudDeletions.CountAsync(x => x.CloudTransferBatchId == batch.Id),
            DeletesSize = await context.CloudDeletions.SumAsync(x => x.FileSize),
            DeletesCompleteCount = await context.CloudDeletions.CountAsync(x =>
                x.CloudTransferBatchId == batch.Id && x.DeletionCompletedSuccessfully),
            DeletesCompleteSize = await context.CloudDeletions
                .Where(x => x.CloudTransferBatchId == batch.Id && x.DeletionCompletedSuccessfully)
                .SumAsync(x => x.FileSize),
            DeletesNotCompletedCount = await context.CloudDeletions.CountAsync(x =>
                x.CloudTransferBatchId == batch.Id && !x.DeletionCompletedSuccessfully),
            DeletesNotCompletedSize = await context.CloudDeletions
                .Where(x => x.CloudTransferBatchId == batch.Id && !x.DeletionCompletedSuccessfully)
                .SumAsync(x => x.FileSize),
            DeletesWithErrorNoteCount = await context.CloudDeletions.CountAsync(x =>
                x.CloudTransferBatchId == batch.Id && !string.IsNullOrWhiteSpace(x.ErrorMessage)),
            DeletesWithErrorNoteSize = await context.CloudDeletions
                .Where(x => x.CloudTransferBatchId == batch.Id && !string.IsNullOrWhiteSpace(x.ErrorMessage))
                .SumAsync(x => x.FileSize)
        };

        toReturn.SizeCompletedPercentage = toReturn.UploadSize == 0
            ? 1M
            : (decimal)toReturn.UploadsCompleteSize / toReturn.UploadSize;

        return toReturn;
    }
}