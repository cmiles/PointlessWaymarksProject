using NetTopologySuite.Features;

namespace PointlessWaymarks.FeatureIntersectionTags.Models;

public record IntersectResults
{
    public IntersectResults(IFeature feature)
    {
        Feature = feature;
        
    }

    public readonly IFeature Feature;
    public List<string> Sources = new();
    public List<string> Tags = new();
    public List<IFeature> IntersectsWith = new();
}