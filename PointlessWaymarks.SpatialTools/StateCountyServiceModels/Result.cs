using System.Text.Json.Serialization;

// ReSharper disable StringLiteralTypo - names from Api
// ReSharper disable IdentifierTypo

namespace PointlessWaymarks.SpatialTools.StateCountyServiceModels;

public record Result(
    [property: JsonPropertyName("block_fips")]
    string BlockFips,
    [property: JsonPropertyName("bbox")] IReadOnlyList<double> Bbox,
    [property: JsonPropertyName("county_fips")]
    string CountyFips,
    [property: JsonPropertyName("county_name")]
    string CountyName,
    [property: JsonPropertyName("state_fips")]
    string StateFips,
    [property: JsonPropertyName("state_code")]
    string StateCode,
    [property: JsonPropertyName("state_name")]
    string StateName,
    [property: JsonPropertyName("block_pop_2020")]
    int BlockPop2020,
    [property: JsonPropertyName("amt")] string Amt,
    [property: JsonPropertyName("bea")] string Bea,
    [property: JsonPropertyName("bta")] string Bta,
    [property: JsonPropertyName("cma")] string Cma,
    [property: JsonPropertyName("eag")] string Eag,
    [property: JsonPropertyName("ivm")] string Ivm,
    [property: JsonPropertyName("mea")] string Mea,
    [property: JsonPropertyName("mta")] string Mta,
    [property: JsonPropertyName("pea")] string Pea,
    [property: JsonPropertyName("rea")] string Rea,
    [property: JsonPropertyName("rpc")] string Rpc,
    [property: JsonPropertyName("vpc")] string Vpc
);