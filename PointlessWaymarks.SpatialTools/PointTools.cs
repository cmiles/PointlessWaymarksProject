using NetTopologySuite.Geometries;

namespace PointlessWaymarks.SpatialTools;

public static class PointTools
{
    public static Point Wgs84Point(double x, double y, double z)
    {
        return GeoJsonTools.Wgs84GeometryFactory().CreatePoint(new CoordinateZ(x, y, z));
    }

    public static Point Wgs84Point(double x, double y)
    {
        return GeoJsonTools.Wgs84GeometryFactory().CreatePoint(new Coordinate(x, y));
    }
}