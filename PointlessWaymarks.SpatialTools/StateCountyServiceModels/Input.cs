using System.Text.Json.Serialization;

namespace PointlessWaymarks.SpatialTools.StateCountyServiceModels;

public record Input(
    [property: JsonPropertyName("lat")] double Lat,
    [property: JsonPropertyName("lon")] double Lon,
    [property: JsonPropertyName("censusYear")]
    string CensusYear
);