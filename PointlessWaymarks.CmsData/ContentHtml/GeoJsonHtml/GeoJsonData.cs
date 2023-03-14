using NetTopologySuite.Features;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;

public static class GeoJsonData
{
    public static async Task<string?> GenerateGeoJson(string geoJsonContent, string pageUrl)
    {
        var contentFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(geoJsonContent);

        var bounds = GeoJsonTools.GeometryBoundingBox(GeoJsonTools.GeoJsonToGeometries(geoJsonContent));

        var jsonDto = new GeoJsonSiteJsonData(pageUrl,
            new SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), contentFeatureCollection);

        var jsonString = await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);

        jsonString = jsonString.Replace("{{self}}", pageUrl);

        await BracketCodeCommon.ProcessCodesForSite(jsonString).ConfigureAwait(false);

        return jsonString;
    }

    public static async Task WriteJsonData(GeoJsonContent geoJsonContent)
    {
        var dataFileInfo = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalSiteGeoJsonDataDirectory().FullName,
            $"GeoJson-{geoJsonContent.ContentId}.json"));

        if (dataFileInfo.Exists)
        {
            dataFileInfo.Delete();
            dataFileInfo.Refresh();
        }

        if (string.IsNullOrWhiteSpace(geoJsonContent.GeoJson))
        {
            var toThrow = new ArgumentException(
                $"GeoJson Content with Blank GeoJson Submitted to WriteJsonData, ContentId {geoJsonContent.ContentId}, Title {geoJsonContent.Title}");
            toThrow.Data.Add("ContentId", geoJsonContent.ContentId);
            toThrow.Data.Add("Title", geoJsonContent.Title);

            throw toThrow;
        }

        await FileManagement.WriteAllTextToFileAndLogAsync(dataFileInfo.FullName,
            (await GenerateGeoJson(geoJsonContent.GeoJson,
                UserSettingsSingleton.CurrentSettings().GeoJsonPageUrl(geoJsonContent)).ConfigureAwait(false))!).ConfigureAwait(false);
    }

    // ReSharper disable NotAccessedPositionalProperty.Global - Happy with Data Structures Here
    public record GeoJsonSiteJsonData(string PageUrl, SpatialBounds Bounds, FeatureCollection GeoJson);

    public record SpatialBounds(double InitialViewBoundsMaxLatitude, double InitialViewBoundsMaxLongitude,
        double InitialViewBoundsMinLatitude, double InitialViewBoundsMinLongitude);
    // ReSharper restore NotAccessedPositionalProperty.Global
}