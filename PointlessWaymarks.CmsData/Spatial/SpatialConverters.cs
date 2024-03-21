using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.Spatial;

public static class SpatialConverters
{
    public static List<Geometry> GeoJsonContentToGeometries(GeoJsonContent content)
    {
        return string.IsNullOrWhiteSpace(content.GeoJson)
            ? []
            : GeoJsonTools.GeoJsonToGeometries(content.GeoJson);
    }


    public static Envelope GeometryBoundingBox(LineContent content, Envelope? envelope = null)
    {
        var geometryList = LineContentToGeometries(content);
        return GeoJsonTools.GeometryBoundingBox(geometryList, envelope);
    }



    public static Envelope GeometryBoundingBox(GeoJsonContent content, Envelope? envelope = null)
    {
        var geometryList = GeoJsonContentToGeometries(content);
        return GeoJsonTools.GeometryBoundingBox(geometryList, envelope);
    }

    public static Envelope GeometryBoundingBox(List<GeoJsonContent> content, Envelope? envelope = null)
    {
        var geometryList = content.SelectMany(GeoJsonContentToGeometries).ToList();
        return GeoJsonTools.GeometryBoundingBox(geometryList, envelope);
    }

    public static Envelope GeometryBoundingBox(List<LineContent> content, Envelope? envelope = null)
    {
        var geometryList = content.SelectMany(LineContentToGeometries).ToList();
        return GeoJsonTools.GeometryBoundingBox(geometryList, envelope);
    }


    public static List<Geometry> LineContentToGeometries(LineContent content)
    {
        return GeoJsonTools.LineStringToGeometries(content.Line ?? string.Empty);
    }

    public static Envelope PointBoundingBox(List<PointContent> content, Envelope? envelope = null)
    {
        var pointList = content.Select(PointContentToPoint).ToList();
        return PointBoundingBox(pointList, envelope);
    }

    public static Envelope PhotoBoundingBox(List<PhotoContent> content, Envelope? envelope = null)
    {
        var photoPointList = content.Where(x => x.HasLocation()).Select(x => GeoJsonTools.Wgs84GeometryFactory()
            .CreatePoint(new CoordinateZ(x.Longitude.Value, x.Latitude.Value, x.Elevation ?? 0))).ToList();

        return PointBoundingBox(photoPointList, envelope);
    }

    public static Envelope PointBoundingBox(List<Point> points, Envelope? boundingBox = null)
    {
        boundingBox ??= new Envelope();
        foreach (var feature in points) boundingBox.ExpandToInclude(feature.EnvelopeInternal);

        return boundingBox;
    }

    public static Point PointContentToPoint(PointContent content)
    {
        return GeoJsonTools.Wgs84GeometryFactory()
            .CreatePoint(new CoordinateZ(content.Longitude, content.Latitude, content.Elevation ?? 0));
    }

}