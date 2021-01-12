using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Spatial
{
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
            var serializer = GeoJsonSerializer.Create(GeometryFactory.Default, 3);

            using var stringReader = new StringReader(geoJson);
            using var jsonReader = new JsonTextReader(stringReader);
            return serializer.Deserialize<FeatureCollection>(jsonReader);
        }

        public static List<Geometry> GeoJsonToGeometries(string geoJson)
        {
            var serializer = GeoJsonSerializer.Create();

            using var stringReader = new StringReader(geoJson);
            using var jsonReader = new JsonTextReader(stringReader);
            var featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);

            return featureCollection.Select(x => SpatialHelpers.Wgs84GeometryFactory().CreateGeometry(x.Geometry))
                .ToList();
        }

        public static Envelope GeometryBoundingBox(LineContent content, Envelope? envelope = null)
        {
            var geometryList = LineContentToGeometries(content);
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
            if (string.IsNullOrWhiteSpace(content.Line)) return new List<Geometry>();

            var serializer = GeoJsonSerializer.Create(SpatialHelpers.Wgs84GeometryFactory(), 3);

            using var stringReader = new StringReader(content.Line);
            using var jsonReader = new JsonTextReader(stringReader);
            var featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);

            return featureCollection.Select(x => SpatialHelpers.Wgs84GeometryFactory().CreateGeometry(x.Geometry))
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
            return SpatialHelpers.Wgs84GeometryFactory()
                .CreatePoint(new CoordinateZ(content.Longitude, content.Latitude, content.Elevation ?? 0));
        }
    }
}