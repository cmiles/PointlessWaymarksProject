using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;

namespace PointlessWaymarks.CmsData.Content;

public static class MapIconGenerator
{
    public static async Task GenerateMapIconsFile()
    {
        var mapIconsFile = UserSettingsSingleton.CurrentSettings().LocalSiteMapIconsDataFile();

        if (mapIconsFile.Exists)
        {
            mapIconsFile.Delete();
            mapIconsFile.Refresh();
        }

        var serializedIcons = await SerializedMapIcons();

        await FileManagement.WriteAllTextToFileAndLogAsync(mapIconsFile.FullName, serializedIcons)
            .ConfigureAwait(false);
    }

    public static async Task<string> SerializedMapIcons()
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allMapIcons = await db.MapIcons.OrderBy(x => x.IconName).ToListAsync();

        return JsonSerializer.Serialize(allMapIcons, new JsonSerializerOptions()
        {
            WriteIndented = true
        });
    }
}