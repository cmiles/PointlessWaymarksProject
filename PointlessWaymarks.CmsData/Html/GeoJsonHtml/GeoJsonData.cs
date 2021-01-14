using System;
using System.IO;
using System.Threading.Tasks;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.CommonHtml;
using PointlessWaymarks.CmsData.Spatial;

namespace PointlessWaymarks.CmsData.Html.GeoJsonHtml
{
    public static class GeoJsonData
    {
        public static async Task<string> GenerateGeoJson(string geoJsonContent, string pageUrl)
        {
            var serializer = GeoJsonSerializer.Create(SpatialHelpers.Wgs84GeometryFactory());

            using var stringReader = new StringReader(geoJsonContent);
            using var jsonReader = new JsonTextReader(stringReader);
            var contentFeatureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);

            var bounds = SpatialConverters.GeometryBoundingBox(SpatialConverters.GeoJsonToGeometries(geoJsonContent));

            var jsonDto = new GeoJsonSiteJsonData(pageUrl,
                new SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), contentFeatureCollection);

            await using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            serializer.Serialize(jsonWriter, jsonDto);

            var jsonString = stringWriter.ToString();

            jsonString = jsonString.Replace("[[self]]", pageUrl);

            BracketCodeCommon.ProcessCodesForSite(jsonString);

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
                await GenerateGeoJson(geoJsonContent.GeoJson,
                    UserSettingsSingleton.CurrentSettings().GeoJsonPageUrl(geoJsonContent)));
        }

        public record GeoJsonSiteJsonData(string PageUrl, SpatialBounds Bounds, FeatureCollection GeoJson);

        public record SpatialBounds(double InitialViewBoundsMaxLatitude, double InitialViewBoundsMaxLongitude,
            double InitialViewBoundsMinLatitude, double InitialViewBoundsMinLongitude);
    }
}