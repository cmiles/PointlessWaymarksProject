using Amazon;

namespace PointlessWaymarks.WpfCommon.S3Uploads;

public class S3Information
{
    public required Func<string> AccessKey { get; set; }
    public required Func<string> BucketName { get; set; }
    public required Func<string> BucketRegion { get; set; }
    public required Func<string> FullFileNameForJsonUploadInformation { get; set; }
    public required Func<string> FullFileNameForToExcel { get; set; }
    public required Func<string> Secret { get; set; }

    public RegionEndpoint? BucketRegionEndpoint()
    {
        var bucketRegion = BucketRegion();
        return RegionEndpoint.EnumerableAllRegions.SingleOrDefault(x =>
            x.SystemName == bucketRegion);
    }
}