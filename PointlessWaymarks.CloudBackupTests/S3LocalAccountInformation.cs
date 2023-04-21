using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupTests;

public class S3LocalAccountInformation : IS3AccountInformation
{
    public Func<string> AccessKey { get; init; }
    public Func<string> BucketName => () => LocalStackBucket;
    public Func<string> BucketRegion { get; init; }

    public RegionEndpoint? BucketRegionEndpoint()
    {
        var bucketRegion = BucketRegion();
        return RegionEndpoint.EnumerableAllRegions.SingleOrDefault(x =>
            x.SystemName == bucketRegion);
    }

    public Func<string> FullFileNameForJsonUploadInformation { get; init; }
    public Func<string> FullFileNameForToExcel { get; init; }

    public AmazonS3Client S3Client()
    {
        return new AmazonS3Client(new AnonymousAWSCredentials(), new AmazonS3Config
        {
            ServiceURL = "http://localhost:9090",
            ForcePathStyle = true
        });
    }

    public string LocalStackBucket { get; set; }

    public Func<string> Secret { get; init; }
}