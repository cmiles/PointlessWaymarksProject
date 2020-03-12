using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.JsonFiles
{
    public static class Import
    {
        public static List<T> ContentFromFiles<T>(List<string> fileLists, string fileIdentifierPrefix)
        {
            var contentFiles = fileLists.Where(x => x.Contains(fileIdentifierPrefix));

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
            DbImport.LinkStreamToDb(ContentFromFiles<LinkStream>(allFiles, Names.LinkListFileName), progress);
            DbImport.NoteContentToDb(ContentFromFiles<NoteContent>(allFiles, Names.NoteContentPrefix), progress);
            DbImport.PhotoContentToDb(ContentFromFiles<PhotoContent>(allFiles, Names.PhotoContentPrefix), progress);
            DbImport.PostContentToDb(ContentFromFiles<PostContent>(allFiles, Names.PostContentPrefix), progress);

            DbImport.HistoricFileContentToDb(
                ContentFromFiles<HistoricFileContent>(allFiles, Names.HistoricFileContentPrefix), progress);
            DbImport.HistoricImageContentToDb(
                ContentFromFiles<HistoricImageContent>(allFiles, Names.HistoricImageContentPrefix), progress);
            DbImport.HistoricLinkStreamToDb(
                ContentFromFiles<HistoricLinkStream>(allFiles, Names.HistoricLinkListFileName), progress);
            DbImport.HistoricNoteContentToDb(
                ContentFromFiles<HistoricNoteContent>(allFiles, Names.HistoricNoteContentPrefix), progress);
            DbImport.HistoricPhotoContentToDb(
                ContentFromFiles<HistoricPhotoContent>(allFiles, Names.HistoricPhotoContentPrefix), progress);
            DbImport.HistoricPostContentToDb(
                ContentFromFiles<HistoricPostContent>(allFiles, Names.HistoricPostContentPrefix), progress);
        }

        public static List<string> GetAllJsonFiles(DirectoryInfo rootDirectory)
        {
            return Directory.GetFiles(rootDirectory.FullName, "*.json", SearchOption.AllDirectories).ToList();
        }

        public static List<T> HistoricContentFromFiles<T>(List<string> fileLists, string fileIdentifierPrefix)
        {
            var contentFiles = fileLists.Where(x => x.Contains(fileIdentifierPrefix));

            var returnList = new List<T>();

            foreach (var loopFiles in contentFiles)
                returnList.Add(JsonSerializer.Deserialize<T>(File.ReadAllText(loopFiles)));

            return returnList;
        }
    }
}