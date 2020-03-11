using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
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

        public static List<string> GetAllJsonFiles(DirectoryInfo rootDirectory)
        {
            return Directory.GetFiles(rootDirectory.FullName, "*.json", SearchOption.AllDirectories).ToList();
        }

        private static DbSet<T> GetDbSet<T>(Type entityType, PointlessWaymarksContext db) where T : class
        {
            var allProperties = typeof(PointlessWaymarksContext).GetProperties(BindingFlags.Public);
            var typeDbSet = allProperties.Single(x =>
                x.PropertyType.IsGenericParameter && x.PropertyType.GenericTypeArguments.Length == 1 &&
                x.PropertyType.GenericTypeArguments.First() == entityType);

            return (DbSet<T>) typeDbSet.GetValue(db);
        }

        public static void ImportContentIntoDb(List<FileContent> toImport, IProgress<string> progress)
        {
            if (toImport == null || !toImport.Any()) progress?.Report("No files to import?");

            var db = Db.Context().Result;


            foreach (var loopFiles in toImport)
            {
                var existingItems = db.FileContents.Where(x => x.ContentId == loopFiles.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopFiles.Title} - Found {existingItems.Count} to move to historic");

                foreach (var loopExisting in existingItems)
                {
                    var newHistoricEntry = new HistoricFileContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricFileContents.Add(newHistoricEntry);
                    db.FileContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                if (existingItems.Any()) progress?.Report($"{loopFiles.Title} - Adding FileContent");

                db.FileContents.Add(loopFiles);

                db.SaveChanges(true);
            }
        }


        public static List<T> ImportHistoricContent<T>(List<string> fileLists, string fileIdentifierPrefix)
        {
            var contentFiles = fileLists.Where(x => x.Contains(fileIdentifierPrefix));

            var returnList = new List<T>();

            foreach (var loopFiles in contentFiles)
                returnList.Add(JsonSerializer.Deserialize<T>(File.ReadAllText(loopFiles)));

            return returnList;
        }
    }
}