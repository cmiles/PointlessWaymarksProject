namespace PointlessWaymarks.CloudBackupData.Models;

public class CloudTransferBatch
{
    public DateTime CreatedOn { get; set; }
    public int Id { get; set; }
    public virtual BackupJob? Job { get; set; }
    public int JobId { get; set; }

    public virtual ICollection<CloudUpload> CloudUploads { get; } = new List<CloudUpload>();
    public virtual ICollection<CloudDelete> CloudDeletions { get; } = new List<CloudDelete>();
    public virtual ICollection<FileSystemFile> FileSystemFiles { get; } = new List<FileSystemFile>();
}