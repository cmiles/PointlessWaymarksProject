using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
public partial class BatchListListItem
{
    public int CloudFileCount { get; set; }
    public required CloudTransferBatch DbBatch { get; set; }
    public int DeletesCompleteCount { get; set; }
    public int DeletesCount { get; set; }
    public int DeletesNotCompletedCount { get; set; }
    public int DeletesWithErrorNoteCount { get; set; }
    public int LocalFileCount { get; set; }
    public int UploadCount { get; set; }
    public int UploadsCompleteCount { get; set; }
    public int UploadsNotCompletedCount { get; set; }
    public int UploadsWithErrorNoteCount { get; set; }

    public static BatchListListItem CreateInstance(CloudTransferBatch batch)
    {
        return new BatchListListItem
        {
            DbBatch = batch,
            LocalFileCount = batch.FileSystemFiles.Count,
            CloudFileCount = batch.CloudFiles.Count,
            UploadCount = batch.CloudUploads.Count,
            UploadsCompleteCount = batch.CloudUploads.Count(x => x.UploadCompletedSuccessfully),
            UploadsNotCompletedCount = batch.CloudUploads.Count(x => !x.UploadCompletedSuccessfully),
            UploadsWithErrorNoteCount = batch.CloudUploads.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)),
            DeletesCount = batch.CloudDeletions.Count(),
            DeletesCompleteCount = batch.CloudDeletions.Count(x => x.DeletionCompletedSuccessfully),
            DeletesNotCompletedCount = batch.CloudDeletions.Count(x => !x.DeletionCompletedSuccessfully),
            DeletesWithErrorNoteCount = batch.CloudDeletions.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))
        };
    }
}