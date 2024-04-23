using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupTests;

public class S3LocalAccountInformation : IS3AccountInformation
{
    public Func<string> AccessKey { get; init; } = () => string.Empty;
    public Func<string> BucketName => () => LocalS3Bucket;
    public Func<string> BucketRegion { get; init; } = () => "us-west-1";
    public Func<string> FullFileNameForJsonUploadInformation { get; init; } = () => "CloudTestUpload.json";
    public Func<string> FullFileNameForToExcel { get; init; } = () => "CloudTestExcel.xlsx";
    public string LocalS3Bucket { get; set; }
    public Func<string> Secret { get; init; } = () => string.Empty;

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