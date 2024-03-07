using System.Text.Json;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.Json;

public static class Export
{
    public static async Task WriteLinkListJson(IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var db = await Db.Context();
        var allContent = await db.LinkContents.OrderByDescending(x => x.CreatedOn).ToListAsync();

        var jsonFile = settings.LocalSiteLinkListJsonFile();

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
            $"{UserSettingsUtilities.HistoricLinkListFileName}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLog(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteFileContentData(FileContent dbEntry, IProgress<string>? progress = null)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteContentDataDirectory().FullName,
            $"{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<FileContentOnDiskData>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject?.Content).AreEqual)
            {
                progress?.Report($"File - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"File - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var onDiskData = new FileContentOnDiskData(Db.ContentTypeDisplayString(dbEntry), dbEntry);

        var jsonDbEntry = JsonSerializer.Serialize(onDiskData, JsonTools.WriteIndentedOptions);

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"File - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricFileContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic File Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteFileContentDirectory(dbEntry).FullName,
            $"{UserSettingsUtilities.HistoricFileContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteVideoContentData(VideoContent dbEntry, IProgress<string>? progress = null)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteContentDataDirectory().FullName,
            $"{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<VideoContentOnDiskData>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject?.Content).AreEqual)
            {
                progress?.Report($"Video - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Video - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var onDiskData = new VideoContentOnDiskData(Db.ContentTypeDisplayString(dbEntry), dbEntry);

        var jsonDbEntry = JsonSerializer.Serialize(onDiskData, JsonTools.WriteIndentedOptions);

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Video - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricVideoContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Video Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteVideoContentDirectory(dbEntry).FullName,
            $"{UserSettingsUtilities.HistoricVideoContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WritePostContentData(PostContent dbEntry, IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteContentDataDirectory().FullName,
            $"{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<PostContentOnDiskData>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject?.Content).AreEqual)
            {
                progress?.Report($"Post - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Post - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var onDiskData = new PostContentOnDiskData(Db.ContentTypeDisplayString(dbEntry), dbEntry);

        var jsonDbEntry = JsonSerializer.Serialize(onDiskData, JsonTools.WriteIndentedOptions);

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Post - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricPostContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Post Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePostContentDirectory(dbEntry).FullName,
            $"{UserSettingsUtilities.HistoricPostContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteLineContentData(LineContent dbEntry, IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteContentDataDirectory().FullName,
            $"{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<LineContentOnDiskData>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject?.Content).AreEqual)
            {
                progress?.Report($"Line - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Line - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var onDiskData = new LineContentOnDiskData(Db.ContentTypeDisplayString(dbEntry), dbEntry, LineData.GenerateLineElevationDataList(dbEntry), await LineData.SpatialContentIdReferencesFromBodyContentReferences(dbEntry));

        var jsonDbEntry = await GeoJsonTools.SerializeWithGeoJsonSerializer(onDiskData);

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
            $"{UserSettingsUtilities.HistoricLineContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);

        progress?.Report($"Line - {dbEntry.Title} - Done with Json Serialization");
    }

    public static async Task WriteGeoJsonContentData(GeoJsonContent dbEntry, IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteContentDataDirectory().FullName,
            $"{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<GeoJsonContentOnDiskData>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject?.Content).AreEqual)
            {
                progress?.Report($"GeoJson - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"GeoJson - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var onDiskData = new GeoJsonContentOnDiskData(Db.ContentTypeDisplayString(dbEntry), dbEntry);

        var jsonDbEntry = JsonSerializer.Serialize(onDiskData, JsonTools.WriteIndentedOptions);

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
            $"{UserSettingsUtilities.HistoricGeoJsonContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);

        progress?.Report($"GeoJson - {dbEntry.Title} - Done with Json Serialization");
    }

    public static async Task WriteMapComponentContentData(MapComponentDto dbEntry, IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteContentDataDirectory().FullName,
            $"{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<MapComponentOnDiskData>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject?.Content).AreEqual)
            {
                progress?.Report(
                    $"MapComponent - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"MapComponent - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var dtoElementGuids = dbEntry.Elements.Select(x => x.ElementContentId).ToList();

        var db = await Db.Context().ConfigureAwait(false);
        var pointGuids = await db.PointContents.Where(x => dtoElementGuids.Contains(x.ContentId)).OrderBy(x => x.Title)
            .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
        var lineGuids = await db.LineContents.Where(x => dtoElementGuids.Contains(x.ContentId)).OrderBy(x => x.Title)
            .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
        var geoJsonGuids = await db.GeoJsonContents.Where(x => dtoElementGuids.Contains(x.ContentId))
            .OrderBy(x => x.Title)
            .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
        var photoGuids = await db.PhotoContents.Where(x => dtoElementGuids.Contains(x.ContentId)).OrderBy(x => x.Title)
            .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
        var showDetailsGuids = dbEntry.Elements.Where(x => x.ShowDetailsDefault).Select(x => x.ElementContentId)
            .Distinct().ToList();

        var onDiskData = new MapComponentOnDiskData(Db.ContentTypeDisplayString(dbEntry), dbEntry, new SpatialContentIdReferences(pointGuids, lineGuids, geoJsonGuids, photoGuids), showDetailsGuids);

        var jsonDbEntry = JsonSerializer.Serialize(onDiskData, JsonTools.WriteIndentedOptions);

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"MapComponent - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var latestHistoricEntries = await db.HistoricMapComponents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Map Component Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteMapComponentDataDirectory().FullName,
            $"{UserSettingsUtilities.HistoricMapComponentContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WritePointContentData(PointContent dbEntry, IProgress<string>? progress)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var db = await Db.Context().ConfigureAwait(false);

        var dtoToArchive = await Db.PointContentDtoFromPoint(dbEntry, db);

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteContentDataDirectory().FullName,
            $"{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<PointContentOnDiskData>(jsonFileStream);

            if (new CompareLogic().Compare(dtoToArchive, onDiskObject?.Content).AreEqual)
            {
                progress?.Report($"Point - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Point - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var onDiskData = new PointContentOnDiskData(Db.ContentTypeDisplayString(dbEntry), dtoToArchive);

        var jsonDbEntry = JsonSerializer.Serialize(onDiskData, JsonTools.WriteIndentedOptions);

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Point - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var latestHistoricEntries = await db.HistoricPointContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Map Component Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePointContentDirectory(dtoToArchive).FullName,
            $"{UserSettingsUtilities.HistoricPointContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteNoteContentData(NoteContent dbEntry, IProgress<string>? progress = null)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteContentDataDirectory().FullName,
            $"{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<NoteContentOnDiskData>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject?.Content).AreEqual)
            {
                progress?.Report($"Note - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Note - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var onDiskData = new NoteContentOnDiskData(Db.ContentTypeDisplayString(dbEntry), dbEntry);

        var jsonDbEntry = JsonSerializer.Serialize(onDiskData, JsonTools.WriteIndentedOptions);

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Note - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricNoteContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Note Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteNoteContentDirectory(dbEntry).FullName,
            $"{UserSettingsUtilities.HistoricNoteContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WriteImageContentData(ImageContent dbEntry, IProgress<string>? progress = null)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteContentDataDirectory().FullName,
            $"{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<ImageContentOnDiskData>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject?.Content).AreEqual)
            {
                progress?.Report($"Image - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Image - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var pictureInfo = PictureAssetProcessing.ProcessPictureDirectory(dbEntry.ContentId);
        var smallImageUrl = pictureInfo?.SmallPicture?.SiteUrl;
        var displayImageUrl = pictureInfo?.DisplayPicture?.SiteUrl;

        var onDiskData = new ImageContentOnDiskData(Db.ContentTypeDisplayString(dbEntry), dbEntry, smallImageUrl,
            displayImageUrl);

        var jsonDbEntry = JsonSerializer.Serialize(onDiskData, JsonTools.WriteIndentedOptions);

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Image - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricImageContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Image Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteImageContentDirectory(dbEntry).FullName,
            $"{UserSettingsUtilities.HistoricImageContentPrefix}{dbEntry.ContentId}.json"));

        if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
        jsonHistoricFile.Refresh();

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry)
            .ConfigureAwait(false);
    }

    public static async Task WritePhotoContentData(PhotoContent dbEntry, IProgress<string>? progress = null)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteContentDataDirectory().FullName,
            $"{dbEntry.ContentId}.json"));

        if (jsonFile.Exists)
        {
            await using var jsonFileStream = jsonFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var onDiskObject = await JsonSerializer.DeserializeAsync<PhotoContentOnDiskData>(jsonFileStream);

            if (new CompareLogic().Compare(dbEntry, onDiskObject?.Content).AreEqual)
            {
                progress?.Report($"Photo - {dbEntry.Title} - Current and On Disk Json are the same - continuing");
                return;
            }
        }

        progress?.Report($"Photo - {dbEntry.Title} - Serializing and Writing Current Entry");

        if (jsonFile.Exists) jsonFile.Delete();
        jsonFile.Refresh();

        var pictureInfo = PictureAssetProcessing.ProcessPictureDirectory(dbEntry.ContentId);
        var smallImageUrl = pictureInfo?.SmallPicture?.SiteUrl;
        var displayImageUrl = pictureInfo?.DisplayPicture?.SiteUrl;

        var onDiskData = new PhotoContentOnDiskData(Db.ContentTypeDisplayString(dbEntry), dbEntry, smallImageUrl,
            displayImageUrl);

        var jsonDbEntry = JsonSerializer.Serialize(onDiskData, JsonTools.WriteIndentedOptions);

        await FileManagement.WriteAllTextToFileAndLogAsync(jsonFile.FullName, jsonDbEntry).ConfigureAwait(false);

        progress?.Report($"Photo - {dbEntry.Title} - Serializing and Writing Historic Entries");

        var db = await Db.Context().ConfigureAwait(false);

        var latestHistoricEntries = await db.HistoricPhotoContents.Where(x => x.ContentId == dbEntry.ContentId)
            .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToListAsync();

        if (!latestHistoricEntries.Any()) return;

        progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic Photo Content Entries");

        var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

        var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePhotoContentDirectory(dbEntry).FullName,
            $"{UserSettingsUtilities.HistoricPhotoContentPrefix}{dbEntry.ContentId}.json"));

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

        var jsonFile = settings.LocalSiteMenuLinksJsonFile();

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

        var jsonFile = settings.LocalSiteTagExclusionsJsonFile();

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