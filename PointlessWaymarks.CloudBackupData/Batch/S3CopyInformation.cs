using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupData.Batch;

public record S3CopyInformation(
    S3FileSystemFileAndMetadataWithCloudKey LocalFile,
    S3RemoteFileAndMetadata ExistingRemoteFile);