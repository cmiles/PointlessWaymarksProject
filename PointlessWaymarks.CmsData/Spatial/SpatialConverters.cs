using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.Spatial;

public static class SpatialConverters
{
    public static List<Geometry> GeoJsonContentToGeometries(GeoJsonContent content)
    {
        return string.IsNullOrWhiteSpace(content.GeoJson)
            ? new List<Geometry>()
            : GeoJsonToGeometries(content.GeoJson);
    }

    public static FeatureCollection GeoJsonToFeatureCollection(string geoJson)
    {
        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            GeometryFactory.Default, 3);

        using var stringReader = new StringReader(geoJson);
        using var jsonReader = new JsonTextReader(stringReader);
        return serializer.Deserialize<FeatureCollection>(jsonReader);
    }

    public static List<Geometry> GeoJsonToGeometries(string geoJson)
    {
        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            GeometryFactory.Default, 3);

        using var stringReader = new StringReader(geoJson);
        using var jsonReader = new JsonTextReader(stringReader);
        var featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);

        return featureCollection.Select(x => GeoJsonTools.Wgs84GeometryFactory().CreateGeometry(x.Geometry))
            .ToList();
    }

    public static Envelope GeometryBoundingBox(LineContent content, Envelope? envelope = null)
    {
        var geometryList = LineContentToGeometries(content);
        return GeometryBoundingBox(geometryList, envelope);
    }

    public static Envelope GeometryBoundingBox(string lineString, Envelope? envelope = null)
    {
        var geometryList = LineStringToGeometries(lineString);
        return GeometryBoundingBox(geometryList, envelope);
    }

    public static Envelope GeometryBoundingBox(GeoJsonContent content, Envelope? envelope = null)
    {
        var geometryList = GeoJsonContentToGeometries(content);
        return GeometryBoundingBox(geometryList, envelope);
    }

    public static Envelope GeometryBoundingBox(List<GeoJsonContent> content, Envelope? envelope = null)
    {
        var geometryList = content.SelectMany(GeoJsonContentToGeometries).ToList();
        return GeometryBoundingBox(geometryList, envelope);
    }

    public static Envelope GeometryBoundingBox(List<LineContent> content, Envelope? envelope = null)
    {
        var geometryList = content.SelectMany(LineContentToGeometries).ToList();
        return GeometryBoundingBox(geometryList, envelope);
    }

    public static Envelope GeometryBoundingBox(List<Geometry> geometries, Envelope? boundingBox = null)
    {
        boundingBox ??= new Envelope();
        foreach (var feature in geometries) boundingBox.ExpandToInclude(feature.EnvelopeInternal);

        return boundingBox;
    }

    public static List<Geometry> LineContentToGeometries(LineContent content)
    {
        return LineStringToGeometries(content.Line ?? string.Empty);
    }

    public static List<Geometry> LineStringToGeometries(string lineString)
    {
        if (string.IsNullOrWhiteSpace(lineString)) return new List<Geometry>();

        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            GeoJsonTools.Wgs84GeometryFactory(), 3);

        using var stringReader = new StringReader(lineString);
        using var jsonReader = new JsonTextReader(stringReader);
        var featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);

        return featureCollection.Select(x => GeoJsonTools.Wgs84GeometryFactory().CreateGeometry(x.Geometry))
            .ToList();
    }

    public static Envelope PointBoundingBox(List<PointContent> content, Envelope? envelope = null)
    {
        var pointList = content.Select(PointContentToPoint).ToList();
        return PointBoundingBox(pointList, envelope);
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