namespace PointlessWaymarks.CloudBackupData.Models;

public class BackupJob
{
    public virtual ICollection<CloudTransferBatch> Batches { get; set; }
    public string CloudDirectory { get; set; }
    public DateTime CreatedOn { get; set; }
    public virtual ICollection<ExcludedDirectory> ExcludedDirectories { get; set; }
    public virtual ICollection<ExcludedDirectoryNamePattern> ExcludedDirectoryNamePatterns { get; set; }
    public virtual ICollection<ExcludedFileNamePattern> ExcludedFileNamePatterns { get; set; }
    public virtual ICollection<FileSystemFile> FileSystemFiles { get; set; }
    public int Id { get; set; }
    public string LocalDirectory { get; set; }
    public int DefaultMaximumRunTimeInHours { get; set; }
    public string Name { get; set; }
}