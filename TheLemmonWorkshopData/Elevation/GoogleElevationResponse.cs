using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheLemmonWorkshopData.Elevation
{
    public class GoogleElevationResponse
    {
        [JsonPropertyName("results")]
        public List<GoogleElevationResult> Elevations { get; set; } = new List<GoogleElevationResult>();

        [JsonPropertyName("status")] public string Status { get; set; } = "";
    }
}