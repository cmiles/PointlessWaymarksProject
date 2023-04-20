using Amazon;
using Amazon.S3;

namespace PointlessWaymarks.CommonTools.S3;

public class S3AccountInformation : IS3AccountInformation
{
    public required Func<string> AccessKey { get; init; }
    public required Func<string> BucketName { get; init; }
    public required Func<string> BucketRegion { get; init; }

    public RegionEndpoint? BucketRegionEndpoint()
    {
        var bucketRegion = BucketRegion();
        return RegionEndpoint.EnumerableAllRegions.SingleOrDefault(x =>
            x.SystemName == bucketRegion);
    }

    public required Func<string> FullFileNameForJsonUploadInformation { get; init; }
    public required Func<string> FullFileNameForToExcel { get; init; }

    public AmazonS3Client S3Client()
    {
        var accessKey = AccessKey();
        var secret = Secret();

        return new AmazonS3Client(accessKey, secret, BucketRegionEndpoint());
    }

    public required Func<string> Secret { get; init; }
}