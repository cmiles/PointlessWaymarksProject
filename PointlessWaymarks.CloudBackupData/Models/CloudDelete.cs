﻿namespace PointlessWaymarks.CloudBackupData.Models;

public class CloudDelete
{
    public virtual CloudTransferBatch? CloudTransferBatch { get; set; }
    public int CloudTransferBatchId { get; set; }
    public string BucketName { get; set; } = string.Empty;
    public string CloudObjectKey { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public bool DeletionCompletedSuccessfully { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    
    public int Id { get; set; }
    public DateTime LastUpdatedOn { get; set; }
}