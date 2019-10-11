using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace TheLemmonWorkshopData
{
    public static class SpatialHelpers
    {
        public static LineString Wgs84FactoryLineString() => Wgs84GeometryFactory().CreateLineString();

        public static Point Wgs84FactoryPoint() => Wgs84GeometryFactory().CreatePoint();

        public static GeometryFactory Wgs84GeometryFactory() =>
                            NtsGeometryServices.Instance.CreateGeometryFactory(new PrecisionModel(), srid: 4326,
                NetTopologySuite.Geometries.Implementation.DotSpatialAffineCoordinateSequenceFactory.Instance);

        public static Point Wgs84Point(double x, double y, double z)
        {
            var point = Wgs84FactoryPoint();
            point.X = x;
            point.Y = y;
            point.Z = z;

            return point;
        }
    }
}