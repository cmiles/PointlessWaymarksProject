using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Database.PointDetailDataModels;

namespace PointlessWaymarksCmsData.Html.PointHtml
{
    public partial class PointMap
    {
        public DateTime? GenerationVersion { get; set; }

        public string PointJson { get; set; }

        public async Task WriteLocalJavascript()
        {
            var db = await Db.Context();
            var allPointIds = await db.PointContents.Select(x => x.ContentId).ToListAsync();
            var extendedPointInformation = await Db.PointAndPointDetails(allPointIds, db);
            var settings = UserSettingsSingleton.CurrentSettings();

            PointJson = JsonSerializer.Serialize(extendedPointInformation.Select(x =>
                new
                {
                    x.Title,
                    x.Longitude,
                    x.Latitude,
                    x.Slug,
                    PointPageUrl = settings.PointPageUrl(x),
                    DetailTypeString = string.Join(", ", PointDetailUtilities.PointDtoTypeList(x))
                }).ToList());

            var htmlFileInfo = new FileInfo($"{settings.LocalSitePointMapJavascriptFile()}");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLogAsync(htmlFileInfo.FullName, TransformText());
        }
    }
}