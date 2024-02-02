namespace PointlessWaymarks.SpatialTools;

public record LineElevationChartDataPoint(
    double AccumulatedDistance,
    double? Elevation,
    double AccumulatedClimb,
    double AccumulatedDescent,
    double Latitude,
    double Longitude);