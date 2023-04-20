namespace PointlessWaymarks.CloudBackupData.Models;

public class ExcludedDirectory
{
    public DateTime CreatedOn { get; set; }
    public string Directory { get; set; }
    public int Id { get; set; }
    public BackupJob Job { get; set; }
    public int JobId { get; set; }
}