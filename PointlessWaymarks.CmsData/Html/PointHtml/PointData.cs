using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.PointDetailDataModels;

namespace PointlessWaymarks.CmsData.Html.PointHtml
{
    public static class PointData
    {
        public static async Task WriteJsonData()
        {
            var db = await Db.Context();
            var allPointIds = await db.PointContents.Select(x => x.ContentId).ToListAsync();
            var extendedPointInformation = await Db.PointAndPointDetails(allPointIds, db);
            var settings = UserSettingsSingleton.CurrentSettings();

            var pointJson = JsonSerializer.Serialize(extendedPointInformation.Select(x =>
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

            var dataFileInfo = new FileInfo($"{settings.LocalSitePointDataFile()}");

            if (dataFileInfo.Exists)
            {
                dataFileInfo.Delete();
                dataFileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLogAsync(dataFileInfo.FullName, pointJson);
        }
    }
}