using System.Text.Json.Serialization;

namespace PointlessWaymarks.SpatialTools.ElevationServiceModels;

public class ElevationResult
{
    [JsonPropertyName("elevation")] public double? Elevation { get; set; }

    [JsonPropertyName("location")] public ElevationLocation? Location { get; set; }

    [JsonPropertyName("resolution")] public double Resolution { get; set; }
}