namespace PointlessWaymarks.CloudBackupData.Models;

public class CloudTransferBatch
{
    public DateTime CreatedOn { get; set; }
    public bool DeletionsCompletedSuccessfully { get; set; }
    public int Id { get; set; }
    public BackupJob Job { get; set; }
    public int JobId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool UploadsCompletedSuccessfully { get; set; }

    public ICollection<CloudUpload> CloudUploads { get; } = new List<CloudUpload>();
    public ICollection<CloudDelete> CloudDeletions { get; } = new List<CloudDelete>();
    public ICollection<FileSystemFile> FileSystemFiles { get; } = new List<FileSystemFile>();
}