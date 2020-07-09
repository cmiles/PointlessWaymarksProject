using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Json
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

        public static void FullImportFromRootDirectory(DirectoryInfo rootDirectory, IProgress<string> progress)
        {
            if (rootDirectory == null || !rootDirectory.Exists)
            {
                progress?.Report("Root Directory does not exist?");
                return;
            }

            var allFiles = GetAllJsonFiles(rootDirectory);

            DbImport.FileContentToDb(ContentFromFiles<FileContent>(allFiles, Names.FileContentPrefix), progress);
            DbImport.ImageContentToDb(ContentFromFiles<ImageContent>(allFiles, Names.ImageContentPrefix), progress);
            DbImport.LinkStreamToDb(
                ContentFromFiles<List<LinkStream>>(allFiles, Names.LinkListFileName).SelectMany(x => x).ToList(),
                progress);
            DbImport.NoteContentToDb(ContentFromFiles<NoteContent>(allFiles, Names.NoteContentPrefix), progress);
            DbImport.PhotoContentToDb(ContentFromFiles<PhotoContent>(allFiles, Names.PhotoContentPrefix), progress);
            DbImport.PostContentToDb(ContentFromFiles<PostContent>(allFiles, Names.PostContentPrefix), progress);

            DbImport.HistoricPostContentToDb(
                ContentFromFiles<List<HistoricPostContent>>(allFiles, Names.HistoricPostContentPrefix)
                    .SelectMany(x => x).ToList(), progress);
            DbImport.HistoricFileContentToDb(
                ContentFromFiles<List<HistoricFileContent>>(allFiles, Names.HistoricFileContentPrefix)
                    .SelectMany(x => x).ToList(), progress);
            DbImport.HistoricImageContentToDb(
                ContentFromFiles<List<HistoricImageContent>>(allFiles, Names.HistoricImageContentPrefix)
                    .SelectMany(x => x).ToList(), progress);
            DbImport.HistoricLinkStreamToDb(
                ContentFromFiles<List<HistoricLinkStream>>(allFiles, Names.HistoricLinkListFileName).SelectMany(x => x)
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
    }
}