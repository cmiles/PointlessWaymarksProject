namespace PointlessWaymarks.CloudBackupData.Batch;

public class CloudBackupLocalDirectory
{
    public required DirectoryInfo Directory { get; set; }
    public bool Included { get; set; }
}