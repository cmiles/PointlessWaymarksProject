using Flurl;
using Flurl.Http;
using PointlessWaymarks.CommonTools;
using SimMetricsCore;

namespace PointlessWaymarks.SpatialTools.GeoNames;

public static class GeoNamesSearch
{
    public static async Task<GeoNamesSearchReturn> Search(string search, string userName)
    {
        return await "https://secure.geonames.org".AppendPathSegment("searchJSON")
            .SetQueryParams(new
            {
                q = search,
                username = userName,
                style = "LONG"
            })
            .PostAsync()
            .ReceiveJson<GeoNamesSearchReturn>();
    }

    public static async Task<List<GeoNamesSimpleSearchResult>> SearchSimple(string search, string userName)
    {
        var rawResults = await Search(search, userName);

        if (!rawResults.geonames.Any()) return [];

        var orderedResultsList = rawResults.geonames
            .OrderByDescending(x => x.name.GetSimilarities(search.AsList(), SimMetricType.JaroWinkler).First().Score)
            .ToList();

        var returnList = new List<GeoNamesSimpleSearchResult>();

        foreach (var loopResult in orderedResultsList)
        {
            var titleItems = new List<string?>
                { loopResult.name ?? loopResult.toponymName ?? "(none)", loopResult.adminCode1, loopResult.countryCode };
            var descriptionItems = new List<string?>
                { loopResult.countryName, loopResult.adminName1, loopResult.fcodeName };
            returnList.Add(new GeoNamesSimpleSearchResult
            {
                Name = string.Join(", ", titleItems.Where(x => !string.IsNullOrWhiteSpace(x))),
                Description = string.Join(" - ", descriptionItems.Where(x => !string.IsNullOrWhiteSpace(x))),
                Latitude = double.Parse(loopResult.lat ?? "0"),
                Longitude = double.Parse(loopResult.lng ?? "0")
            });
        }

        return returnList;
    }
}