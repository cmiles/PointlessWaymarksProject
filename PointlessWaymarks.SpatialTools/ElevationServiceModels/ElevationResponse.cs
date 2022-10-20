using System.Text.Json.Serialization;

namespace PointlessWaymarks.SpatialTools.ElevationServiceModels;

public class ElevationResponse
{
    [JsonPropertyName("results")] public List<ElevationResult> Elevations { get; set; } = new();

    [JsonPropertyName("status")] public string Status { get; set; } = "";
}