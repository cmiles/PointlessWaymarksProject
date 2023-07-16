namespace PointlessWaymarks.CommonTools.S3;

public record S3RemoteFileAndMetadata(string Bucket, string Key, S3StandardMetadata Metadata);