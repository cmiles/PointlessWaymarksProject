using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PointlessWaymarksCmsData.Spatial.Elevation
{
    public class GoogleElevationResponse
    {
        [JsonPropertyName("results")]
        public List<GoogleElevationResult> Elevations { get; set; } = new List<GoogleElevationResult>();

        [JsonPropertyName("status")] public string Status { get; set; } = "";
    }
}