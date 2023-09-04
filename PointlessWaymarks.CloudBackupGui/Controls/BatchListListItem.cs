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

    public static BatchListListItem CreateInstance(CloudTransferBatch batch)
    {
        var toReturn = new BatchListListItem
        {
            DbBatch = batch,
            LocalFileCount = batch.FileSystemFiles.Count,
            LocalFileSize = batch.FileSystemFiles.Sum(x => x.FileSize),
            CloudFileCount = batch.CloudFiles.Count,
            CloudFileSize = batch.CloudFiles.Sum(x => x.FileSize),
            UploadCount = batch.CloudUploads.Count,
            UploadSize = batch.CloudUploads.Sum(x => x.FileSize),
            UploadsCompleteCount = batch.CloudUploads.Count(x => x.UploadCompletedSuccessfully),
            UploadsCompleteSize = batch.CloudUploads.Where(x => x.UploadCompletedSuccessfully).Sum(x => x.FileSize),
            UploadsNotCompletedCount = batch.CloudUploads.Count(x => !x.UploadCompletedSuccessfully),
            UploadsNotCompletedSize = batch.CloudUploads.Where(x => !x.UploadCompletedSuccessfully).Sum(x => x.FileSize),
            UploadsWithErrorNoteCount = batch.CloudUploads.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)),
            UploadsWithErrorNoteSize = batch.CloudUploads.Where(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)).Sum(x => x.FileSize),
            DeletesCount = batch.CloudDeletions.Count(),
            DeletesSize = batch.CloudDeletions.Sum(x => x.FileSize),
            DeletesCompleteCount = batch.CloudDeletions.Count(x => x.DeletionCompletedSuccessfully),
            DeletesCompleteSize = batch.CloudDeletions.Where(x => x.DeletionCompletedSuccessfully).Sum(x => x.FileSize),
            DeletesNotCompletedCount = batch.CloudDeletions.Count(x => !x.DeletionCompletedSuccessfully),
            DeletesNotCompletedSize = batch.CloudDeletions.Where(x => !x.DeletionCompletedSuccessfully).Sum(x => x.FileSize),
            DeletesWithErrorNoteCount = batch.CloudDeletions.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)),
            DeletesWithErrorNoteSize = batch.CloudDeletions.Where(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)).Sum(x => x.FileSize)
        };

        toReturn.SizeCompletedPercentage = toReturn.UploadSize == 0 ? 1M : (decimal) toReturn.UploadsCompleteSize / toReturn.UploadSize;
        
        return toReturn;
    }
}