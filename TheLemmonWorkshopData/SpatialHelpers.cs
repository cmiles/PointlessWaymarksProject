using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace TheLemmonWorkshopData
{
    public static class SpatialHelpers
    {
        public static GeometryFactory Wgs84GeometryFactory() =>
                            NtsGeometryServices.Instance.CreateGeometryFactory(new PrecisionModel(), srid: 4326,
                NetTopologySuite.Geometries.Implementation.DotSpatialAffineCoordinateSequenceFactory.Instance);

        public static Point Wgs84Point(double x, double y, double z) =>  Wgs84GeometryFactory().CreatePoint(new CoordinateZ(x, y, z));
    }
}