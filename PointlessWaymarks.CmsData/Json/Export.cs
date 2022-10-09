using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Json;

public static class Export
{
    public static async Task WriteLinkListJson()
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var db = Db.Context().Result;
        var allContent = db.LinkContents.OrderByDescending(x => x.CreatedOn).ToList();

        var jsonDbEntry = JsonSerializer.Serialize(allContent);

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteLinkDirectory().FullName,
            $"{Names.LinkListFileName}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLog(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        var latestHistoricEntries = db.HistoricLinkContents.ToList();

        if (!latestHistoricEntries.Any()) return;

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteLinkDirectory().FullName,
            $"{Names.HistoricLinkListFileName}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLog(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(FileContent dbEntry, IProgress<string>? progress = null)
    {
        progress?.Report("Writing Db Entry to Json");

        var settings = UserSettingsSingleton.CurrentSettings();
        var db = await Db.Context().ConfigureAwait(false);
        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteFileContentDirectory(dbEntry).FullName,
            $"{Names.FileContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report("Writing Historic Db Entries to Json");

        var latestHistoricEntries = db.HistoricFileContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToList();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic File Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteFileContentDirectory(dbEntry).FullName,
            $"{Names.HistoricFileContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(PostContent dbEntry)
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        var db = await Db.Context().ConfigureAwait(false);
        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePostContentDirectory(dbEntry).FullName,
            $"{Names.PostContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        var latestHistoricEntries = db.HistoricPostContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10);

        if (!latestHistoricEntries.Any()) return;

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePostContentDirectory(dbEntry).FullName,
            $"{Names.HistoricPostContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(LineContent dbEntry)
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        var db = await Db.Context().ConfigureAwait(false);
        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteLineContentDirectory(dbEntry).FullName,
            $"{Names.LineContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        //Other content types preserve more history - these content types can be quite large due to the
        //GeoJson content so less history is kept.
        var latestHistoricEntries = db.HistoricLineContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(2);

        if (!latestHistoricEntries.Any()) return;

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteLineContentDirectory(dbEntry).FullName,
            $"{Names.HistoricLineContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(GeoJsonContent dbEntry)
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        var db = await Db.Context().ConfigureAwait(false);
        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteGeoJsonContentDirectory(dbEntry).FullName,
            $"{Names.GeoJsonContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        //Other content types preserve more history - these content types can be quite large due to the
        //GeoJson content so less history is kept.
        var latestHistoricEntries = db.HistoricGeoJsonContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(2);

        if (!latestHistoricEntries.Any()) return;

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(
            settings.LocalSiteGeoJsonContentDirectory(dbEntry).FullName,
            $"{Names.HistoricGeoJsonContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(MapComponent dbEntry)
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        var db = await Db.Context().ConfigureAwait(false);
        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteMapComponentDataDirectory().FullName,
            $"{Names.MapComponentContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        var latestHistoricEntries = db.HistoricMapComponents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10);

        var toArchive = new List<HistoricMapComponentDto>();

        foreach (var loopHistoricEntries in latestHistoricEntries)
        {
            var historicElements = await db.HistoricMapComponentElements.Where(x =>
                x.MapComponentContentId == loopHistoricEntries.ContentId &&
                x.LastUpdateOn == loopHistoricEntries.LastUpdatedOn).ToListAsync().ConfigureAwait(false);

            toArchive.Add(new HistoricMapComponentDto(loopHistoricEntries, historicElements));
        }

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteMapComponentDataDirectory().FullName,
            $"{Names.HistoricMapComponentContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName,
            JsonSerializer.Serialize(toArchive)).ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(PointContent dbEntry)
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        var db = await Db.Context().ConfigureAwait(false);
        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePointContentDirectory(dbEntry).FullName,
            $"{Names.PointContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        var latestHistoricEntries = db.HistoricPointContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePointContentDirectory(dbEntry).FullName,
            $"{Names.HistoricPointContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        if (latestHistoricEntries.Any())
        {
            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);
            await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
                .ConfigureAwait(false);
        }

        var pointDetailsFile = new FileInfo(Path.Combine(settings.LocalSitePointContentDirectory(dbEntry).FullName,
            $"{Names.PointDetailsContentPrefix}{dbEntry.ContentId}.json"));

        if (pointDetailsFile.Exists) pointDetailsFile.Delete();
        pointDetailsFile.Refresh();

        var pointDetails = await Db.PointDetailsForPoint(dbEntry.ContentId, db).ConfigureAwait(false);

        if (pointDetails.Any())
        {
            var jsonPointDetails = JsonSerializer.Serialize(pointDetails);
            await FileManagement.WriteAllTextToFileAndLogAsync(pointDetailsFile.FullName, jsonPointDetails)
                .ConfigureAwait(false);
        }

        var historicPointDetailsFile = new FileInfo(Path.Combine(
            settings.LocalSitePointContentDirectory(dbEntry).FullName,
            $"{Names.HistoricPointDetailsContentPrefix}{dbEntry.ContentId}.json"));

        if (historicPointDetailsFile.Exists) historicPointDetailsFile.Delete();
        historicPointDetailsFile.Refresh();

        var historicPointDetails =
            await Db.HistoricPointDetailsForPoint(dbEntry.ContentId, db, 40).ConfigureAwait(false);

        if (historicPointDetails.Any())
        {
            var jsonPointDetails = JsonSerializer.Serialize(historicPointDetails);
            await FileManagement.WriteAllTextToFileAndLogAsync(historicPointDetailsFile.FullName, jsonPointDetails)
                .ConfigureAwait(false);
        }
    }

    public static async Task WriteLocalDbJson(NoteContent dbEntry, IProgress<string>? progress = null)
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        var db = await Db.Context().ConfigureAwait(false);
        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteNoteContentDirectory(dbEntry).FullName,
            $"{Names.NoteContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        var latestHistoricEntries = db.HistoricNoteContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10);

        if (!latestHistoricEntries.Any()) return;

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteNoteContentDirectory(dbEntry).FullName,
            $"{Names.HistoricNoteContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(ImageContent dbEntry)
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        var db = await Db.Context().ConfigureAwait(false);
        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteImageContentDirectory(dbEntry).FullName,
            $"{Names.ImageContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        var latestHistoricEntries = db.HistoricImageContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10);

        if (!latestHistoricEntries.Any()) return;

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteImageContentDirectory(dbEntry).FullName,
            $"{Names.HistoricImageContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(PhotoContent dbEntry)
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        var db = await Db.Context().ConfigureAwait(false);
        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePhotoContentDirectory(dbEntry).FullName,
            $"{Names.PhotoContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        var latestHistoricEntries = db.HistoricPhotoContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10);

        if (!latestHistoricEntries.Any()) return;

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePhotoContentDirectory(dbEntry).FullName,
            $"{Names.HistoricPhotoContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteMenuLinksJson()
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var db = Db.Context().Result;
        var allContent = db.MenuLinks.OrderByDescending(x => x.MenuOrder).ToList();

        var jsonDbEntry = JsonSerializer.Serialize(allContent);

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteDirectory().FullName,
            $"{Names.MenuLinksFileName}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLog(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);
    }

    public static async Task WriteTagExclusionsJson()
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var db = Db.Context().Result;
        var allContent = db.TagExclusions.OrderByDescending(x => x.Tag).ToList();

        var jsonDbEntry = JsonSerializer.Serialize(allContent);

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteTagsDirectory().FullName,
            $"{Names.TagExclusionsFileName}.json"));

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLog(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);
    }
}