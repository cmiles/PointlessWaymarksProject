namespace PointlessWaymarks.CloudBackupData.Models;

public class CloudDelete
{
    public string BucketName { get; set; } = string.Empty;
    public string CloudObjectKey { get; set; } = string.Empty;
    public virtual CloudTransferBatch? CloudTransferBatch { get; set; }
    public int CloudTransferBatchId { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool DeletionCompletedSuccessfully { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int Id { get; set; }
    public DateTime LastUpdatedOn { get; set; }
}