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

namespace PointlessWaymarksCmsData.Html.MapComponentData
{
    public static class MapData
    {
        public static async Task WriteLocalJsonData(MapComponentDto dto)
        {
            var dtoElementGuids = dto.Elements.Select(x => x.ElementContentId).ToList();

            var db = await Db.Context();
            var pointGuids = await db.PointContents.Where(x => dtoElementGuids.Contains(x.ContentId))
                .Select(x => x.ContentId).ToListAsync();
            var lineGuids = await db.LineContents.Where(x => dtoElementGuids.Contains(x.ContentId))
                .Select(x => x.ContentId).ToListAsync();
            var geoJsonGuids = await db.GeoJsonContents.Where(x => dtoElementGuids.Contains(x.ContentId))
                .Select(x => x.ContentId).ToListAsync();

            var mapDtoJson = new MapSiteJsonData(dto.Map, geoJsonGuids, lineGuids, pointGuids);

            var dataFileInfo = new FileInfo("Map-(dto.Map.ContentId)");

            if (dataFileInfo.Exists)
            {
                dataFileInfo.Delete();
                dataFileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLogAsync(dataFileInfo.FullName,
                JsonSerializer.Serialize(mapDtoJson));
        }

        public record MapSiteJsonData(MapComponent MapComponentData, List<Guid> GeoJsonGuids, List<Guid> LineGuids,
            List<Guid> PointGuids);
    }
}