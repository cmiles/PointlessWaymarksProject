using System.Text.Json.Serialization;

namespace PointlessWaymarks.FeatureIntersectionTags.Models;

public record FeatureFile(
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("attributesForTags")]
    IReadOnlyList<string> AttributesForTags,
    [property: JsonPropertyName("tagAll")] string TagAll,
    [property: JsonPropertyName("fileName")]
    string FileName
);