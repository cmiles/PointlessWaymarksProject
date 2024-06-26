﻿using NetTopologySuite.Features;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.FeatureIntersectionTags.Models;

public class IntersectResult
{
    public IntersectResult(IFeature feature)
    {
        Features = feature.AsList();
    }

    public IntersectResult(List<IFeature> features)
    {
        Features = features;
    }

    public Guid ContentId { get; init; } = Guid.Empty;
    public List<IFeature> Features { get; }
    public List<IFeature> IntersectsWith { get; } = [];
    public List<string> Sources { get; } = [];
    public List<string> Tags { get; } = [];
}