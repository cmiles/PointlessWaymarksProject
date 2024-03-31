using Flurl;
using Flurl.Http;
using PointlessWaymarks.CommonTools;
using SimMetricsCore;

namespace PointlessWaymarks.SpatialTools.GeoNames;

public static class GeoNamesSearch
{
    public static async Task<GeoNamesSearchReturn> Search(string search, string userName, string continentCode,
        string countryBias)
    {
        return await "https://secure.geonames.org".AppendPathSegment("searchJSON")
            .SetQueryParams(new
            {
                q = search,
                username = userName,
                style = "LONG",
            })
            .PostAsync()
            .ReceiveJson<GeoNamesSearchReturn>();
    }

    public static async Task<List<GeoNamesSimpleSearchEntry>> SearchSimple(string search, string userName,
        string continentCode,
        string countryBias)
    {
        var rawResults = await Search(search, userName, continentCode, countryBias);

        if (!rawResults.geonames.Any()) return [];

        return rawResults.geonames
            .OrderByDescending(x => x.name.GetSimilarities(search.AsList(), SimMetricType.JaroWinkler).First().Score)
            .Select(x => new GeoNamesSimpleSearchEntry
            {
                Name = x.name ?? x.toponymName ?? "(none)",
                Description = $"{x.countryName} {x.adminName1} {x.fcodeName}",
                Latitude = double.Parse(x.lat),
                Longitude = double.Parse(x.lng)
            }).ToList();
    }
}