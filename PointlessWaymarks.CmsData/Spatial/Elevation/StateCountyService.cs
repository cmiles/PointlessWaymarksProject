using System.Text.Json;
using PointlessWaymarks.CmsData.Spatial.Elevation.FccAreaApiModels;

namespace PointlessWaymarks.CmsData.Spatial.Elevation;

public static class StateCountyService
{
    private static readonly HttpClient StateCountyHttpClient = new();

    public static async Task<(string state, string county)> GetStateCounty(double latitude, double longitude)
    {
        var requestUrl =
            $"https://geo.fcc.gov/api/census/area?lat={latitude}&lon={longitude}&censusYear=2020&format=json";

        var deserializedResponse =
            await JsonSerializer.DeserializeAsync<FccAreaApiResponse>(
                await StateCountyHttpClient.GetStreamAsync(requestUrl));

        return (deserializedResponse?.Results.FirstOrDefault()?.StateName ?? string.Empty,
            deserializedResponse?.Results.FirstOrDefault()?.CountyName ?? string.Empty);
    }
}