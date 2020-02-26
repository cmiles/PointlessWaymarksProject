using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.JsonFiles
{
    public static class Export
    {
        public static void WriteLinkListJson()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var db = Db.Context().Result;
            var allContent = db.LinkStreams.OrderByDescending(x => x.CreatedOn).ToList();

            var jsonDbEntry = JsonSerializer.Serialize(allContent);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteLinkDirectory().FullName, "LinkList.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricLinkStreams.ToList();

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteLinkDirectory().FullName,
                "HistoricLinkList.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public static async Task WriteLocalDbJson(FileContent dbEntry, IProgress<string> progress = null)
        {
            progress?.Report("Writing Db Entry to Json");

            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(dbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteFileContentDirectory(dbEntry).FullName,
                $"File---{dbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            progress?.Report("Writing Historic Db Entries to Json");

            var latestHistoricEntries = db.HistoricFileContents.Where(x => x.ContentId == dbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10).ToList();

            if (!latestHistoricEntries.Any()) return;

            progress?.Report($" Archiving last {latestHistoricEntries.Count} Historic File Content Entries");

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteFileContentDirectory(dbEntry).FullName,
                $"HistoricFiles---{dbEntry.ContentId}.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public static async Task WriteLocalDbJson(PostContent dbEntry)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(dbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePostContentDirectory(dbEntry).FullName,
                $"Post---{dbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricPostContents.Where(x => x.ContentId == dbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePostContentDirectory(dbEntry).FullName,
                $"HistoricPosts---{dbEntry.ContentId}.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public static async Task WriteLocalDbJson(NoteContent dbEntry)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(dbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteNoteContentDirectory(dbEntry).FullName,
                $"Note---{dbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricNoteContents.Where(x => x.ContentId == dbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteNoteContentDirectory(dbEntry).FullName,
                $"HistoricNotes---{dbEntry.ContentId}.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public static async Task WriteLocalDbJson(ImageContent dbEntry)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(dbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteImageContentDirectory(dbEntry).FullName,
                $"Image---{dbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricImageContents.Where(x => x.ContentId == dbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteImageContentDirectory(dbEntry).FullName,
                $"HistoricImages---{dbEntry.ContentId}-Historic.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }


        public static async Task WriteLocalDbJson(PhotoContent dbEntry)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(dbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePhotoContentDirectory(dbEntry).FullName,
                $"Photo---{dbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricPhotoContents.Where(x => x.ContentId == dbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePhotoContentDirectory(dbEntry).FullName,
                $"HistoricPhoto---{dbEntry.ContentId}.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }
    }
}