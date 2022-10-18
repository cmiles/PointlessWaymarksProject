using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.ContentHtml.LineHtml;

public static class LineData
{
    public static async Task<string> GenerateLineJson(string lineGeoJson, string title, string pageUrl)
    {
        var contentFeatureCollection = GeoJsonTools.DeserializeToFeatureCollection(lineGeoJson);

        var bounds = SpatialConverters.GeometryBoundingBox(SpatialConverters.GeoJsonToGeometries(lineGeoJson));

        var jsonDto = new LineSiteJsonData(pageUrl,
            new GeoJsonData.SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX),
            contentFeatureCollection);

        return await SpatialHelpers.SerializeWithGeoJsonSerializer(jsonDto);
    }

    public static async Task WriteJsonData(LineContent lineContent)
    {
        if (string.IsNullOrWhiteSpace(lineContent.Line))
            throw new ArgumentException(
                "WriteJsonData in LineData was given a LineContent with a null/blank/empty Line");

        var dataFileInfo = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalSiteLineDataDirectory().FullName,
            $"Line-{lineContent.ContentId}.json"));

        if (dataFileInfo.Exists)
        {
            dataFileInfo.Delete();
            dataFileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLogAsync(dataFileInfo.FullName,
                await GenerateLineJson(lineContent.Line, lineContent.Title ?? string.Empty,
                    UserSettingsSingleton.CurrentSettings().LinePageUrl(lineContent)).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    public record LineSiteJsonData(string PageUrl, GeoJsonData.SpatialBounds Bounds, FeatureCollection GeoJson);
}