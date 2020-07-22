using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;

namespace PointlessWaymarksCmsData.Content
{
    public static class RelatedContentReference
    {
        public static async Task ExtractAndWriteRelatedConteDbReferencesFromString(Guid sourceGuid, string toSearch,
            PointlessWaymarksContext db, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toSearch)) return;

            var toAdd = BracketCodeCommon.BracketCodeContentIds(toSearch);

            if (!toAdd.Any()) return;

            var dbEntries = toAdd.Select(x => new RelatedContent {ContentOne = sourceGuid, ContentTwo = x});

            await db.RelatedContents.AddRangeAsync(dbEntries);
        }

        public static async Task ExtractAndWriteRelatedContentDbReferences(List<IContentCommon> content,
            PointlessWaymarksContext db, IProgress<string> progress)
        {
            if (content == null || !content.Any()) return;

            foreach (var loopContent in content)
                await ExtractAndWriteRelatedContentDbReferences(loopContent, db, progress);
        }

        public static async Task ExtractAndWriteRelatedContentDbReferences(IContentCommon content,
            PointlessWaymarksContext db, IProgress<string> progress)
        {
            var toAdd = new List<Guid>();

            if (content.MainPicture != null) toAdd.Add(content.MainPicture.Value);

            var toSearch = string.Empty;

            toSearch += content.BodyContent + content.Summary;

            if (content is IUpdateNotes updateContent) toSearch += updateContent.UpdateNotes;

            if (string.IsNullOrWhiteSpace(toSearch) && !toAdd.Any()) return;

            toAdd.AddRange(BracketCodeCommon.BracketCodeContentIds(toSearch));

            if (!toAdd.Any()) return;

            var dbEntries = toAdd.Select(x => new RelatedContent {ContentOne = content.ContentId, ContentTwo = x});

            await db.RelatedContents.AddRangeAsync(dbEntries);
        }

        public static async Task GenerateRelatedContentDbTable(DateTime contentAfter, IProgress<string> progress)
        {
            var db = await Db.Context();

            progress?.Report("Clearing RelatedContents Db Table");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM [" + "RelatedContents" + "];");

            var files = (await db.FileContents.Where(x => x.ContentVersion > contentAfter).ToListAsync())
                .Cast<IContentCommon>().ToList();
            progress?.Report($"Processing {files.Count} File Content Entries for Related Content");
            await ExtractAndWriteRelatedContentDbReferences(files, db, progress);

            var images = (await db.ImageContents.Where(x => x.ContentVersion > contentAfter).ToListAsync())
                .Cast<IContentCommon>().ToList();
            progress?.Report($"Processing {images.Count} Image Content Entries for Related Content");
            await ExtractAndWriteRelatedContentDbReferences(images, db, progress);

            var links = await db.LinkStreams.Where(x => x.ContentVersion > contentAfter)
                .Select(x => new {x.ContentId, toCheck = x.Comments + x.Description}).ToListAsync();
            progress?.Report($"Processing {links.Count} Link Content Entries for Related Content");
            foreach (var loopLink in links)
                await ExtractAndWriteRelatedConteDbReferencesFromString(loopLink.ContentId, loopLink.toCheck, db,
                    progress);

            var notes = (await db.NoteContents.Where(x => x.ContentVersion > contentAfter).ToListAsync())
                .Cast<IContentCommon>().ToList();
            progress?.Report($"Processing {notes.Count} Note Content Entries for Related Content");
            await ExtractAndWriteRelatedContentDbReferences(notes, db, progress);

            var photos = (await db.PhotoContents.Where(x => x.ContentVersion > contentAfter).ToListAsync())
                .Cast<IContentCommon>().ToList();
            progress?.Report($"Processing {photos.Count} Photo Content Entries for Related Content");
            await ExtractAndWriteRelatedContentDbReferences(photos, db, progress);

            var posts = (await db.PostContents.Where(x => x.ContentVersion > contentAfter).ToListAsync())
                .Cast<IContentCommon>().ToList();
            progress?.Report($"Processing {posts.Count} Post Content Entries for Related Content");
            await ExtractAndWriteRelatedContentDbReferences(posts, db, progress);
        }
    }
}