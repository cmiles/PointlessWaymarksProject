using NetTopologySuite.Features;

namespace PointlessWaymarks.FeatureIntersectionTags.Models;

public record IntersectResults(IFeature Feature, List<string> Tags);