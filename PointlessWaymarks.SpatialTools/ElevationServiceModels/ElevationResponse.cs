using System.Text.Json.Serialization;

namespace PointlessWaymarks.SpatialTools.ElevationServiceModels;

public class ElevationResponse
{
    [JsonPropertyName("results")] public List<ElevationResult> Elevations { get; set; } = [];

    [JsonPropertyName("status")] public string Status { get; set; } = "";
}