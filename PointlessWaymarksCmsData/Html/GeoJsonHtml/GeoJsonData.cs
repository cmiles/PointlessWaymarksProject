using System.IO;
using System.Threading.Tasks;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Html.GeoJsonHtml
{
    public static class GeoJsonData
    {
        public static async Task WriteLocalJsonData(GeoJsonContent geoJsonContent)
        {
            var serializer = GeoJsonSerializer.Create();

            using var stringReader = new StringReader(geoJsonContent.GeoJson);
            using var jsonReader = new JsonTextReader(stringReader);
            var contentFeatureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);

            var jsonDto =
                new GeoJsonSiteJsonData(UserSettingsSingleton.CurrentSettings().GeoJsonPageUrl(geoJsonContent), new SpatialBounds(geoJsonContent.InitialViewBoundsMaxX, geoJsonContent.InitialViewBoundsMaxY, geoJsonContent.InitialViewBoundsMinX, geoJsonContent.InitialViewBoundsMinY),
                    contentFeatureCollection);

            var dataFileInfo = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSiteGeoJsonDataDirectory().FullName,
                $"GeoJson-{geoJsonContent.ContentId}.json"));

            if (dataFileInfo.Exists)
            {
                dataFileInfo.Delete();
                dataFileInfo.Refresh();
            }

            await using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            serializer.Serialize(jsonWriter, jsonDto);

            await FileManagement.WriteAllTextToFileAndLogAsync(dataFileInfo.FullName, stringWriter.ToString());
        }

        public record GeoJsonSiteJsonData(string PageUrl, SpatialBounds Bounds, FeatureCollection GeoJson);

        public record SpatialBounds(double InitialViewBoundsMaxX, double InitialViewBoundsMaxY,
            double InitialViewBoundsMinX, double InitialViewBoundsMinY);
    }
}