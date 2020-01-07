using System.Text.Json.Serialization;

namespace PointlessWaymarksCmsData.Elevation
{
    public class GoogleElevationResult
    {
        [JsonPropertyName("elevation")] public double Elevation { get; set; }

        [JsonPropertyName("location")] public GoogleLocation Location { get; set; }

        [JsonPropertyName("resolution")] public double Resolution { get; set; }
    }
}