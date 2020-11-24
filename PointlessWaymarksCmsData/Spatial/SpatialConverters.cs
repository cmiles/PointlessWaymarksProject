using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Spatial
{
    public static class SpatialConverters
    {
        public static GeometryFactory DefaultGeometryFactory => new GeometryFactory(new PrecisionModel(), 4326);

        public static Point PointContentToPoint(PointContent content)
        {
            return DefaultGeometryFactory.CreatePoint(new CoordinateZ(content.Longitude, content.Latitude,
                content.Elevation ?? 0));
        }

        public static Envelope PointBoundingBox(List<PointContent> content)
        {
            var pointList = content.Select(PointContentToPoint).ToList();
            return PointBoundingBox(pointList);
        }

        public static Envelope PointBoundingBox(List<Point> points)
        {
            var boundingBox = new Envelope();
            foreach (var feature in points)
            {
                boundingBox.ExpandToInclude(feature.EnvelopeInternal);
            }

            return boundingBox;
        }
    }
}
