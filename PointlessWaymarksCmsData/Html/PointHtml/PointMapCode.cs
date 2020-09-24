using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Database;

namespace PointlessWaymarksCmsData.Html.PointHtml
{
    public partial class PointMap
    {
        public async Task WriteLocalJavascript()
        {
            var db = await Db.Context();
            var allPointContent = await db.PointContents.AsNoTracking().ToListAsync();
            var settings = UserSettingsSingleton.CurrentSettings();

            PointJson = JsonSerializer.Serialize(allPointContent.Select(x =>
                new
                {
                    x.Title,
                    x.Longitude,
                    x.Latitude,
                    x.Slug,
                    PointPageUrl = settings.PointPageUrl(x)
                }).ToList());

            var htmlFileInfo =
                new FileInfo(
                    $"{settings.LocalSitePointMapJavascriptFile()}");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            await File.WriteAllTextAsync(htmlFileInfo.FullName, TransformText());
        }

        public string PointJson { get; set; }
    }
}
