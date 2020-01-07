using System.Text.Json.Serialization;

namespace PointlessWaymarksCmsData.Elevation
{
    public class GoogleLocation
    {
        [JsonPropertyName("lat")] public double Latitude { get; set; }

        [JsonPropertyName("lng")] public double Longitude { get; set; }
    }
}