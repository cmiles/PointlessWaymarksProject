using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupTests;

public class S3LocalAccountInformation : IS3AccountInformation
{
    public Func<string> AccessKey { get; init; }
    public Func<string> BucketName => () => LocalS3Bucket;
    public Func<string> BucketRegion { get; init; }
    public Func<string> FullFileNameForJsonUploadInformation { get; init; }
    public Func<string> FullFileNameForToExcel { get; init; }
    public string LocalS3Bucket { get; set; }
    public Func<string> Secret { get; init; }

    public RegionEndpoint? BucketRegionEndpoint()
    {
        var bucketRegion = BucketRegion();
        return RegionEndpoint.EnumerableAllRegions.SingleOrDefault(x =>
            x.SystemName == bucketRegion);
    }

    public AmazonS3Client S3Client()
    {
        return new AmazonS3Client(new AnonymousAWSCredentials(), new AmazonS3Config
        {
            ServiceURL = "http://localhost:9090",
            ForcePathStyle = true
        });
    }
}