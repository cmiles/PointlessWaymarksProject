﻿using System.IO;

namespace PointlessWaymarks.CmsWpfControls.S3Uploads;

public record S3Upload(FileInfo ToUpload, string S3Key, string BucketName, string Region, string Note);

public record S3UploadFileRecord(string FileFullName, string S3Key, string BucketName, string Region, string Note);