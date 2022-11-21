using NetTopologySuite.Features;
using PointlessWaymarks.LoggingTools;

namespace PointlessWaymarks.FeatureIntersectionTags.Models;

public class IntersectResults
{
    public IntersectResults(IFeature feature)
    {
        Features = feature.AsList();
    }

    public IntersectResults(List<IFeature> features)
    {
        Features = features;
    }

    public Guid ContentId { get; set; }= Guid.Empty;
    public List<IFeature> Features { get; }
    public List<string> Sources { get; } = new();
    public List<string> Tags { get; } = new();
    public List<IFeature> IntersectsWith { get; } = new();
}