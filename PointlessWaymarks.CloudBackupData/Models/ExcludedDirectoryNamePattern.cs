namespace PointlessWaymarks.CloudBackupData.Models;

public class ExcludedDirectoryNamePattern
{
    public DateTime CreatedOn { get; set; }
    public int Id { get; set; }
    public virtual BackupJob? Job { get; set; }
    public int BackupJobId { get; set; }
    public string Pattern { get; set; }  = string.Empty;
}