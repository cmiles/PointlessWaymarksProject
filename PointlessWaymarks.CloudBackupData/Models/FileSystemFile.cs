namespace PointlessWaymarks.CloudBackupData.Models;

public class FileSystemFile
{
    public string CreatedOn { get; set; }
    public string FileHash { get; set; }
    public decimal FileSize { get; set; }
    public DateTime FileSystemDateTime { get; set; }
    public int Id { get; set; }
    public string LastUpdatedOn { get; set; }
}