using NetTopologySuite.Features;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;

public static class GeoJsonData
{
    public static async Task<string> GenerateGeoJson(string geoJsonContent, string pageUrl)
    {
        var contentFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(geoJsonContent);

        var bounds = GeoJsonTools.GeometryBoundingBox(GeoJsonTools.GeoJsonToGeometries(geoJsonContent));

        var jsonDto = new GeoJsonSiteJsonData(pageUrl,
            SpatialBounds.FromEnvelope(bounds), contentFeatureCollection);

        var jsonString = await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);

        jsonString = jsonString.Replace("{{self}}", pageUrl);

        await BracketCodeCommon.ProcessCodesForSite(jsonString).ConfigureAwait(false);

        return jsonString;
    }

    // ReSharper disable NotAccessedPositionalProperty.Global - Happy with Data Structures Here
    public record GeoJsonSiteJsonData(string PageUrl, SpatialBounds Bounds, FeatureCollection GeoJson);
    // ReSharper restore NotAccessedPositionalProperty.Global
}