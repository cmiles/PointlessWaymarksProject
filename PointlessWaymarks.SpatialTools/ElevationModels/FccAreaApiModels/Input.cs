using System.Text.Json.Serialization;

namespace PointlessWaymarks.SpatialTools.ElevationModels.FccAreaApiModels;

public record Input(
    [property: JsonPropertyName("lat")] double Lat,
    [property: JsonPropertyName("lon")] double Lon,
    [property: JsonPropertyName("censusYear")]
    string CensusYear
);