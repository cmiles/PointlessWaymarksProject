namespace PointlessWaymarks.CloudBackupData.Models;

public class ExcludedDirectory
{
    public DateTime CreatedOn { get; set; }
    public string Directory { get; set; }  = string.Empty;
    public int Id { get; set; }
    public virtual BackupJob? Job { get; set; }
    public int JobId { get; set; }
}