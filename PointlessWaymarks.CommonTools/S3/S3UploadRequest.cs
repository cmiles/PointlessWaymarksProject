using Amazon.S3.Transfer;

namespace PointlessWaymarks.CommonTools.S3;

/// <summary>
///     Holds file information for transfer to Amazon S3
/// </summary>
public class S3UploadRequest
{
    /// <summary>
    ///     Holds file information for transfer to Amazon S3
    /// </summary>
    /// <param name="toUpload"></param>
    /// <param name="s3Key"></param>
    /// <param name="bucketName"></param>
    /// <param name="serviceUrl"></param>
    /// <param name="note"></param>
    public S3UploadRequest(S3LocalFileAndMetadata toUpload, string s3Key, string bucketName, string serviceUrl, string note)
    {
        ToUpload = toUpload;
        S3Key = s3Key;
        BucketName = bucketName;
        ServiceUrl = serviceUrl;
        Note = note;
    }

    public string BucketName { get; }

    public string Note { get; }

    public string ServiceUrl { get; }

    public string S3Key { get; }

    public S3LocalFileAndMetadata ToUpload { get; }

    public TransferUtilityUploadRequest UploadRequest()
    {
        var uploadRequest = new TransferUtilityUploadRequest
        {
            BucketName = BucketName,
            FilePath = ToUpload.LocalFile.FullName,
            Key = S3Key
        };

        return ToUpload.UploadMetadata.AddMetadataToRequest(uploadRequest);
    }
}