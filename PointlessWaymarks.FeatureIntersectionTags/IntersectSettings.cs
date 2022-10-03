using System.Text.Json.Serialization;

namespace PointlessWaymarks.FeatureIntersectionTags;

public record IntersectSettings(
    [property: JsonPropertyName("intersectFiles")]
    IReadOnlyList<IntersectFile> IntersectFiles,
    [property: JsonPropertyName("intersectFilesDirectory")]
    string IntersectFilesDirectory
);