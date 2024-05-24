using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupData.Reports;

[NotifyPropertyChanged]
public class BatchStatistics
{
    public bool BasedOnNewCloudFileScan { get; set; }
    public required DateTime BatchCreatedOn { get; set; }
    public int BatchId { get; set; }
    public int CloudFileCount { get; set; }
    public long CloudFileSize { get; set; }
    public int CopiesCompleteCount { get; set; }
    public long CopiesCompleteSize { get; set; }
    public int CopiesNotCompletedCount { get; set; }
    public long CopiesNotCompletedSize { get; set; }
    public long CopiesSize { get; set; }
    public decimal CopiesSizeCompletedPercentage { get; set; }
    public int CopiesWithErrorNoteCount { get; set; }
    public long CopiesWithErrorNoteSize { get; set; }
    public int CopyCount { get; set; }
    public int DeletesCompleteCount { get; set; }
    public long DeletesCompleteSize { get; set; }
    public int DeletesCount { get; set; }
    public int DeletesNotCompletedCount { get; set; }
    public long DeletesNotCompletedSize { get; set; }
    public long DeletesSize { get; set; }
    public int DeletesWithErrorNoteCount { get; set; }
    public long DeletesWithErrorNoteSize { get; set; }
    public int JobId { get; set; }
    public int LocalFileCount { get; set; }
    public long LocalFileSize { get; set; }
    public int UploadCount { get; set; }
    public int UploadsCompleteCount { get; set; }
    public long UploadsCompleteSize { get; set; }
    public long UploadSize { get; set; }
    public int UploadsNotCompletedCount { get; set; }
    public long UploadsNotCompletedSize { get; set; }
    public decimal UploadsSizeCompletedPercentage { get; set; }
    public int UploadsWithErrorNoteCount { get; set; }
    public long UploadsWithErrorNoteSize { get; set; }
    
    public static async Task<BatchStatistics> CreateInstance(int batchId)
    {
        var context = await CloudBackupContext.CreateReportingInstance();
        
        var batch = context.CloudTransferBatches.Single(x => x.Id == batchId);
        
        var toReturn = new BatchStatistics
        {
            BatchCreatedOn = batch.CreatedOn,
            BatchId = batch.Id,
            JobId = batch.JobId,
            BasedOnNewCloudFileScan = batch.BasedOnNewCloudFileScan,
            LocalFileCount = await context.FileSystemFiles.CountAsync(x => x.CloudTransferBatchId == batch.Id),
            LocalFileSize = await context.FileSystemFiles.Where(x => x.CloudTransferBatchId == batch.Id)
                .SumAsync(x => x.FileSize),
            CloudFileCount = await context.CloudFiles.CountAsync(x => x.CloudTransferBatchId == batch.Id),
            CloudFileSize = await context.CloudFiles.Where(x => x.CloudTransferBatchId == batch.Id)
                .SumAsync(x => x.FileSize),
            UploadCount = await context.CloudUploads.CountAsync(x => x.CloudTransferBatchId == batch.Id),
            UploadSize = await context.CloudUploads.Where(x => x.CloudTransferBatchId == batch.Id)
                .SumAsync(x => x.FileSize),
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
            CopyCount = await context.CloudCopies.CountAsync(x => x.CloudTransferBatchId == batch.Id),
            CopiesSize = await context.CloudCopies.Where(x => x.CloudTransferBatchId == batch.Id)
                .SumAsync(x => x.FileSize),
            CopiesCompleteCount = await context.CloudCopies.CountAsync(x =>
                x.CloudTransferBatchId == batch.Id && x.CopyCompletedSuccessfully),
            CopiesCompleteSize = await context.CloudCopies
                .Where(x => x.CloudTransferBatchId == batch.Id && x.CopyCompletedSuccessfully)
                .SumAsync(x => x.FileSize),
            CopiesNotCompletedCount = await context.CloudCopies.CountAsync(x =>
                x.CloudTransferBatchId == batch.Id && !x.CopyCompletedSuccessfully),
            CopiesNotCompletedSize = await context.CloudCopies
                .Where(x => x.CloudTransferBatchId == batch.Id && !x.CopyCompletedSuccessfully)
                .SumAsync(x => x.FileSize),
            CopiesWithErrorNoteCount = await context.CloudCopies.CountAsync(x =>
                x.CloudTransferBatchId == batch.Id && !string.IsNullOrWhiteSpace(x.ErrorMessage)),
            CopiesWithErrorNoteSize = await context.CloudCopies
                .Where(x => x.CloudTransferBatchId == batch.Id && !string.IsNullOrWhiteSpace(x.ErrorMessage))
                .SumAsync(x => x.FileSize),
            DeletesCount = await context.CloudDeletions.CountAsync(x => x.CloudTransferBatchId == batch.Id),
            DeletesSize = await context.CloudDeletions.Where(x => x.CloudTransferBatchId == batch.Id).SumAsync(x => x.FileSize),
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
        
        toReturn.CopiesSizeCompletedPercentage = toReturn.CopiesSize == 0
            ? 1M
            : (decimal)toReturn.CopiesCompleteSize / toReturn.CopiesSize;
        
        toReturn.UploadsSizeCompletedPercentage = toReturn.UploadSize == 0
            ? 1M
            : (decimal)toReturn.UploadsCompleteSize / toReturn.UploadSize;
        
        return toReturn;
    }
    
    public async Task Refresh()
    {
        var newStatistics = await CreateInstance(BatchId);
        this.InjectFrom(newStatistics);
    }
}