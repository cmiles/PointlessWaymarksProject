namespace PointlessWaymarks.CloudBackupData.Models;

public class ExcludedFileNamePattern
{
    public DateTime CreatedOn { get; set; }
    public int Id { get; set; }
    public virtual BackupJob Job { get; set; }
    public int JobId { get; set; }
    public string Pattern { get; set; }
}