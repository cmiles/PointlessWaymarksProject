namespace PointlessWaymarks.CloudBackupData.Models;

public class CloudUpload
{
    public Guid BatchId { get; set; }
    public string BucketName { get; set; }
    public string CloudObjectKey { get; set; }
    public DateTime CreatedOn { get; set; }
    public string ErrorMessage { get; set; }
    public string FilePath { get; set; }
    public int Id { get; set; }
    public DateTime LastUpdatedOn { get; set; }
    public bool UploadCompletedSuccessfully { get; set; }
}