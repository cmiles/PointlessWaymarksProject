using System.Text.Json.Serialization;

namespace PointlessWaymarks.CmsData.Spatial.Elevation
{
    public class ElevationResponse
    {
        [JsonPropertyName("results")] public List<ElevationResult> Elevations { get; set; } = new();

        [JsonPropertyName("status")] public string Status { get; set; } = "";
    }
}