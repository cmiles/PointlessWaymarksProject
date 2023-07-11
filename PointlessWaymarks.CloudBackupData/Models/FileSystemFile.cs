namespace PointlessWaymarks.CloudBackupData.Models;

public class FileSystemFile
{
    public DateTime CreatedOn { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public string FileSystemDateTime { get; set; } = string.Empty;
    public int Id { get; set; }
    public virtual BackupJob? Job { get; set; }
    public int JobId { get; set; }
    public int CloudTransferBatchId { get; set; }
    public virtual CloudTransferBatch? CloudTransferBatch { get; set; }

}