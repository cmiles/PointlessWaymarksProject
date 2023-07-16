namespace PointlessWaymarks.CloudBackupData.Batch;

public class CloudUploadAndDeleteRunInformation
{
    public int DeleteCount { get; set; }
    public int DeleteErrorCount { get; set; }
    public DateTime Ended { get; set; }
    public bool EndedBecauseOfMaxRuntime { get; set; }
    public DateTime Started { get; set; }
    public int UploadCount { get; set; }
    public long UploadedSize { get; set; }
    public int UploadErrorCount { get; set; }
    public double UploadSeconds { get; set; }
}