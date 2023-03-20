using System.Text.Json;
using PointlessWaymarks.SpatialTools.StateCountyServiceModels;
using Serilog;

namespace PointlessWaymarks.SpatialTools;

public static class StateCountyService
{
    private static readonly HttpClient StateCountyHttpClient = new();

    public static async Task<(string state, string county)> GetStateCounty(double latitude, double longitude)
    {
        var requestUrl =
            $"https://geo.fcc.gov/api/census/area?lat={latitude}&lon={longitude}&censusYear=2020&format=json";

        FccAreaApiResponse? deserializedResponse = null;

        try
        {
            deserializedResponse = await JsonSerializer.DeserializeAsync<FccAreaApiResponse>(
                await StateCountyHttpClient.GetStreamAsync(requestUrl));
        }
        catch (Exception e)
        {
            Log.Error(e, "Ignored Exception - Call failed to the FCC Area Api");
        }


        return (deserializedResponse?.Results.FirstOrDefault()?.StateName ?? string.Empty,
            deserializedResponse?.Results.FirstOrDefault()?.CountyName ?? string.Empty);
    }
}