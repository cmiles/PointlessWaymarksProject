using Amazon.S3.Transfer;

namespace PointlessWaymarks.CommonTools.S3;

public class S3UploadMetadata(string lastWriteTime, string fileSystemHash)
{
    public const string FileSystemHashKey = "x-amz-meta-filesystemhash";
    public const string LastWriteTimeKey = "x-amz-meta-lastwritetime";

    public string FileSystemHash { get; init; } = fileSystemHash;

    public string LastWriteTime { get; init; } = lastWriteTime;

    public TransferUtilityUploadRequest AddMetadataToRequest(TransferUtilityUploadRequest request)
    {
        request.Metadata.Add(LastWriteTimeKey, LastWriteTime);
        request.Metadata.Add(FileSystemHashKey, FileSystemHash);
        return request;
    }
}