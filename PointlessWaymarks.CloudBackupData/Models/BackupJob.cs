using System.ComponentModel.DataAnnotations.Schema;

namespace PointlessWaymarks.CloudBackupData.Models;

public class BackupJob
{
    public virtual ICollection<CloudTransferBatch> Batches { get; set; } = new List<CloudTransferBatch>();
    public string CloudBucket { get; set; } = string.Empty;
    public string CloudDirectory { get; set; } = string.Empty;
    public string CloudRegion { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public virtual ICollection<ExcludedDirectory> ExcludedDirectories { get; set; } = new List<ExcludedDirectory>();

    public virtual ICollection<ExcludedDirectoryNamePattern> ExcludedDirectoryNamePatterns { get; set; } =
        new List<ExcludedDirectoryNamePattern>();

    public virtual ICollection<ExcludedFileNamePattern> ExcludedFileNamePatterns { get; set; } =
        new List<ExcludedFileNamePattern>();

    public virtual ICollection<CloudCacheFile> CloudCacheFiles { get; set; } =
        new List<CloudCacheFile>();

    public virtual ICollection<FileSystemFile> FileSystemFiles { get; set; } = new List<FileSystemFile>();
    public int Id { get; set; }
    public string LocalDirectory { get; set; } = string.Empty;
    public int MaximumRunTimeInHours { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PersistentId { get; set; } = Guid.Empty;

    [NotMapped] public string VaultIdentifier => $"Pointless-{PersistentId}";
}