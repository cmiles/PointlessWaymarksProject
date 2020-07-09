using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;

namespace PointlessWaymarksCmsData.Spatial
{
    public static class SpatialHelpers
    {
        public static GeometryFactory Wgs84GeometryFactory()
        {
            return NtsGeometryServices.Instance.CreateGeometryFactory(new PrecisionModel(), 4326,
                DotSpatialAffineCoordinateSequenceFactory.Instance);
        }

        public static Point Wgs84Point(double x, double y, double z)
        {
            return Wgs84GeometryFactory().CreatePoint(new CoordinateZ(x, y, z));
        }
    }
}