namespace PointlessWaymarks.CloudBackupData.Models;

public class FileSystemFile
{
    public DateTime CreatedOn { get; set; }
    public string FileHash { get; set; }
    public string FileSystemDateTime { get; set; }
    public int Id { get; set; }
    public BackupJob Job { get; set; }
    public int JobId { get; set; }
    public int CloudTransferBatchId { get; set; }
    public CloudTransferBatch CloudTransferBatch { get; set; }

}