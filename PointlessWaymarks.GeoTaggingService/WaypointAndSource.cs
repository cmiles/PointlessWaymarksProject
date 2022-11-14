using NetTopologySuite.IO;

namespace PointlessWaymarks.GeoTaggingService;

public record WaypointAndSource(GpxWaypoint Waypoint, string Source);