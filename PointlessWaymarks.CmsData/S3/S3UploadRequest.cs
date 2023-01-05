namespace PointlessWaymarks.CmsData.S3;

/// <summary>
/// Holds file information for transfer to Amazon S3
/// </summary>
/// <param name="ToUpload"></param>
/// <param name="S3Key"></param>
/// <param name="BucketName"></param>
/// <param name="Region"></param>
/// <param name="Note"></param>
public record S3UploadRequest(FileInfo ToUpload, string S3Key, string BucketName, string Region, string Note);