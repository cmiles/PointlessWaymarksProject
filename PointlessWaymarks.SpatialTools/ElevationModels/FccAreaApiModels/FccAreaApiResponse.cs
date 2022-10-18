using System.Text.Json.Serialization;

namespace PointlessWaymarks.SpatialTools.ElevationModels.FccAreaApiModels;

public record FccAreaApiResponse(
    [property: JsonPropertyName("input")] Input Input,
    [property: JsonPropertyName("results")]
    IReadOnlyList<Result> Results
);