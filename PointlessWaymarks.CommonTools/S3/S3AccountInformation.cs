using Amazon;
using Amazon.Runtime;
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
    
    public required Func<string>? CloudflareAccountId { get; init; }
    public required Func<string> FullFileNameForJsonUploadInformation { get; init; }
    public required Func<string> FullFileNameForToExcel { get; init; }
    
    public AmazonS3Client S3Client()
    {
        var accessKey = AccessKey();
        var secret = Secret();
        
        if (S3Provider() == S3Providers.Cloudflare)
        {
            var credentials = new BasicAWSCredentials(accessKey, secret);
            var accountId = CloudflareAccountId();
            return new AmazonS3Client(credentials, new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com"
            });
        }
        
        return new AmazonS3Client(accessKey, secret, BucketRegionEndpoint());
    }
    
    public required Func<S3Providers> S3Provider { get; set; }
    public required Func<string> Secret { get; init; }
}