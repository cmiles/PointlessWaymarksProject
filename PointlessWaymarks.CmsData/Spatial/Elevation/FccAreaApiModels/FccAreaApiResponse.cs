using System.Text.Json.Serialization;

namespace PointlessWaymarks.CmsData.Spatial.Elevation.FccAreaApiModels;

public record FccAreaApiResponse(
    [property: JsonPropertyName("input")] Input Input,
    [property: JsonPropertyName("results")]
    IReadOnlyList<Result> Results
);