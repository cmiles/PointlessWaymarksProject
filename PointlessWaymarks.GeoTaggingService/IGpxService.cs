namespace PointlessWaymarks.GeoTaggingService;

public interface IGpxService
{
    Task<List<WaypointAndSource>> GetGpxPoints(List<DateTime> photoDateTimeUtcList, IProgress<string>? progress);
}