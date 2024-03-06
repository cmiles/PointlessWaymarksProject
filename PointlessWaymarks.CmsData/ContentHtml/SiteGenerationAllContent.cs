using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.ContentHtml.ImageHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineMonthlyActivitySummaryHtml;
using PointlessWaymarks.CmsData.ContentHtml.LinkListHtml;
using PointlessWaymarks.CmsData.ContentHtml.MapComponentData;
using PointlessWaymarks.CmsData.ContentHtml.NoteHtml;
using PointlessWaymarks.CmsData.ContentHtml.PhotoGalleryHtml;
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.ContentHtml.PostHtml;
using PointlessWaymarks.CmsData.ContentHtml.SearchListHtml;
using PointlessWaymarks.CmsData.ContentHtml.VideoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentHtml;

public static class SiteGenerationAllContent
{
    public static async Task GenerateAllDailyPhotoGalleriesHtml(DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        var allPages = await DailyPhotoPageGenerators.DailyPhotoGalleries(generationVersion, progress)
            .ConfigureAwait(false);

        await Parallel.ForEachAsync(allPages, async (x, _) => await x.WriteLocalHtml().ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    public static async Task GenerateAllFileHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.FileContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Files to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for File {loopItem.Title}");

            var htmlModel = new SingleFilePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteFileContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateAllGeoJsonHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.GeoJsonContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} GeoJson Entries to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for GeoJson {loopItem.Title}");

            var htmlModel = new SingleGeoJsonPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteGeoJsonContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateAllImageHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.ImageContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Images to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Image {loopItem.Title}");

            var htmlModel = new SingleImagePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteImageContentData(loopItem).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateAllLineHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.LineContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Lines to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Line {loopItem.Title}");

            var htmlModel = new SingleLinePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLineContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);

        await MapComponentGenerator.GenerateAllLinesData();
        await new LineMonthlyActivitySummaryPage(generationVersion).WriteLocalHtml();
    }

    public static async Task GenerateAllListHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var taskSet = new List<Func<Task>>
        {
            async () => await SearchListPageGenerators
                .WriteAllContentCommonSearchListHtml(generationVersion, progress).ConfigureAwait(false),
            async () => await SearchListPageGenerators.WriteFileContentListHtml(generationVersion, progress)
                .ConfigureAwait(false),
            async () => await SearchListPageGenerators.WriteImageContentListHtml(generationVersion, progress)
                .ConfigureAwait(false),
            async () => await SearchListPageGenerators.WritePhotoContentListHtml(generationVersion, progress)
                .ConfigureAwait(false),
            async () => await SearchListPageGenerators.WritePostContentListHtml(generationVersion, progress)
                .ConfigureAwait(false),
            async () => await SearchListPageGenerators.WritePointContentListHtml(generationVersion, progress)
                .ConfigureAwait(false),
            async () => await SearchListPageGenerators.WriteNoteContentListHtml(generationVersion, progress)
                .ConfigureAwait(false),
            async () =>
            {
                var linkListPage = new LinkListPage { GenerationVersion = generationVersion };
                await linkListPage.WriteLocalHtmlRssAndJson().ConfigureAwait(false);
                progress?.Report("Creating Link List Json");
                await Export.WriteLinkListJson(progress).ConfigureAwait(false);
            }
        };

        await Parallel.ForEachAsync(taskSet, async (x, _) => await x()).ConfigureAwait(false);
    }

    public static async Task GenerateAllMapData(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        await PointData.WriteJsonData().ConfigureAwait(false);

        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await (await db.MapComponents.ToListAsync().ConfigureAwait(false)).SelectInSequenceAsync(async x => await x.ToMapComponentDto(db));

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Map Components to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing Data for {loopItem.Title}");

            await Export.WriteMapComponentContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateAllNoteHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.NoteContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Posts to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Note Dated {loopItem.CreatedOn:d}");

            var htmlModel = new SingleNotePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteNoteContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateAllPhotoHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.PhotoContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Photos to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Photo {loopItem.Title}");

            var htmlModel = new SinglePhotoPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WritePhotoContentData(loopItem).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateAllPointHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        await PointData.WriteJsonData().ConfigureAwait(false);

        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.PointContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Points to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Point {loopItem.Title}");

            var dto = await Db.PointAndPointDetails(loopItem.ContentId, await Db.Context().ConfigureAwait(false))
                .ConfigureAwait(false);

            if (dto == null)
            {
                var toThrow = new ArgumentException(
                    $"Tried to retrieve Point Content DTO for {loopItem.ContentId} and found nothing in the database?");
                toThrow.Data.Add("ContentId", loopItem.ContentId);
                toThrow.Data.Add("Title", loopItem.Title);

                throw toThrow;
            }

            var htmlModel = new SinglePointPage(dto) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WritePointContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateAllPostHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.PostContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Posts to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Post {loopItem.Title}");

            var htmlModel = new SinglePostPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WritePostContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateAllTagHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        await SearchListPageGenerators.WriteTagListAndTagPages(generationVersion, progress).ConfigureAwait(false);
    }

    public static async Task GenerateAllUtilityJson(IProgress<string>? progress = null)
    {
        progress?.Report("Creating Menu Links Json");
        await Export.WriteMenuLinksJson(progress).ConfigureAwait(false);

        progress?.Report("Creating Tag Exclusion Json");
        await Export.WriteTagExclusionsJson(progress).ConfigureAwait(false);
    }

    public static async Task GenerateAllVideoHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.VideoContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Videos to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Video {loopItem.Title}");

            var htmlModel = new SingleVideoPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteVideoContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
}