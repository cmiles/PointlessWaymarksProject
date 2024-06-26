namespace PointlessWaymarks.CloudBackupData.Models;

public class CloudTransferBatch
{
    public bool BasedOnNewCloudFileScan { get; set; }
    public virtual ICollection<CloudCopy> CloudCopies { get; } = new List<CloudCopy>();
    public virtual ICollection<CloudDelete> CloudDeletions { get; } = new List<CloudDelete>();
    public virtual ICollection<CloudFile> CloudFiles { get; } = new List<CloudFile>();
    public virtual ICollection<CloudUpload> CloudUploads { get; } = new List<CloudUpload>();
    public DateTime CreatedOn { get; set; }
    public virtual ICollection<FileSystemFile> FileSystemFiles { get; } = new List<FileSystemFile>();
    public int Id { get; set; }
    public virtual BackupJob? Job { get; set; }
    public int BackupJobId { get; set; }
}