using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupData.Batch;

public record FileListAndChangeData
{
    public required IS3AccountInformation AccountInformation { get; set; }
    public required bool ChangesBasedOnNewCloudFileScan { get; set; }
    public List<S3FileSystemFileAndMetadataWithCloudKey> FileSystemFiles { get; set; } = [];
    public List<S3FileSystemFileAndMetadataWithCloudKey> FileSystemFilesToUpload { get; set; } = [];
    public required BackupJob Job { get; set; }
    public List<S3RemoteFileAndMetadata> S3Files { get; set; } = [];
    public List<S3CopyInformation> S3FilesToCopy { get; set; } = [];
    public List<S3RemoteFileAndMetadata> S3FilesToDelete { get; set; } = [];
}