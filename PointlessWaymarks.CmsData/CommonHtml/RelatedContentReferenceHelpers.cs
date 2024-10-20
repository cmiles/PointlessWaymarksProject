using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class RelatedContentReferenceHelpers
{
    public static async Task ExtractAndWriteRelatedContentDbReferences(DateTime generationVersion,
        List<IContentCommon> content, PointlessWaymarksContext db, IProgress<string>? progress = null)
    {
        if (!content.Any()) return;

        foreach (var loopContent in content)
            await ExtractAndWriteRelatedContentDbReferences(generationVersion, loopContent, db, progress)
                .ConfigureAwait(false);
    }


    public static async Task ExtractAndWriteRelatedContentDbReferences(DateTime generationVersion,
        IContentCommon content, PointlessWaymarksContext db, IProgress<string>? progress = null)
    {
        var toAdd = new List<Guid>();

        if (content.MainPicture != null && content.MainPicture != content.ContentId)
            toAdd.Add(content.MainPicture.Value);

        var toSearch = string.Empty;

        if (content is TrailContent trail)
        {
            if (trail.LineContentId is not null) toAdd.Add(trail.LineContentId.Value);
            if (trail.MapComponentId is not null) toAdd.Add(trail.MapComponentId.Value);
            if (trail.StartingPointContentId is not null) toAdd.Add(trail.StartingPointContentId.Value);
            if (trail.EndingPointContentId is not null) toAdd.Add(trail.EndingPointContentId.Value);

            toSearch += trail.Bikes + trail.BikesNote + trail.Dogs + trail.DogsNote + trail.Fee + trail.FeeNote +
                        trail.LocationArea + trail.OtherDetails + trail.TrailShape;
        }

        toSearch += content.BodyContent + content.Summary;

        if (content is GeoJsonContent geoContent) toSearch += geoContent.GeoJson;

        if (content is IUpdateNotes updateContent) toSearch += updateContent.UpdateNotes;

        if (string.IsNullOrWhiteSpace(toSearch) && !toAdd.Any()) return;

        toAdd.AddRange(BracketCodeCommon.BracketCodeContentIds(toSearch));

        if (!toAdd.Any()) return;

        var dbEntries = toAdd.Distinct().Select(x => new GenerationRelatedContent
        {
            ContentOne = content.ContentId, ContentTwo = x, GenerationVersion = generationVersion
        });

        await db.GenerationRelatedContents.AddRangeAsync(dbEntries).ConfigureAwait(false);

        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task ExtractAndWriteRelatedContentDbReferencesFromString(Guid sourceGuid, string toSearch,
        PointlessWaymarksContext db, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toSearch)) return;

        var toAdd = BracketCodeCommon.BracketCodeContentIds(toSearch);

        if (!toAdd.Any()) return;

        var dbEntries = toAdd.Select(x => new GenerationRelatedContent { ContentOne = sourceGuid, ContentTwo = x });

        await db.GenerationRelatedContents.AddRangeAsync(dbEntries).ConfigureAwait(false);
    }

    public static async Task GenerateRelatedContentDbTable(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        //!!Content Type List!!
        var taskFunctionList = new List<Func<Task>>
        {
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var files = (await db.FileContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false))
                    .Cast<IContentCommon>().ToList();
                progress?.Report($"Processing {files.Count} File Content Entries for Related Content");
                await ExtractAndWriteRelatedContentDbReferences(generationVersion, files, db, progress)
                    .ConfigureAwait(false);
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var geoJson = (await db.GeoJsonContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false))
                    .Cast<IContentCommon>().ToList();
                progress?.Report($"Processing {geoJson.Count} GeoJson Content Entries for Related Content");
                await ExtractAndWriteRelatedContentDbReferences(generationVersion, geoJson, db, progress)
                    .ConfigureAwait(false);
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var images = (await db.ImageContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false))
                    .Cast<IContentCommon>().ToList();
                progress?.Report($"Processing {images.Count} Image Content Entries for Related Content");
                await ExtractAndWriteRelatedContentDbReferences(generationVersion, images, db, progress)
                    .ConfigureAwait(false);
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var lines = (await db.LineContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false))
                    .Cast<IContentCommon>().ToList();
                progress?.Report($"Processing {lines.Count} Line Content Entries for Related Content");
                await ExtractAndWriteRelatedContentDbReferences(generationVersion, lines, db, progress)
                    .ConfigureAwait(false);
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var links = await db.LinkContents
                    .Select(x => new { x.ContentId, toCheck = x.Comments + x.Description })
                    .ToListAsync().ConfigureAwait(false);
                progress?.Report($"Processing {links.Count} Link Content Entries for Related Content");
                foreach (var loopLink in links)
                    await ExtractAndWriteRelatedContentDbReferencesFromString(loopLink.ContentId, loopLink.toCheck,
                        db,
                        progress).ConfigureAwait(false);
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var notes = (await db.NoteContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false))
                    .Cast<IContentCommon>().ToList();
                progress?.Report($"Processing {notes.Count} Note Content Entries for Related Content");
                await ExtractAndWriteRelatedContentDbReferences(generationVersion, notes, db, progress)
                    .ConfigureAwait(false);
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var photos = (await db.PhotoContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false))
                    .Cast<IContentCommon>().ToList();
                progress?.Report($"Processing {photos.Count} Photo Content Entries for Related Content");
                await ExtractAndWriteRelatedContentDbReferences(generationVersion, photos, db, progress)
                    .ConfigureAwait(false);
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var points = (await db.PointContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false))
                    .Cast<IContentCommon>().ToList();
                progress?.Report($"Processing {points.Count} Point Content Entries for Related Content");
                await ExtractAndWriteRelatedContentDbReferences(generationVersion, points, db, progress)
                    .ConfigureAwait(false);
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var posts = (await db.PostContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false))
                    .Cast<IContentCommon>().ToList();
                progress?.Report($"Processing {posts.Count} Post Content Entries for Related Content");
                await ExtractAndWriteRelatedContentDbReferences(generationVersion, posts, db, progress)
                    .ConfigureAwait(false);
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var trails = (await db.TrailContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false))
                    .Cast<IContentCommon>().ToList();
                progress?.Report($"Processing {trails.Count} Trail Content Entries for Related Content");
                await ExtractAndWriteRelatedContentDbReferences(generationVersion, trails, db, progress)
                    .ConfigureAwait(false);
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var videos = (await db.VideoContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false))
                    .Cast<IContentCommon>().ToList();
                progress?.Report($"Processing {videos.Count} Video Content Entries for Related Content");
                await ExtractAndWriteRelatedContentDbReferences(generationVersion, videos, db, progress)
                    .ConfigureAwait(false);
            }
        };

        var taskList = taskFunctionList.Select(Task.Run).ToList();

        await Task.WhenAll(taskList).ConfigureAwait(false);
    }
}