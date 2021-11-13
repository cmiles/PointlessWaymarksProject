using System.Text.Json.Serialization;

namespace PointlessWaymarks.CmsData.Spatial.Elevation;

public class ElevationResult
{
    [JsonPropertyName("elevation")] public double? Elevation { get; set; }

    [JsonPropertyName("location")] public ElevationLocation? Location { get; set; }

    [JsonPropertyName("resolution")] public double Resolution { get; set; }
}