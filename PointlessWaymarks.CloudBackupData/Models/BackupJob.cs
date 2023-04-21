namespace PointlessWaymarks.CloudBackupData.Models;

public class BackupJob
{
    public ICollection<CloudTransferBatch> Batches { get; set; }
    public string CloudDirectory { get; set; }
    public DateTime CreatedOn { get; set; }
    public ICollection<ExcludedDirectory> ExcludedDirectories { get; set; }
    public ICollection<ExcludedDirectoryNamePattern> ExcludedDirectoryNamePatterns { get; set; }
    public ICollection<ExcludedFileNamePattern> ExcludedFileNamePatterns { get; set; }
    public ICollection<FileSystemFile> FileSystemFiles { get; set; }
    public int Id { get; set; }
    public string LocalDirectory { get; set; }
}