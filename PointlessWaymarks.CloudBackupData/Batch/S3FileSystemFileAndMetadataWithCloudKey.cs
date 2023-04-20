﻿using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupData.Batch;

public record S3FileSystemFileAndMetadataWithCloudKey(FileInfo LocalFile, S3Metadata Metadata, string CloudKey);