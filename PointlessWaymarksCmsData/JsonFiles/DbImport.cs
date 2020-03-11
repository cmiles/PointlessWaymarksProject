
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
                 progress?.Report("No files to import?");
                 return;
             }

             progress?.Report($"FileContent - Working with {toImport.Count} Entries");
        
             var db = Db.Context().Result;
             
             foreach (var loopFiles in toImport)
             {
                 progress?.Report($"{loopFiles.Title} - Starting FileContent");

                 var existingItems = db.FileContents.Where(x => x.ContentId == loopFiles.ContentId).ToList();
    
                 if(existingItems.Any()) progress?.Report($"{loopFiles.Title} - Found {existingItems.Count} to move to historic");
                 
                 foreach (var loopExisting in existingItems)
                 {
                     var newHistoricEntry = new HistoricFileContent();
                     newHistoricEntry.InjectFrom(loopExisting);
                     newHistoricEntry.Id = 0;
    
                     db.HistoricFileContents.Add(newHistoricEntry);
                     db.FileContents.Remove(loopExisting);
                     db.SaveChanges(true);
                 }
    
                 progress?.Report($"{loopFiles.Title} - Adding FileContent");
    
                 db.FileContents.Add(loopFiles);
    
                 db.SaveChanges(true);

                 progress?.Report($"{loopFiles.Title} - Imported");
             }

             progress?.Report("FileContent - Finished");

         }
         public static void PhotoContentToDb(List<PhotoContent> toImport, IProgress<string> progress)
         {
             progress?.Report("PhotoContent - Starting");

             if (toImport == null || !toImport.Any())
             {
                 progress?.Report("No files to import?");
                 return;
             }

             progress?.Report($"PhotoContent - Working with {toImport.Count} Entries");
        
             var db = Db.Context().Result;
             
             foreach (var loopFiles in toImport)
             {
                 progress?.Report($"{loopFiles.Title} - Starting PhotoContent");

                 var existingItems = db.PhotoContents.Where(x => x.ContentId == loopFiles.ContentId).ToList();
    
                 if(existingItems.Any()) progress?.Report($"{loopFiles.Title} - Found {existingItems.Count} to move to historic");
                 
                 foreach (var loopExisting in existingItems)
                 {
                     var newHistoricEntry = new HistoricPhotoContent();
                     newHistoricEntry.InjectFrom(loopExisting);
                     newHistoricEntry.Id = 0;
    
                     db.HistoricPhotoContents.Add(newHistoricEntry);
                     db.PhotoContents.Remove(loopExisting);
                     db.SaveChanges(true);
                 }
    
                 progress?.Report($"{loopFiles.Title} - Adding PhotoContent");
    
                 db.PhotoContents.Add(loopFiles);
    
                 db.SaveChanges(true);

                 progress?.Report($"{loopFiles.Title} - Imported");
             }

             progress?.Report("PhotoContent - Finished");

         }
         public static void PostContentToDb(List<PostContent> toImport, IProgress<string> progress)
         {
             progress?.Report("PostContent - Starting");

             if (toImport == null || !toImport.Any())
             {
                 progress?.Report("No files to import?");
                 return;
             }

             progress?.Report($"PostContent - Working with {toImport.Count} Entries");
        
             var db = Db.Context().Result;
             
             foreach (var loopFiles in toImport)
             {
                 progress?.Report($"{loopFiles.Title} - Starting PostContent");

                 var existingItems = db.PostContents.Where(x => x.ContentId == loopFiles.ContentId).ToList();
    
                 if(existingItems.Any()) progress?.Report($"{loopFiles.Title} - Found {existingItems.Count} to move to historic");
                 
                 foreach (var loopExisting in existingItems)
                 {
                     var newHistoricEntry = new HistoricPostContent();
                     newHistoricEntry.InjectFrom(loopExisting);
                     newHistoricEntry.Id = 0;
    
                     db.HistoricPostContents.Add(newHistoricEntry);
                     db.PostContents.Remove(loopExisting);
                     db.SaveChanges(true);
                 }
    
                 progress?.Report($"{loopFiles.Title} - Adding PostContent");
    
                 db.PostContents.Add(loopFiles);
    
                 db.SaveChanges(true);

                 progress?.Report($"{loopFiles.Title} - Imported");
             }

             progress?.Report("PostContent - Finished");

         }
         public static void ImageContentToDb(List<ImageContent> toImport, IProgress<string> progress)
         {
             progress?.Report("ImageContent - Starting");

             if (toImport == null || !toImport.Any())
             {
                 progress?.Report("No files to import?");
                 return;
             }

             progress?.Report($"ImageContent - Working with {toImport.Count} Entries");
        
             var db = Db.Context().Result;
             
             foreach (var loopFiles in toImport)
             {
                 progress?.Report($"{loopFiles.Title} - Starting ImageContent");

                 var existingItems = db.ImageContents.Where(x => x.ContentId == loopFiles.ContentId).ToList();
    
                 if(existingItems.Any()) progress?.Report($"{loopFiles.Title} - Found {existingItems.Count} to move to historic");
                 
                 foreach (var loopExisting in existingItems)
                 {
                     var newHistoricEntry = new HistoricImageContent();
                     newHistoricEntry.InjectFrom(loopExisting);
                     newHistoricEntry.Id = 0;
    
                     db.HistoricImageContents.Add(newHistoricEntry);
                     db.ImageContents.Remove(loopExisting);
                     db.SaveChanges(true);
                 }
    
                 progress?.Report($"{loopFiles.Title} - Adding ImageContent");
    
                 db.ImageContents.Add(loopFiles);
    
                 db.SaveChanges(true);

                 progress?.Report($"{loopFiles.Title} - Imported");
             }

             progress?.Report("ImageContent - Finished");

         }
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
                 progress?.Report($"{loopFiles.Title} - Starting NoteContent");

                 var existingItems = db.NoteContents.Where(x => x.ContentId == loopFiles.ContentId).ToList();
    
                 if(existingItems.Any()) progress?.Report($"{loopFiles.Title} - Found {existingItems.Count} to move to historic");
                 
                 foreach (var loopExisting in existingItems)
                 {
                     var newHistoricEntry = new HistoricNoteContent();
                     newHistoricEntry.InjectFrom(loopExisting);
                     newHistoricEntry.Id = 0;
    
                     db.HistoricNoteContents.Add(newHistoricEntry);
                     db.NoteContents.Remove(loopExisting);
                     db.SaveChanges(true);
                 }
    
                 progress?.Report($"{loopFiles.Title} - Adding NoteContent");
    
                 db.NoteContents.Add(loopFiles);
    
                 db.SaveChanges(true);

                 progress?.Report($"{loopFiles.Title} - Imported");
             }

             progress?.Report("NoteContent - Finished");

         }

        public static void HistoricFileContentToDb(List<HistoricFileContent> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricFileContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No files to import?");
                return;
            }

            progress?.Report($"HistoricFileContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopFiles in toImport)
            {
                progress?.Report($"{loopFiles.Title} - Starting HistoricFileContent");

                loopFiles.Id = 0;
                
                db.HistoricFileContents.Add(loopFiles);

                db.SaveChanges(true);

                progress?.Report($"{loopFiles.Title} - Imported");
            }

            progress?.Report("FileContent - Finished");
        }
        public static void HistoricPhotoContentToDb(List<HistoricPhotoContent> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricPhotoContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No files to import?");
                return;
            }

            progress?.Report($"HistoricPhotoContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopFiles in toImport)
            {
                progress?.Report($"{loopFiles.Title} - Starting HistoricPhotoContent");

                loopFiles.Id = 0;
                
                db.HistoricPhotoContents.Add(loopFiles);

                db.SaveChanges(true);

                progress?.Report($"{loopFiles.Title} - Imported");
            }

            progress?.Report("FileContent - Finished");
        }
        public static void HistoricPostContentToDb(List<HistoricPostContent> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricPostContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No files to import?");
                return;
            }

            progress?.Report($"HistoricPostContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopFiles in toImport)
            {
                progress?.Report($"{loopFiles.Title} - Starting HistoricPostContent");

                loopFiles.Id = 0;
                
                db.HistoricPostContents.Add(loopFiles);

                db.SaveChanges(true);

                progress?.Report($"{loopFiles.Title} - Imported");
            }

            progress?.Report("FileContent - Finished");
        }
        public static void HistoricImageContentToDb(List<HistoricImageContent> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricImageContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No files to import?");
                return;
            }

            progress?.Report($"HistoricImageContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopFiles in toImport)
            {
                progress?.Report($"{loopFiles.Title} - Starting HistoricImageContent");

                loopFiles.Id = 0;
                
                db.HistoricImageContents.Add(loopFiles);

                db.SaveChanges(true);

                progress?.Report($"{loopFiles.Title} - Imported");
            }

            progress?.Report("FileContent - Finished");
        }
        public static void HistoricNoteContentToDb(List<HistoricNoteContent> toImport, IProgress<string> progress)
        {
            progress?.Report("HistoricNoteContent - Starting");

            if (toImport == null || !toImport.Any())
            {
                progress?.Report("No files to import?");
                return;
            }

            progress?.Report($"HistoricNoteContent - Working with {toImport.Count} Entries");

            var db = Db.Context().Result;

            foreach (var loopFiles in toImport)
            {
                progress?.Report($"{loopFiles.Title} - Starting HistoricNoteContent");

                loopFiles.Id = 0;
                
                db.HistoricNoteContents.Add(loopFiles);

                db.SaveChanges(true);

                progress?.Report($"{loopFiles.Title} - Imported");
            }

            progress?.Report("FileContent - Finished");
        }
    }
}