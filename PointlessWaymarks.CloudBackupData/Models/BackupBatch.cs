namespace PointlessWaymarks.CloudBackupGui.Models;

public class BackupBatch
{
    public Guid BatchId { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool DeletionsCompletedSuccessfully { get; set; }
    public int Id { get; set; }
    public string Notes { get; set; }
    public bool UploadsCompletedSuccessfully { get; set; }
}