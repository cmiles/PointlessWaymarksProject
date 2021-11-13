using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.MapComponentData
{
    public static class MapData
    {
        public static async Task WriteJsonData(Guid mapComponentGuid)
        {
            await WriteJsonData(await Db.MapComponentDtoFromContentId(mapComponentGuid).ConfigureAwait(false)).ConfigureAwait(false);
        }

        public static async Task WriteJsonData(MapComponentDto dto)
        {
            var dtoElementGuids = dto.Elements.Select(x => x.ElementContentId).ToList();

            var db = await Db.Context().ConfigureAwait(false);
            var pointGuids = await db.PointContents.Where(x => dtoElementGuids.Contains(x.ContentId))
                .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
            var lineGuids = await db.LineContents.Where(x => dtoElementGuids.Contains(x.ContentId))
                .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
            var geoJsonGuids = await db.GeoJsonContents.Where(x => dtoElementGuids.Contains(x.ContentId))
                .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
            var showDetailsGuid = dto.Elements.Where(x => x.ShowDetailsDefault).Select(x => x.ElementContentId)
                .Distinct().ToList();

            var mapDtoJson = new MapSiteJsonData(dto.Map, geoJsonGuids, lineGuids, pointGuids, showDetailsGuid);

            var dataFileInfo = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSiteMapComponentDataDirectory().FullName,
                $"Map-{dto.Map.ContentId}.json"));

            if (dataFileInfo.Exists)
            {
                dataFileInfo.Delete();
                dataFileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLogAsync(dataFileInfo.FullName,
                JsonSerializer.Serialize(mapDtoJson)).ConfigureAwait(false);
        }

        public record MapSiteJsonData(MapComponent MapComponent, List<Guid> GeoJsonGuids, List<Guid> LineGuids,
            List<Guid> PointGuids, List<Guid> ShowDetailsGuid);
    }
}