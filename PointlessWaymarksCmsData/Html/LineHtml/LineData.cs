using System.IO;
using System.Threading.Tasks;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.GeoJsonHtml;
using PointlessWaymarksCmsData.Spatial;

namespace PointlessWaymarksCmsData.Html.LineHtml
{
    public static class LineData
    {
        public static async Task WriteLocalJsonData(LineContent geoJsonContent)
        {
            var serializer = GeoJsonSerializer.Create(SpatialHelpers.Wgs84GeometryFactory(), 3);

            using var stringReader = new StringReader(geoJsonContent.Line);
            using var jsonReader = new JsonTextReader(stringReader);
            var contentFeatureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);

            var jsonDto = new LineSiteJsonData(UserSettingsSingleton.CurrentSettings().LinePageUrl(geoJsonContent),
                new GeoJsonData.SpatialBounds(geoJsonContent.InitialViewBoundsMaxLatitude, geoJsonContent.InitialViewBoundsMaxLongitude,
                    geoJsonContent.InitialViewBoundsMinLatitude, geoJsonContent.InitialViewBoundsMinLongitude), contentFeatureCollection);

            var dataFileInfo = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSiteLineDataDirectory().FullName,
                $"Line-{geoJsonContent.ContentId}.json"));

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

        public record LineSiteJsonData(string PageUrl, GeoJsonData.SpatialBounds Bounds, FeatureCollection GeoJson);
    }
}