using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Json
{
    public static class Export
    {
        public static void WriteLinkListJson()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var db = Db.Context().Result;
            var allContent = db.LinkContents.OrderByDescending(x => x.CreatedOn).ToList();

            var jsonDbEntry = JsonSerializer.Serialize(allContent);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteLinkDirectory().FullName,
                $"{Names.LinkListFileName}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricLinkContents.ToList();

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteLinkDirectory().FullName,
                $"{Names.HistoricLinkListFileName}.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public static async Task WriteLocalDbJson(FileContent dbEntry, IProgress<string> progress)
        {
            progress?.Report("Writing Db Entry to Json");

            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(dbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteFileContentDirectory(dbEntry).FullName,
                $"{Names.FileContentPrefix}{dbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            await File.WriteAllTextAsync(jsonFile.FullName, jsonDbEntry);

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

            await File.WriteAllTextAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public static async Task WriteLocalDbJson(PostContent dbEntry)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(dbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePostContentDirectory(dbEntry).FullName,
                $"{Names.PostContentPrefix}{dbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            await File.WriteAllTextAsync(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricPostContents.Where(x => x.ContentId == dbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePostContentDirectory(dbEntry).FullName,
                $"{Names.HistoricPostContentPrefix}{dbEntry.ContentId}.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            await File.WriteAllTextAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public static async Task WriteLocalDbJson(PointContent dbEntry)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(dbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePointContentDirectory(dbEntry).FullName,
                $"{Names.PointContentPrefix}{dbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            await File.WriteAllTextAsync(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricPointContents.Where(x => x.ContentId == dbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePointContentDirectory(dbEntry).FullName,
                $"{Names.HistoricPointContentPrefix}{dbEntry.ContentId}.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            await File.WriteAllTextAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public static async Task WriteLocalDbJson(NoteContent dbEntry, IProgress<string> progress)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(dbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteNoteContentDirectory(dbEntry).FullName,
                $"{Names.NoteContentPrefix}{dbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            await File.WriteAllTextAsync(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricNoteContents.Where(x => x.ContentId == dbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteNoteContentDirectory(dbEntry).FullName,
                $"{Names.HistoricNoteContentPrefix}{dbEntry.ContentId}.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            await File.WriteAllTextAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public static async Task WriteLocalDbJson(ImageContent dbEntry)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(dbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteImageContentDirectory(dbEntry).FullName,
                $"{Names.ImageContentPrefix}{dbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            await File.WriteAllTextAsync(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricImageContents.Where(x => x.ContentId == dbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteImageContentDirectory(dbEntry).FullName,
                $"{Names.HistoricImageContentPrefix}{dbEntry.ContentId}.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            await File.WriteAllTextAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }


        public static async Task WriteLocalDbJson(PhotoContent dbEntry)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(dbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePhotoContentDirectory(dbEntry).FullName,
                $"{Names.PhotoContentPrefix}{dbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            await File.WriteAllTextAsync(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricPhotoContents.Where(x => x.ContentId == dbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePhotoContentDirectory(dbEntry).FullName,
                $"{Names.HistoricPhotoContentPrefix}{dbEntry.ContentId}.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            await File.WriteAllTextAsync(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public static void WriteMenuLinksJson()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var db = Db.Context().Result;
            var allContent = db.MenuLinks.OrderByDescending(x => x.MenuOrder).ToList();

            var jsonDbEntry = JsonSerializer.Serialize(allContent);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteDirectory().FullName,
                $"{Names.MenuLinksFileName}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);
        }

        public static void WriteTagExclusionsJson()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var db = Db.Context().Result;
            var allContent = db.TagExclusions.OrderByDescending(x => x.Tag).ToList();

            var jsonDbEntry = JsonSerializer.Serialize(allContent);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteTagsDirectory().FullName,
                $"{Names.TagExclusionsFileName}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);
        }
    }
}