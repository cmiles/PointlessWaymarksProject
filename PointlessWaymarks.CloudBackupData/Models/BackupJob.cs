namespace PointlessWaymarks.CloudBackupData.Models;

public class BackupJob
{
    public string CloudDirectory { get; set; }
    public ICollection<CloudTransferBatch> CloudTransferBatches { get; set; }
    public DateTime CreatedOn { get; set; }
    public ICollection<ExcludedDirectory> ExcludedDirectories { get; set; }
    public ICollection<ExcludedDirectoryNamePattern> ExcludedDirectoryNamePatterns { get; set; }
    public ICollection<ExcludedFileNamePattern> ExcludedFileNamePatterns { get; set; }
    public ICollection<FileSystemFile> FileSystemFiles { get; set; }
    public int Id { get; set; }
    public string LocalDirectory { get; set; }
}