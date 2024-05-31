namespace PointlessWaymarks.CommonTools.S3;

/// <summary>
///     Data Structure to record a S3 Upload
/// </summary>
/// <param name="FileFullName"></param>
/// <param name="S3Key"></param>
/// <param name="BucketName"></param>
/// <param name="ServiceUrl"></param>
/// <param name="Note"></param>
public record S3UploadFileEntry(string FileFullName, string S3Key, string BucketName, string ServiceUrl, string Note);