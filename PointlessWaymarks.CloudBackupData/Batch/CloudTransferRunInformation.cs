namespace PointlessWaymarks.CloudBackupData.Batch;

public class CloudTransferRunInformation
{
    public long CopiedSize { get; set; }
    public int CopyCount { get; set; }
    public int CopyErrorCount { get; set; }
    public double CopySeconds { get; set; }
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