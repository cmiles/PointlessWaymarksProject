using System.Text.Json.Serialization;

namespace PointlessWaymarks.CmsData.Spatial.Elevation
{
    public class ElevationLocation
    {
        [JsonPropertyName("lat")] public double Latitude { get; set; }

        [JsonPropertyName("lng")] public double Longitude { get; set; }
    }
}