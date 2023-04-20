namespace PointlessWaymarks.CloudBackupData.Models;

public class CloudTransferBatch
{
    public DateTime CreatedOn { get; set; }
    public bool DeletionsCompletedSuccessfully { get; set; }
    public int Id { get; set; }
    public BackupJob Job { get; set; }
    public int JobId { get; set; }
    public string Notes { get; set; }
    public bool UploadsCompletedSuccessfully { get; set; }
}