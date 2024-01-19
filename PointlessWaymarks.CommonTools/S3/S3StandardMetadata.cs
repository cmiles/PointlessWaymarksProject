namespace PointlessWaymarks.CommonTools.S3;

public class S3StandardMetadata(string lastWriteTime, string fileSystemHash, long fileSize)
{
    public long FileSize { get; init; } = fileSize;

    public string FileSystemHash { get; init; } = fileSystemHash;

    public string LastWriteTime { get; init; } = lastWriteTime;
}