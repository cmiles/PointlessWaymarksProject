using System.Text.Json.Serialization;

namespace PointlessWaymarks.SpatialTools.StateCountyServiceModels;

public record FccAreaApiResponse(
    [property: JsonPropertyName("input")] Input Input,
    [property: JsonPropertyName("results")]
    IReadOnlyList<Result> Results
);