using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.PointDetailDataModels;

namespace PointlessWaymarks.CmsData.ContentHtml.PointHtml
{
    public static class PointData
    {
        public static async Task<string> JsonDataToString()
        {
            var db = await Db.Context();
            var allPointIds = await db.PointContents.Select(x => x.ContentId).ToListAsync();
            var extendedPointInformation = await Db.PointAndPointDetails(allPointIds, db);
            var settings = UserSettingsSingleton.CurrentSettings();

            return JsonSerializer.Serialize(extendedPointInformation.Select(x =>
                new
                {
                    x.ContentId,
                    x.Title,
                    x.Longitude,
                    x.Latitude,
                    x.Slug,
                    PointPageUrl = settings.PointPageUrl(x),
                    DetailTypeString = string.Join(", ", PointDetailUtilities.PointDtoTypeList(x))
                }).ToList());
        }

        public static async Task WriteJsonData()
        {
            var pointJson = await JsonDataToString();

            var dataFileInfo = new FileInfo($"{UserSettingsSingleton.CurrentSettings().LocalSitePointDataFile()}");

            if (dataFileInfo.Exists)
            {
                dataFileInfo.Delete();
                dataFileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLogAsync(dataFileInfo.FullName, pointJson);
        }
    }
}