namespace PointlessWaymarks.GeoTaggingService;

public interface IGpxService
{
    Task<List<WaypointAndSource>> GetGpxPoints(DateTime photoDateTimeUtc, IProgress<string>? progress);
}