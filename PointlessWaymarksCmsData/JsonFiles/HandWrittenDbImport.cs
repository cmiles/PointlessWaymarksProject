using System;
using System.Collections.Generic;
using System.Linq;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.JsonFiles
{
    public static class GeneratedDbImport
    {
        public static void NoteContentToDb(List<NoteContent> toImport, IProgress<string> progress)
        {
            progress?.Report("NoteContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No files to import?");
                return;
            }

            progress?.Report($"NoteContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopFiles in toImport)
            {
                progress?.Report($"{loopFiles.CreatedOn:d} - Starting NoteContent");

                var existingItems = db.NoteContents.Where(x => x.ContentId == loopFiles.ContentId).ToList();

                if (existingItems.Any())
                    progress?.Report($"{loopFiles.CreatedOn:d} - Found {existingItems.Count} to move to historic");

                foreach (var loopExisting in existingItems)
                {
                    var newHistoricEntry = new HistoricNoteContent();
                    newHistoricEntry.InjectFrom(loopExisting);
                    newHistoricEntry.Id = 0;

                    db.HistoricNoteContents.Add(newHistoricEntry);
                    db.NoteContents.Remove(loopExisting);
                    db.SaveChanges(true);
                }

                progress?.Report($"{loopFiles.CreatedOn:d} - Adding NoteContent");

                db.NoteContents.Add(loopFiles);

                db.SaveChanges(true);

                progress?.Report($"{loopFiles.CreatedOn:d} - Imported");
            }

            progress?.Report("NoteContent - Finished");
        }
    }
}