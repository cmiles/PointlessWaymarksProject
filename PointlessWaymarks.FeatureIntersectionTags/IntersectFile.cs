using System.Text.Json.Serialization;

namespace PointlessWaymarks.FeatureIntersectionTags;

public record IntersectFile(
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("attributesForTags")]
    IReadOnlyList<string> AttributesForTags,
    [property: JsonPropertyName("tagAll")] string TagAll,
    [property: JsonPropertyName("downloaded")]
    string Downloaded,
    [property: JsonPropertyName("fileName")]
    string FileName
);