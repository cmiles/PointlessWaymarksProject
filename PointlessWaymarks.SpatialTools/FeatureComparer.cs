using NetTopologySuite.Features;

namespace PointlessWaymarks.SpatialTools;

public class FeatureComparer : IEqualityComparer<IFeature>
{
    public bool Equals(IFeature? x, IFeature? y)
    {
        if (x == null || y == null) return false;
        x.Geometry.Normalize();
        y.Geometry.Normalize();
        return x.Geometry.Equals(y.Geometry);
    }

    public int GetHashCode(IFeature obj)
    {
        return obj.GetHashCode();
    }
}