namespace PointlessWaymarks.CloudBackupData.Models;

public class FileSystemFile
{
    public virtual CloudTransferBatch? CloudTransferBatch { get; set; }
    public int CloudTransferBatchId { get; set; }
    public DateTime CreatedOn { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSystemDateTime { get; set; } = string.Empty;
    public int Id { get; set; }
    public virtual BackupJob? Job { get; set; }
    public int JobId { get; set; }
}