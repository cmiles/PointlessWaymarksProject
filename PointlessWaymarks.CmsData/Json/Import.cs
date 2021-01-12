using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarks.CmsData.Json
{
    public static class Import
    {
        public static List<T> ContentFromFiles<T>(List<string> fileLists, string fileIdentifierPrefix)
        {
            var contentFiles = fileLists.Where(x => x.Contains($"\\{fileIdentifierPrefix}")).ToList();

            var returnList = new List<T>();

            foreach (var loopFiles in contentFiles)
                returnList.Add(JsonSerializer.Deserialize<T>(File.ReadAllText(loopFiles)));

            return returnList;
        }

        public static void FullImportFromRootDirectory(DirectoryInfo rootDirectory, IProgress<string>? progress = null)
        {
            if (rootDirectory == null || !rootDirectory.Exists)
            {
                progress?.Report("Root Directory does not exist?");
                return;
            }

            var allFiles = GetAllJsonFiles(rootDirectory);

            DbImport.FileContentToDb(ContentFromFiles<FileContent>(allFiles, Names.FileContentPrefix), progress);
            DbImport.ImageContentToDb(ContentFromFiles<ImageContent>(allFiles, Names.ImageContentPrefix), progress);
            DbImport.LinkContentToDb(
                ContentFromFiles<List<LinkContent>>(allFiles, Names.LinkListFileName).SelectMany(x => x).ToList(),
                progress);
            DbImport.NoteContentToDb(ContentFromFiles<NoteContent>(allFiles, Names.NoteContentPrefix), progress);
            DbImport.PhotoContentToDb(ContentFromFiles<PhotoContent>(allFiles, Names.PhotoContentPrefix), progress);
            DbImport.PostContentToDb(ContentFromFiles<PostContent>(allFiles, Names.PostContentPrefix), progress);

            MenuLinksToDb(ContentFromFiles<MenuLink>(allFiles, Names.MenuLinksFileName), progress);
            TagExclusionsToDb(ContentFromFiles<TagExclusion>(allFiles, Names.TagExclusionsFileName), progress);

            DbImport.HistoricPostContentToDb(
                ContentFromFiles<List<HistoricPostContent>>(allFiles, Names.HistoricPostContentPrefix)
                    .SelectMany(x => x).ToList(), progress);
            DbImport.HistoricFileContentToDb(
                ContentFromFiles<List<HistoricFileContent>>(allFiles, Names.HistoricFileContentPrefix)
                    .SelectMany(x => x).ToList(), progress);
            DbImport.HistoricImageContentToDb(
                ContentFromFiles<List<HistoricImageContent>>(allFiles, Names.HistoricImageContentPrefix)
                    .SelectMany(x => x).ToList(), progress);
            DbImport.HistoricLinkContentToDb(
                ContentFromFiles<List<HistoricLinkContent>>(allFiles, Names.HistoricLinkListFileName).SelectMany(x => x)
                    .ToList(), progress);
            DbImport.HistoricNoteContentToDb(
                ContentFromFiles<List<HistoricNoteContent>>(allFiles, Names.HistoricNoteContentPrefix)
                    .SelectMany(x => x).ToList(), progress);
            DbImport.HistoricPhotoContentToDb(
                ContentFromFiles<List<HistoricPhotoContent>>(allFiles, Names.HistoricPhotoContentPrefix)
                    .SelectMany(x => x).ToList(), progress);
        }

        public static List<string> GetAllJsonFiles(DirectoryInfo rootDirectory)
        {
            return Directory.GetFiles(rootDirectory.FullName, "*.json", SearchOption.AllDirectories).ToList();
        }

        public static void MenuLinksToDb(List<MenuLink> toImport, IProgress<string>? progress = null)
        {
            progress?.Report("MenuLinks - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No MenuLink items to import...");
                return;
            }

            progress?.Report($"MenuLinks - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.MenuOrder} - Starting MenuLinks");

                var exactMatch = db.MenuLinks.Any(x =>
                    x.MenuOrder == loopImportItem.MenuOrder && x.LinkTag == loopImportItem.LinkTag);

                if (exactMatch)
                {
                    progress?.Report($"{loopImportItem.MenuOrder} - Found exact match in DB - skipping");
                    continue;
                }

                var orderMatch = db.MenuLinks.Any(x => x.MenuOrder == loopImportItem.MenuOrder);

                if (orderMatch)
                {
                    progress?.Report(
                        $"{loopImportItem.MenuOrder} - Found a conflicting order match in DB - adding to the end");
                    var maxOrder = db.MenuLinks.Max(x => x.MenuOrder);

                    db.MenuLinks.Add(new MenuLink {LinkTag = loopImportItem.LinkTag, MenuOrder = maxOrder});
                    db.SaveChanges(true);
                    continue;
                }

                progress?.Report($"{loopImportItem.MenuOrder} - Adding");

                db.MenuLinks.Add(new MenuLink {LinkTag = loopImportItem.LinkTag, MenuOrder = loopImportItem.MenuOrder});
                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.MenuOrder} - Imported");
            }

            progress?.Report("MenuLinks - Finished");
        }

        public static void TagExclusionsToDb(List<TagExclusion> toImport, IProgress<string>? progress = null)
        {
            progress?.Report("TagExclusions - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No TagExclusion items to import...");
                return;
            }

            progress?.Report($"TagExclusions - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopImportItem in toImport)
            {
                progress?.Report($"{loopImportItem.Tag} - Starting TagExclusions");

                var exactMatch = db.TagExclusions.Any(x => x.Tag == loopImportItem.Tag);

                if (exactMatch)
                {
                    progress?.Report($"{loopImportItem.Tag} - Found exact match in DB - skipping");
                    continue;
                }

                progress?.Report($"{loopImportItem.Tag} - Adding");

                db.TagExclusions.Add(new TagExclusion {Tag = loopImportItem.Tag});
                db.SaveChanges(true);

                progress?.Report($"{loopImportItem.Tag} - Imported");
            }

            progress?.Report("TagExclusions - Finished");
        }
    }
}