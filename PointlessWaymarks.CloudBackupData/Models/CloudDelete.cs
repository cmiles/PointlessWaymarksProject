namespace PointlessWaymarks.CloudBackupData.Models;

public class CloudDelete
{
    public CloudTransferBatch CloudTransferBatch { get; set; }
    public int CloudTransferBatchId { get; set; }
    public string BucketName { get; set; }
    public string CloudObjectKey { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool DeletionCompletedSuccessfully { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int Id { get; set; }
    public DateTime LastUpdatedOn { get; set; }
}