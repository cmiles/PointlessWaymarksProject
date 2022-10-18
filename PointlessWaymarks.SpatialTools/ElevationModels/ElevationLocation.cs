using System.Text.Json.Serialization;

namespace PointlessWaymarks.SpatialTools.ElevationModels;

public class ElevationLocation
{
    [JsonPropertyName("lat")] public double Latitude { get; set; }

    [JsonPropertyName("lng")] public double Longitude { get; set; }
}