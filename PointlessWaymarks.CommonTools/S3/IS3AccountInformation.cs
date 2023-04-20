using Amazon;
using Amazon.S3;

namespace PointlessWaymarks.CommonTools.S3;

public interface IS3AccountInformation
{
    Func<string> AccessKey { get; }
    Func<string> BucketName { get; }
    Func<string> BucketRegion { get; }
    Func<string> FullFileNameForJsonUploadInformation { get; }
    Func<string> FullFileNameForToExcel { get; }
    Func<string> Secret { get; }
    RegionEndpoint? BucketRegionEndpoint();
    AmazonS3Client S3Client();
}