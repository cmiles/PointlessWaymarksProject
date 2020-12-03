using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Spatial
{
    public static class SpatialConverters
    {
        public static GeometryFactory DefaultGeometryFactory => new GeometryFactory(new PrecisionModel(), 4326);

        public static List<Geometry> GeoJsonContentToGeometries(GeoJsonContent content)
        {
            var serializer = GeoJsonSerializer.Create();

            using var stringReader = new StringReader(content.GeoJson);
            using var jsonReader = new JsonTextReader(stringReader);
            var featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);

            return featureCollection.Select(x => DefaultGeometryFactory.CreateGeometry(x.Geometry)).ToList();
        }

        public static Envelope GeometryBoundingBox(GeoJsonContent content, Envelope envelope = null)
        {
            var geometryList = GeoJsonContentToGeometries(content);
            return GeometryBoundingBox(geometryList, envelope);
        }

        public static Envelope GeometryBoundingBox(List<GeoJsonContent> content, Envelope envelope = null)
        {
            var geometryList = content.SelectMany(GeoJsonContentToGeometries).ToList();
            return GeometryBoundingBox(geometryList, envelope);
        }

        public static Envelope GeometryBoundingBox(List<Geometry> geometries, Envelope boundingBox = null)
        {
            boundingBox ??= new Envelope();
            foreach (var feature in geometries) boundingBox.ExpandToInclude(feature.EnvelopeInternal);

            return boundingBox;
        }

        public static Envelope PointBoundingBox(List<PointContent> content, Envelope envelope = null)
        {
            var pointList = content.Select(PointContentToPoint).ToList();
            return PointBoundingBox(pointList, envelope);
        }

        public static Envelope PointBoundingBox(List<Point> points, Envelope boundingBox = null)
        {
            boundingBox ??= new Envelope();
            foreach (var feature in points) boundingBox.ExpandToInclude(feature.EnvelopeInternal);

            return boundingBox;
        }

        public static Point PointContentToPoint(PointContent content)
        {
            return DefaultGeometryFactory.CreatePoint(new CoordinateZ(content.Longitude, content.Latitude,
                content.Elevation ?? 0));
        }
    }
}