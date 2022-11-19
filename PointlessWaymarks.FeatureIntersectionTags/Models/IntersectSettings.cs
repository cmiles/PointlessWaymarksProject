using System.Text.Json.Serialization;

namespace PointlessWaymarks.FeatureIntersectionTags.Models;

public record IntersectSettings(
    [property: JsonPropertyName("intersectFiles")]
    IReadOnlyList<FeatureFile> IntersectFiles,
    [property: JsonPropertyName("padUsDirectory")]
    string PadUsDirectory,
    [property: JsonPropertyName("padUsAttributesForTags")]
    IReadOnlyList<string> PadUsAttributesForTags
);