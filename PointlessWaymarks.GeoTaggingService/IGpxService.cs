using NetTopologySuite.IO;

namespace PointlessWaymarks.GeoTaggingService;

public interface IGpxService
{
    Task<List<GpxWaypoint>> GetGpxPoints(DateTime photoDateTimeUtc, IProgress<string>? progress);
}