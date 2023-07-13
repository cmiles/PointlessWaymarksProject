using Amazon.S3.Transfer;

namespace PointlessWaymarks.CommonTools.S3;

public class S3Metadata
{
    public const string FileSystemHashKey = "x-amz-meta-filesystemhash";

    public const string LastWriteTimeKey = "x-amz-meta-lastwritetime";

    public S3Metadata(string lastWriteTime, string fileSystemHash)
    {
        LastWriteTime = lastWriteTime;
        FileSystemHash = fileSystemHash;
    }

    public string FileSystemHash { get; init; }

    public string LastWriteTime { get; init; }

    public TransferUtilityUploadRequest AddMetadataToRequest(TransferUtilityUploadRequest request)
    {
        request.Metadata.Add(LastWriteTimeKey, LastWriteTime);
        request.Metadata.Add(FileSystemHashKey, FileSystemHash);
        return request;
    }
}