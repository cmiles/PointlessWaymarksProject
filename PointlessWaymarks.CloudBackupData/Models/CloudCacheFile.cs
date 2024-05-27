namespace PointlessWaymarks.CloudBackupData.Models;

public class CloudCacheFile
{
    public string Bucket { get; set; } = string.Empty;
    public string CloudObjectKey { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSystemDateTime { get; set; } = string.Empty;
    public int Id { get; set; }
    public virtual BackupJob? Job { get; set; }
    public int BackupJobId { get; set; }
    public DateTime LastEditOn { get; set; }
    public string Note { get; set; } = string.Empty;
}