namespace PointlessWaymarks.CommonTools.S3;

public class S3StandardMetadata
{
    public S3StandardMetadata(string lastWriteTime, string fileSystemHash, long fileSize)
    {
        LastWriteTime = lastWriteTime;
        FileSystemHash = fileSystemHash;
        FileSize = fileSize;
    }

    public long FileSize { get; init; }

    public string FileSystemHash { get; init; }

    public string LastWriteTime { get; init; }
}