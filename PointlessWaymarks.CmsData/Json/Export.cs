using System.Text.Json;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Json;

public static class Export
{
    public static async Task WriteLinkListJson(IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var db = await Db.Context();
        var allContent = await db.LinkContents.OrderByDescending(x => x.CreatedOn).ToListAsync();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteLinkDirectory().FullName,
            $"{Names.LinkListFileName}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<List<LinkContent>>(jsonFileStream);

            if (new CompareLogic().Compare(allContent, onDiskObject).AreEqual)
            {
                progress?.Report("Link List - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report("Link List - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(allContent);

        await FileManagement.WriteAllTextToFileAndLog(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report("Link List - Serializing and Writing Historic Entries");

        var latestHistoricEntries = await db.HistoricLinkContents.ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Link List Entries");

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
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteFileContentDirectory(dbEntry).FullName,
            $"{Names.FileContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<FileContent>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject).AreEqual)
            {
                progress?.Report($"File - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"File - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"File - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricFileContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

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

    public static async Task WriteLocalDbJson(VideoContent dbEntry, IProgress<string>? progress = null)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteVideoContentDirectory(dbEntry).FullName,
            $"{Names.VideoContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<VideoContent>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject).AreEqual)
            {
                progress?.Report($"Video - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Video - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Video - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricVideoContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Video Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteVideoContentDirectory(dbEntry).FullName,
            $"{Names.HistoricVideoContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(PostContent dbEntry, IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePostContentDirectory(dbEntry).FullName,
            $"{Names.PostContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<PostContent>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject).AreEqual)
            {
                progress?.Report($"Post - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Post - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Post - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricPostContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Post Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePostContentDirectory(dbEntry).FullName,
            $"{Names.HistoricPostContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(LineContent dbEntry, IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteLineContentDirectory(dbEntry).FullName,
            $"{Names.LineContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<LineContent>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject).AreEqual)
            {
                progress?.Report($"Line - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Line - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Line - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        //Other content types preserve more history - these content types can be quite large due to the
        //GeoJson content so less history is kept.
        var latestHistoricEntries = await db.HistoricLineContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(2).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Line Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteLineContentDirectory(dbEntry).FullName,
            $"{Names.HistoricLineContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);

        progress?.Report($"Line - {dbEntry.Title} - Done with Json Serialization");
    }

    public static async Task WriteLocalDbJson(GeoJsonContent dbEntry, IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteGeoJsonContentDirectory(dbEntry).FullName,
            $"{Names.GeoJsonContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<GeoJsonContent>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject).AreEqual)
            {
                progress?.Report($"GeoJson - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"GeoJson - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"GeoJson - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        //Other content types preserve more history - these content types can be quite large due to the
        //GeoJson content so less history is kept.
        var latestHistoricEntries = await db.HistoricGeoJsonContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(2).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic GeoJson Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(
            settings.LocalSiteGeoJsonContentDirectory(dbEntry).FullName,
            $"{Names.HistoricGeoJsonContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);

        progress?.Report($"GeoJson - {dbEntry.Title} - Done with Json Serialization");
    }

    public static async Task WriteLocalDbJson(MapComponent dbEntry, IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var dtoToArchive = await Db.MapComponentDtoFromContentId(dbEntry.ContentId);

        //This top process archives just the current MapComponent rather than the DTO since the 
        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteMapComponentDataDirectory().FullName,
            $"{Names.MapComponentContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<MapComponentDto>(jsonFileStream);

            if (new CompareLogic().Compare(dtoToArchive, onDiskObject).AreEqual)
            {
                progress?.Report(
                    $"MapComponent - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"MapComponent - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(dtoToArchive, new JsonSerializerOptions { WriteIndented = true });

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"MapComponent - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricMapComponents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Map Component Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteMapComponentDataDirectory().FullName,
            $"{Names.HistoricMapComponentContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(PointContent dbEntry, IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var db = await Db.Context().ConfigureAwait(false);

        var dtoToArchive = await Db.PointContentDtoFromPoint(dbEntry, db);

        //This top process archives just the current Point rather than the DTO since the 
        var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePointDataDirectory().FullName,
            $"{Names.PointContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<PointContentDto>(jsonFileStream);

            if (new CompareLogic().Compare(dtoToArchive, onDiskObject).AreEqual)
            {
                progress?.Report($"Point - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Point - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(dtoToArchive, new JsonSerializerOptions { WriteIndented = true });

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Point - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var latestHistoricEntries = await db.HistoricPointContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Map Component Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePointDataDirectory().FullName,
            $"{Names.HistoricPointContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(NoteContent dbEntry, IProgress<string>? progress = null)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteNoteContentDirectory(dbEntry).FullName,
            $"{Names.NoteContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<NoteContent>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject).AreEqual)
            {
                progress?.Report($"Note - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Note - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Note - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricNoteContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Note Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteNoteContentDirectory(dbEntry).FullName,
            $"{Names.HistoricNoteContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(ImageContent dbEntry, IProgress<string>? progress = null)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteImageContentDirectory(dbEntry).FullName,
            $"{Names.ImageContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<ImageContent>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject).AreEqual)
            {
                progress?.Report($"Image - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Image - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Image - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricImageContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Image Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteImageContentDirectory(dbEntry).FullName,
            $"{Names.HistoricImageContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLocalDbJson(PhotoContent dbEntry, IProgress<string>? progress = null)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePhotoContentDirectory(dbEntry).FullName,
            $"{Names.PhotoContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<PhotoContent>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject).AreEqual)
            {
                progress?.Report($"Photo - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Photo - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(dbEntry, new JsonSerializerOptions { WriteIndented = true });

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Photo - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricPhotoContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Photo Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePhotoContentDirectory(dbEntry).FullName,
            $"{Names.HistoricPhotoContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteMenuLinksJson(IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var db = await Db.Context();
        var allContent = await db.MenuLinks.OrderByDescending(x => x.MenuOrder).ToListAsync();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteDirectory().FullName,
            $"{Names.MenuLinksFileName}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<List<MenuLink>>(jsonFileStream);

            if (new CompareLogic().Compare(allContent, onDiskObject).AreEqual)
            {
                progress?.Report("Menu Link Json - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report("Menu Link Json - Serializing and Writing Current Entries");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(allContent);

        await FileManagement.WriteAllTextToFileAndLog(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);
    }

    public static async Task WriteTagExclusionsJson(IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var db = await Db.Context();
        var allContent = await db.TagExclusions.OrderByDescending(x => x.Tag).ToListAsync();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteTagsDirectory().FullName,
            $"{Names.TagExclusionsFileName}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<List<TagExclusion>>(jsonFileStream);

            if (new CompareLogic().Compare(allContent, onDiskObject).AreEqual)
            {
                progress?.Report("Tag Exclusions Json - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report("Tag Exclusions Json - Serializing and Writing Current Entries");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var jsonDbEntry = JsonSerializer.Serialize(allContent);

        await FileManagement.WriteAllTextToFileAndLog(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);
    }
}