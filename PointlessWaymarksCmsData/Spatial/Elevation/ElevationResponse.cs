using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PointlessWaymarksCmsData.Spatial.Elevation
{
    public class ElevationResponse
    {
        [JsonPropertyName("results")]
        public List<ElevationResult> Elevations { get; set; } = new List<ElevationResult>();

        [JsonPropertyName("status")] public string Status { get; set; } = "";
    }
}