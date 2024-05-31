using Amazon;
using Amazon.S3;

namespace PointlessWaymarks.CommonTools.S3;

public interface IS3AccountInformation
{
    Func<string> AccessKey { get; }
    Func<string> BucketName { get; }
    Func<string> ServiceUrl { get; init; }
    Func<string> FullFileNameForJsonUploadInformation { get; }
    Func<string> FullFileNameForToExcel { get; }
    Func<S3Providers> S3Provider { get; set; }
    Func<string> Secret { get; }
    AmazonS3Client S3Client();
}