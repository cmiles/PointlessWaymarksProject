using System.Text.Json.Serialization;

namespace PointlessWaymarks.FeatureIntersectionTags.Models;

public record IntersectSettings(
    [property: JsonPropertyName("intersectFiles")]
    IReadOnlyList<IntersectFile> IntersectFiles,
    [property: JsonPropertyName("padUsDoiRegionFile")]
    string PadUsDoiRegionFile,
    [property: JsonPropertyName("padUsDirectory")]
    string PadUsDirectory,
    [property: JsonPropertyName("padUsFilePrefix")]
    string PadUsFilePrefix,
    [property: JsonPropertyName("padUsAttributesForTags")]
    IReadOnlyList<string> PadUsAttributesForTags
);