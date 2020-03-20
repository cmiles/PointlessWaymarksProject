using System;
using System.Collections.Generic;
using System.Linq;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.JsonFiles
{
    public static class DbImport
    {
        public static void FileContentToDb(List<FileContent> toImport, IProgress<string> progress)
        {
            progress?.Report("FileContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"FileContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting FileContent");

                var existingItems = db.FileContents.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                var foundExistingMatchingEntry = false;

                foreach (var loopExisting in existingItems)
                {
                    if (loopExisting.CreatedOn == loopImportItem.CreatedOn &&
                        loopExisting.CreatedBy == loopImportItem.CreatedBy &&
                        loopExisting.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                        loopExisting.LastUpdatedBy == loopImportItem.LastUpdatedBy)
                    {
                        foundExistingMatchingEntry = true;
                        continue;
                    }

                    var newHistoricEntry = new HistoricFileContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricFileContents.Add(newHistoricEntry);
                    db.FileContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                if (foundExistingMatchingEntry)
                {
                    progress?.Report($"{loopImportItem.Title} - Found in DB");
                    continue;
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

            foreach (var loopImportItem in toImport)
            {
                if (db.HistoricFileContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.CreatedOn == loopImportItem.CreatedOn &&
                    x.CreatedBy == loopImportItem.CreatedBy && x.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                    x.LastUpdatedBy == loopImportItem.LastUpdatedBy))
                {
                    progress?.Report($"Historic {loopImportItem.CreatedOn:g} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricFileContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
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

            foreach (var loopImportItem in toImport)
            {
                if (db.HistoricImageContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.CreatedOn == loopImportItem.CreatedOn &&
                    x.CreatedBy == loopImportItem.CreatedBy && x.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                    x.LastUpdatedBy == loopImportItem.LastUpdatedBy))
                {
                    progress?.Report($"Historic {loopImportItem.CreatedOn:g} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricImageContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
            }

            progress?.Report("FileContent - Finished");
        }

        public static void HistoricLinkStreamToDb(List<HistoricLinkStream> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricLinkStream - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"HistoricLinkStream - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                if (db.HistoricLinkStreams.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.CreatedOn == loopImportItem.CreatedOn &&
                    x.CreatedBy == loopImportItem.CreatedBy && x.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                    x.LastUpdatedBy == loopImportItem.LastUpdatedBy))
                {
                    progress?.Report($"Historic {loopImportItem.CreatedOn:g} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricLinkStreams.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
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

            foreach (var loopImportItem in toImport)
            {
                if (db.HistoricNoteContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.CreatedOn == loopImportItem.CreatedOn &&
                    x.CreatedBy == loopImportItem.CreatedBy && x.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                    x.LastUpdatedBy == loopImportItem.LastUpdatedBy))
                {
                    progress?.Report($"Historic {loopImportItem.CreatedOn:g} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricNoteContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
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

            foreach (var loopImportItem in toImport)
            {
                if (db.HistoricPhotoContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.CreatedOn == loopImportItem.CreatedOn &&
                    x.CreatedBy == loopImportItem.CreatedBy && x.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                    x.LastUpdatedBy == loopImportItem.LastUpdatedBy))
                {
                    progress?.Report($"Historic {loopImportItem.CreatedOn:g} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricPhotoContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
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

            foreach (var loopImportItem in toImport)
            {
                if (db.HistoricPostContents.Any(x =>
                    x.ContentId == loopImportItem.ContentId && x.CreatedOn == loopImportItem.CreatedOn &&
                    x.CreatedBy == loopImportItem.CreatedBy && x.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                    x.LastUpdatedBy == loopImportItem.LastUpdatedBy))
                {
                    progress?.Report($"Historic {loopImportItem.CreatedOn:g} {loopImportItem.Title} - Already Exists");
                    continue;
                }

                loopImportItem.Id = 0;

                db.HistoricPostContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
            }

            progress?.Report("FileContent - Finished");
        }

        public static void ImageContentToDb(List<ImageContent> toImport, IProgress<string> progress)
        {
            progress?.Report("ImageContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"ImageContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting ImageContent");

                var existingItems = db.ImageContents.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                var foundExistingMatchingEntry = false;

                foreach (var loopExisting in existingItems)
                {
                    if (loopExisting.CreatedOn == loopImportItem.CreatedOn &&
                        loopExisting.CreatedBy == loopImportItem.CreatedBy &&
                        loopExisting.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                        loopExisting.LastUpdatedBy == loopImportItem.LastUpdatedBy)
                    {
                        foundExistingMatchingEntry = true;
                        continue;
                    }

                    var newHistoricEntry = new HistoricImageContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricImageContents.Add(newHistoricEntry);
                    db.ImageContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                if (foundExistingMatchingEntry)
                {
                    progress?.Report($"{loopImportItem.Title} - Found in DB");
                    continue;
                }

                progress?.Report($"{loopImportItem.Title} - Adding ImageContent");

                db.ImageContents.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
            }

            progress?.Report("ImageContent - Finished");
        }

        public static void LinkStreamToDb(List<LinkStream> toImport, IProgress<string> progress)
        {
            progress?.Report("LinkStream - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"LinkStream - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting LinkStream");

                var existingItems = db.LinkStreams.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                var foundExistingMatchingEntry = false;

                foreach (var loopExisting in existingItems)
                {
                    if (loopExisting.CreatedOn == loopImportItem.CreatedOn &&
                        loopExisting.CreatedBy == loopImportItem.CreatedBy &&
                        loopExisting.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                        loopExisting.LastUpdatedBy == loopImportItem.LastUpdatedBy)
                    {
                        foundExistingMatchingEntry = true;
                        continue;
                    }

                    var newHistoricEntry = new HistoricLinkStream();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricLinkStreams.Add(newHistoricEntry);
                    db.LinkStreams.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                if (foundExistingMatchingEntry)
                {
                    progress?.Report($"{loopImportItem.Title} - Found in DB");
                    continue;
                }

                progress?.Report($"{loopImportItem.Title} - Adding LinkStream");

                db.LinkStreams.Add(loopImportItem);

                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Title} - Imported");
            }

            progress?.Report("LinkStream - Finished");
        }

        public static void NoteContentToDb(List<NoteContent> toImport, IProgress<string> progress)
        {
            progress?.Report("NoteContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"NoteContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting NoteContent");

                var existingItems = db.NoteContents.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                var foundExistingMatchingEntry = false;

                foreach (var loopExisting in existingItems)
                {
                    if (loopExisting.CreatedOn == loopImportItem.CreatedOn &&
                        loopExisting.CreatedBy == loopImportItem.CreatedBy &&
                        loopExisting.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                        loopExisting.LastUpdatedBy == loopImportItem.LastUpdatedBy)
                    {
                        foundExistingMatchingEntry = true;
                        continue;
                    }

                    var newHistoricEntry = new HistoricNoteContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricNoteContents.Add(newHistoricEntry);
                    db.NoteContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                if (foundExistingMatchingEntry)
                {
                    progress?.Report($"{loopImportItem.Title} - Found in DB");
                    continue;
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
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"PhotoContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting PhotoContent");

                var existingItems = db.PhotoContents.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                var foundExistingMatchingEntry = false;

                foreach (var loopExisting in existingItems)
                {
                    if (loopExisting.CreatedOn == loopImportItem.CreatedOn &&
                        loopExisting.CreatedBy == loopImportItem.CreatedBy &&
                        loopExisting.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                        loopExisting.LastUpdatedBy == loopImportItem.LastUpdatedBy)
                    {
                        foundExistingMatchingEntry = true;
                        continue;
                    }

                    var newHistoricEntry = new HistoricPhotoContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricPhotoContents.Add(newHistoricEntry);
                    db.PhotoContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                if (foundExistingMatchingEntry)
                {
                    progress?.Report($"{loopImportItem.Title} - Found in DB");
                    continue;
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
                progress?.Report("No items to import?");
                return;
            }

            progress?.Report($"PostContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Title} - Starting PostContent");

                var existingItems = db.PostContents.Where(x => x.ContentId == loopImportItem.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopImportItem.Title} - Found {existingItems.Count} current items");

                var foundExistingMatchingEntry = false;

                foreach (var loopExisting in existingItems)
                {
                    if (loopExisting.CreatedOn == loopImportItem.CreatedOn &&
                        loopExisting.CreatedBy == loopImportItem.CreatedBy &&
                        loopExisting.LastUpdatedOn == loopImportItem.LastUpdatedOn &&
                        loopExisting.LastUpdatedBy == loopImportItem.LastUpdatedBy)
                    {
                        foundExistingMatchingEntry = true;
                        continue;
                    }

                    var newHistoricEntry = new HistoricPostContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricPostContents.Add(newHistoricEntry);
                    db.PostContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                if (foundExistingMatchingEntry)
                {
                    progress?.Report($"{loopImportItem.Title} - Found in DB");
                    continue;
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