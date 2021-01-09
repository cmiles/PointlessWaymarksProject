using System;
using System.Collections.Generic;
using System.Linq;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarksCmsData.Json
{
    public static class DbImport
    {
        public static void FileContentToDb(List<FileContent> toImport, IProgress<string> progress)
        {
            progress?.Report("FileContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No FileContent items to import...");
                return;
            }

            progress?.Report($"FileContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting FileContent");

                var exactMatch = db.FileContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

                if (exactMatch)
                {
                    progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                    continue;
                }

                var laterEntries = db.FileContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

                if (laterEntries)
                {
                    if (db.HistoricFileContents.Any(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic FileContent");
                    }
                    else
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricFileContents");

                        var newHistoricEntry = new HistoricFileContent();
                        newHistoricEntry.InjectFrom(loopImportItem);
                        newHistoricEntry.Id = 0;

                        db.HistoricFileContents.Add(newHistoricEntry);
                        db.SaveChanges(true);
                    }

                    continue;
                }

                var existingItems = db.FileContents.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                foreach (var loopExisting in existingItems)
                {
                    var newHistoricEntry = new HistoricFileContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricFileContents.Add(newHistoricEntry);
                    db.FileContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                progress?.Report($"{loopImportItem.Title} - Adding FileContent");

                db.FileContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
            }

            progress?.Report("FileContent - Finished");
        }

        public static void HistoricFileContentToDb(List<HistoricFileContent> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricFileContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"HistoricFileContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            var currentLoop = 1;
            var totalCount = toImport.Count;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

                if (db.HistoricFileContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricFileContents.Add(loopImportItem);

                db.SaveChanges(true);
            }

            progress?.Report("FileContent - Finished");
        }

        public static void HistoricImageContentToDb(List<HistoricImageContent> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricImageContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"HistoricImageContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            var currentLoop = 1;
            var totalCount = toImport.Count;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

                if (db.HistoricImageContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricImageContents.Add(loopImportItem);

                db.SaveChanges(true);
            }

            progress?.Report("FileContent - Finished");
        }

        public static void HistoricLinkContentToDb(List<HistoricLinkContent> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricLinkContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"HistoricLinkContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            var currentLoop = 1;
            var totalCount = toImport.Count;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

                if (db.HistoricLinkContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricLinkContents.Add(loopImportItem);

                db.SaveChanges(true);
            }

            progress?.Report("FileContent - Finished");
        }

        public static void HistoricNoteContentToDb(List<HistoricNoteContent> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricNoteContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"HistoricNoteContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            var currentLoop = 1;
            var totalCount = toImport.Count;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

                if (db.HistoricNoteContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricNoteContents.Add(loopImportItem);

                db.SaveChanges(true);
            }

            progress?.Report("FileContent - Finished");
        }

        public static void HistoricPhotoContentToDb(List<HistoricPhotoContent> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricPhotoContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"HistoricPhotoContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            var currentLoop = 1;
            var totalCount = toImport.Count;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

                if (db.HistoricPhotoContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricPhotoContents.Add(loopImportItem);

                db.SaveChanges(true);
            }

            progress?.Report("FileContent - Finished");
        }

        public static void HistoricPostContentToDb(List<HistoricPostContent> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricPostContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"HistoricPostContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            var currentLoop = 1;
            var totalCount = toImport.Count;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - {currentLoop++} of {totalCount} - Id {loopImportItem.Id}");

                if (db.HistoricPostContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                {
                    progress?.Report(
                        $"Historic {loopImportItem.ContentVersion:r} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricPostContents.Add(loopImportItem);

                db.SaveChanges(true);
            }

            progress?.Report("FileContent - Finished");
        }

        public static void ImageContentToDb(List<ImageContent> toImport, IProgress<string> progress)
        {
            progress?.Report("ImageContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No ImageContent items to import...");
                return;
            }

            progress?.Report($"ImageContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting ImageContent");

                var exactMatch = db.ImageContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

                if (exactMatch)
                {
                    progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                    continue;
                }

                var laterEntries = db.ImageContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

                if (laterEntries)
                {
                    if (db.HistoricImageContents.Any(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic ImageContent");
                    }
                    else
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricImageContents");

                        var newHistoricEntry = new HistoricImageContent();
                        newHistoricEntry.InjectFrom(loopImportItem);
                        newHistoricEntry.Id = 0;

                        db.HistoricImageContents.Add(newHistoricEntry);
                        db.SaveChanges(true);
                    }

                    continue;
                }

                var existingItems = db.ImageContents.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                foreach (var loopExisting in existingItems)
                {
                    var newHistoricEntry = new HistoricImageContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricImageContents.Add(newHistoricEntry);
                    db.ImageContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                progress?.Report($"{loopImportItem.Title} - Adding ImageContent");

                db.ImageContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
            }

            progress?.Report("ImageContent - Finished");
        }

        public static void LinkContentToDb(List<LinkContent> toImport, IProgress<string> progress)
        {
            progress?.Report("LinkContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No LinkContent items to import...");
                return;
            }

            progress?.Report($"LinkContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting LinkContent");

                var exactMatch = db.LinkContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

                if (exactMatch)
                {
                    progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                    continue;
                }

                var laterEntries = db.LinkContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

                if (laterEntries)
                {
                    if (db.HistoricLinkContents.Any(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic LinkContent");
                    }
                    else
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricLinkContents");

                        var newHistoricEntry = new HistoricLinkContent();
                        newHistoricEntry.InjectFrom(loopImportItem);
                        newHistoricEntry.Id = 0;

                        db.HistoricLinkContents.Add(newHistoricEntry);
                        db.SaveChanges(true);
                    }

                    continue;
                }

                var existingItems = db.LinkContents.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                foreach (var loopExisting in existingItems)
                {
                    var newHistoricEntry = new HistoricLinkContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricLinkContents.Add(newHistoricEntry);
                    db.LinkContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                progress?.Report($"{loopImportItem.Title} - Adding LinkContent");

                db.LinkContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
            }

            progress?.Report("LinkContent - Finished");
        }

        public static void NoteContentToDb(List<NoteContent> toImport, IProgress<string> progress)
        {
            progress?.Report("NoteContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No NoteContent items to import...");
                return;
            }

            progress?.Report($"NoteContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting NoteContent");

                var exactMatch = db.NoteContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

                if (exactMatch)
                {
                    progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                    continue;
                }

                var laterEntries = db.NoteContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

                if (laterEntries)
                {
                    if (db.HistoricNoteContents.Any(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic NoteContent");
                    }
                    else
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricNoteContents");

                        var newHistoricEntry = new HistoricNoteContent();
                        newHistoricEntry.InjectFrom(loopImportItem);
                        newHistoricEntry.Id = 0;

                        db.HistoricNoteContents.Add(newHistoricEntry);
                        db.SaveChanges(true);
                    }

                    continue;
                }

                var existingItems = db.NoteContents.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                foreach (var loopExisting in existingItems)
                {
                    var newHistoricEntry = new HistoricNoteContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricNoteContents.Add(newHistoricEntry);
                    db.NoteContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                progress?.Report($"{loopImportItem.Title} - Adding NoteContent");

                db.NoteContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
            }

            progress?.Report("NoteContent - Finished");
        }

        public static void PhotoContentToDb(List<PhotoContent> toImport, IProgress<string> progress)
        {
            progress?.Report("PhotoContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No PhotoContent items to import...");
                return;
            }

            progress?.Report($"PhotoContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting PhotoContent");

                var exactMatch = db.PhotoContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

                if (exactMatch)
                {
                    progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                    continue;
                }

                var laterEntries = db.PhotoContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

                if (laterEntries)
                {
                    if (db.HistoricPhotoContents.Any(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic PhotoContent");
                    }
                    else
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricPhotoContents");

                        var newHistoricEntry = new HistoricPhotoContent();
                        newHistoricEntry.InjectFrom(loopImportItem);
                        newHistoricEntry.Id = 0;

                        db.HistoricPhotoContents.Add(newHistoricEntry);
                        db.SaveChanges(true);
                    }

                    continue;
                }

                var existingItems = db.PhotoContents.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                foreach (var loopExisting in existingItems)
                {
                    var newHistoricEntry = new HistoricPhotoContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricPhotoContents.Add(newHistoricEntry);
                    db.PhotoContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                progress?.Report($"{loopImportItem.Title} - Adding PhotoContent");

                db.PhotoContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
            }

            progress?.Report("PhotoContent - Finished");
        }

        public static void PostContentToDb(List<PostContent> toImport, IProgress<string> progress)
        {
            progress?.Report("PostContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No PostContent items to import...");
                return;
            }

            progress?.Report($"PostContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting PostContent");

                var exactMatch = db.PostContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion);

                if (exactMatch)
                {
                    progress?.Report($"{loopImportItem.Title} - Found exact match in DB - skipping");
                    continue;
                }

                var laterEntries = db.PostContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.ContentVersion > loopImportItem.ContentVersion);

                if (laterEntries)
                {
                    if (db.HistoricPostContents.Any(x =>
                        x.ContentId == loopImportItem.ContentId && x.ContentVersion == loopImportItem.ContentVersion))
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry in Db and this entry already in Historic PostContent");
                    }
                    else
                    {
                        progress?.Report(
                            $"{loopImportItem.Title} - Found later entry already in db - moving this version to HistoricPostContents");

                        var newHistoricEntry = new HistoricPostContent();
                        newHistoricEntry.InjectFrom(loopImportItem);
                        newHistoricEntry.Id = 0;

                        db.HistoricPostContents.Add(newHistoricEntry);
                        db.SaveChanges(true);
                    }

                    continue;
                }

                var existingItems = db.PostContents.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                foreach (var loopExisting in existingItems)
                {
                    var newHistoricEntry = new HistoricPostContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricPostContents.Add(newHistoricEntry);
                    db.PostContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                progress?.Report($"{loopImportItem.Title} - Adding PostContent");

                db.PostContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
            }

            progress?.Report("PostContent - Finished");
        }
    }
}