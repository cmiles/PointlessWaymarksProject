using System.Text.Json.Serialization;

namespace PointlessWaymarks.SpatialTools.ElevationServiceModels;

public class ElevationLocation
{
    [JsonPropertyName("lat")] public double Latitude { get; set; }

    [JsonPropertyName("lng")] public double Longitude { get; set; }
}