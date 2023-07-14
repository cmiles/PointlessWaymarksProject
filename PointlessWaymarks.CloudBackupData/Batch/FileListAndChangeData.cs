using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupData.Batch;

public record FileListAndChangeData
{
    public required IS3AccountInformation AccountInformation { get; set; }
    public List<S3FileSystemFileAndMetadataWithCloudKey> FileSystemFiles { get; set; } = new();
    public List<S3FileSystemFileAndMetadataWithCloudKey> FileSystemFilesToUpload { get; set; } = new();
    public required BackupJob Job { get; set; }
    public List<S3RemoteFileAndMetadata> S3Files { get; set; } = new();
    public List<S3RemoteFileAndMetadata> S3FilesToDelete { get; set; } = new();
}