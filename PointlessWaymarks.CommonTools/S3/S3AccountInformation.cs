using Amazon;

namespace PointlessWaymarks.CommonTools.S3;

public class S3AccountInformation
{
    public required Func<string> AccessKey { get; init; }
    public required Func<string> BucketName { get; init; }
    public required Func<string> BucketRegion { get; init; }
    public required Func<string> FullFileNameForJsonUploadInformation { get; init; }
    public required Func<string> FullFileNameForToExcel { get; init; }
    public required Func<string> Secret { get; init; }

    public RegionEndpoint? BucketRegionEndpoint()
    {
        var bucketRegion = BucketRegion();
        return RegionEndpoint.EnumerableAllRegions.SingleOrDefault(x =>
            x.SystemName == bucketRegion);
    }
}