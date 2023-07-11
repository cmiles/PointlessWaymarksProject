namespace PointlessWaymarks.CloudBackupData.Models;

public class CloudUpload
{
    public virtual CloudTransferBatch? CloudTransferBatch { get; set; }
    public int CloudTransferBatchId { get; set; }
    public string BucketName { get; set; } = string.Empty;
    public string CloudObjectKey { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string FileSystemFile { get; set; } = string.Empty;
    public int Id { get; set; }
    public DateTime LastUpdatedOn { get; set; }
    public bool UploadCompletedSuccessfully { get; set; }
}