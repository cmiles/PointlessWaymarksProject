using NetTopologySuite.Features;

namespace PointlessWaymarks.FeatureIntersectionTags;

public record IntersectResults(IFeature Feature, List<string> Tags);