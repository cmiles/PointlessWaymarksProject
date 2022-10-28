using NetTopologySuite.IO;

namespace PointlessWaymarks.GeoTaggingService;

public interface IGpxService
{
    Task<List<GpxWaypoint>> GetGpxTrack(DateTime photoDateTimeUtc);
}