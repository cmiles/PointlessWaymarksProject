using System.Collections.Concurrent;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.ContentHtml.ImageHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineMonthlyActivitySummaryHtml;
using PointlessWaymarks.CmsData.ContentHtml.LinkListHtml;
using PointlessWaymarks.CmsData.ContentHtml.NoteHtml;
using PointlessWaymarks.CmsData.ContentHtml.PhotoGalleryHtml;
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.ContentHtml.PostHtml;
using PointlessWaymarks.CmsData.ContentHtml.SearchListHtml;
using PointlessWaymarks.CmsData.ContentHtml.VideoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class SiteGenerationChangedContent
{
    public static async Task GenerateChangedContentIdReferences(DateTime contentAfter, DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var mainContext = await Db.Context().ConfigureAwait(false);

        //!!Content Type List!!
        progress?.Report("Clearing GenerationChangedContentIds Table");
        await mainContext.Database.ExecuteSqlRawAsync("DELETE FROM [" + "GenerationChangedContentIds" + "];")
            .ConfigureAwait(false);

        var guidBag = new ConcurrentBag<List<Guid>>();

        var taskSet = new List<Func<Task>>
        {
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var files = await db.FileContents.Where(x => x.ContentVersion > contentAfter && !x.IsDraft)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(files);
                progress?.Report($"Found {files.Count} File Content Entries Changed After {contentAfter}");
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var geoJson = await db.GeoJsonContents.Where(x => x.ContentVersion > contentAfter && !x.IsDraft)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(geoJson);
                progress?.Report($"Found {geoJson.Count} GeoJson Content Entries Changed After {contentAfter}");
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var images = await db.ImageContents.Where(x => x.ContentVersion > contentAfter && !x.IsDraft)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(images);
                progress?.Report($"Found {images.Count} Image Content Entries Changed After {contentAfter}");
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var lines = await db.LineContents.Where(x => x.ContentVersion > contentAfter && !x.IsDraft)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(lines);
                progress?.Report($"Found {lines.Count} Line Content Entries Changed After {contentAfter}");
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var links = await db.LinkContents.Where(x => x.ContentVersion > contentAfter)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(links);
                progress?.Report($"Found {links.Count} Link Content Entries Changed After {contentAfter}");
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var maps = await db.MapComponents.Where(x => x.ContentVersion > contentAfter)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(maps);
                progress?.Report($"Found {maps.Count} Map Components Changed After {contentAfter}");
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var notes = await db.NoteContents.Where(x => x.ContentVersion > contentAfter && !x.IsDraft)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(notes);
                progress?.Report($"Found {notes.Count} Note Content Entries Changed After {contentAfter}");
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var photos = await db.PhotoContents.Where(x => x.ContentVersion > contentAfter && !x.IsDraft)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(photos);
                progress?.Report($"Found {photos.Count} Photo Content Entries Changed After {contentAfter}");
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var points = await db.PointContents.Where(x => x.ContentVersion > contentAfter && !x.IsDraft)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(points);
                progress?.Report($"Found {points.Count} Point Content Entries Changed After {contentAfter}");
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var posts = await db.PostContents.Where(x => x.ContentVersion > contentAfter && !x.IsDraft)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(posts);
                progress?.Report($"Found {posts.Count} Post Content Entries Changed After {contentAfter}");
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var posts = await db.Snippets.Where(x => x.ContentVersion > contentAfter)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(posts);
                progress?.Report($"Found {posts.Count} Snippet Content Entries Changed After {contentAfter}");
            },
            async () =>
            {
                var db = await Db.Context().ConfigureAwait(false);
                var videos = await db.VideoContents.Where(x => x.ContentVersion > contentAfter && !x.IsDraft)
                    .Select(x => x.ContentId).ToListAsync().ConfigureAwait(false);
                guidBag.Add(videos);
                progress?.Report($"Found {videos.Count} Video Content Entries Changed After {contentAfter}");
            }
        };

        await Parallel.ForEachAsync(taskSet, async (x, _) => await x()).ConfigureAwait(false);

        var contentChanges = guidBag.SelectMany(x => x.Select(y => y)).ToList();

        progress?.Report("Gathering deleted and new content with related content...");

        var previousGenerationVersion = mainContext.GenerationLogs
            .Where(x => x.GenerationVersion < generationVersion).Select(x => x.GenerationVersion)
            .OrderByDescending(x => x).FirstOrDefault();

        var lastLoopPrimaryIds = await mainContext.GenerationRelatedContents
            .Where(x => x.GenerationVersion == previousGenerationVersion).Select(x => x.ContentOne).Distinct()
            .ToListAsync().ConfigureAwait(false);

        var thisLoopPrimaryIds = await mainContext.GenerationRelatedContents
            .Where(x => x.GenerationVersion == generationVersion).Select(x => x.ContentOne).Distinct().ToListAsync()
            .ConfigureAwait(false);

        var noLongerPresentIds = lastLoopPrimaryIds.Except(thisLoopPrimaryIds).ToList();
        progress?.Report($"Found {noLongerPresentIds.Count} Content Ids not in current Related Ids");

        var addedIds = thisLoopPrimaryIds.Except(lastLoopPrimaryIds).ToList();
        progress?.Report($"Found {addedIds.Count} new Content Ids in current Related Ids");

        contentChanges = contentChanges.Concat(noLongerPresentIds).Concat(addedIds).Distinct().ToList();

        var originalContentSets = contentChanges.Chunk(500).ToList();

        var relatedIds = new List<Guid>();

        var currentCount = 1;

        progress?.Report($"Processing {originalContentSets.Count} Sets of Changed Content for Related Content");

        //This loop should get everything the content touches and everything that touches it refreshing both this time and last time
        foreach (var loopSets in originalContentSets)
        {
            progress?.Report($"Processing Related Content Set {currentCount++}");
            relatedIds.AddRange(await mainContext.GenerationRelatedContents
                .Where(x => loopSets.Contains(x.ContentTwo) && x.GenerationVersion == generationVersion)
                .Select(x => x.ContentOne).Distinct().ToListAsync().ConfigureAwait(false));
            relatedIds.AddRange(await mainContext.GenerationRelatedContents
                .Where(x => loopSets.Contains(x.ContentTwo) && x.GenerationVersion == previousGenerationVersion)
                .Select(x => x.ContentOne).Distinct().ToListAsync().ConfigureAwait(false));
            relatedIds.AddRange(await mainContext.GenerationRelatedContents
                .Where(x => loopSets.Contains(x.ContentOne) && x.GenerationVersion == generationVersion)
                .Select(x => x.ContentTwo).Distinct().ToListAsync().ConfigureAwait(false));
            relatedIds.AddRange(await mainContext.GenerationRelatedContents
                .Where(x => loopSets.Contains(x.ContentOne) && x.GenerationVersion == previousGenerationVersion)
                .Select(x => x.ContentTwo).Distinct().ToListAsync().ConfigureAwait(false));

            relatedIds = relatedIds.Distinct().ToList();
        }

        contentChanges.AddRange(relatedIds.Distinct());

        contentChanges = contentChanges.Distinct().ToList();

        progress?.Report($"Adding {relatedIds.Distinct().Count()} Content Ids Generate");

        await mainContext.GenerationChangedContentIds.AddRangeAsync(contentChanges.Distinct()
            .Select(x => new GenerationChangedContentId { ContentId = x }).ToList()).ConfigureAwait(false);

        await mainContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task GenerateChangedContentIdReferencesFromTagExclusionChanges(DateTime generationVersion,
        DateTime previousGenerationVersion, IProgress<string>? progress)
    {
        progress?.Report("Querying for changes to Tags Excluded from Search");

        var db = await Db.Context().ConfigureAwait(false);

        var excludedThisGeneration = await db.GenerationTagLogs
            .Where(x => x.GenerationVersion == generationVersion && x.TagIsExcludedFromSearch).AsNoTracking()
            .ToListAsync().ConfigureAwait(false);

        var excludedLastGeneration = await db.GenerationTagLogs.Where(x =>
                x.GenerationVersion == previousGenerationVersion && x.TagIsExcludedFromSearch).AsNoTracking()
            .ToListAsync().ConfigureAwait(false);

        var changedExclusions = excludedThisGeneration
            .Join(excludedLastGeneration, o => o.TagSlug, i => i.TagSlug, (x, y) => new { x, y })
            .Where(x => x.x.TagIsExcludedFromSearch != x.y.TagIsExcludedFromSearch).Where(x => x.x.TagSlug != null)
            .Select(x => x.x.TagSlug).ToList();

        if (changedExclusions.Count == 0)
        {
            progress?.Report("Found no Tags where there are changes to Excluded from Search");
            return;
        }

        progress?.Report($"Found {changedExclusions.Count} Tags where there are changes to Excluded from Search");

        var impactedContent = db.GenerationTagLogs.Where(x => changedExclusions.Contains(x.TagSlug))
            .Select(x => x.RelatedContentId).Distinct().ToList();

        progress?.Report(
            $"Found {impactedContent.Count} Content Ids where Tags Excluded from Search changes may cause the content to regenerate - writing to db.");

        foreach (var loopExclusionContentChanges in impactedContent)
        {
            if (await db.GenerationChangedContentIds.AnyAsync(x => x.ContentId == loopExclusionContentChanges)
                    .ConfigureAwait(false))
                continue;
            await db.GenerationChangedContentIds.AddAsync(new GenerationChangedContentId
            {
                ContentId = loopExclusionContentChanges
            }).ConfigureAwait(false);
        }

        progress?.Report("Done checking for changes to Tags Excluded from Search");
    }

    public static async Task GenerateChangedDailyPhotoGalleries(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var lastGeneration = await db.GenerationLogs.Where(x => x.GenerationVersion < generationVersion)
            .OrderByDescending(x => x.GenerationVersion).FirstOrDefaultAsync().ConfigureAwait(false);

        if (lastGeneration == null)
        {
            progress?.Report("No Last Generation Found - generating All Daily Photo HTML.");
            await SiteGenerationAllContent.GenerateAllDailyPhotoGalleriesHtml(generationVersion, progress)
                .ConfigureAwait(false);
            return;
        }

        //The assumption is that a photo date was either in the last Logs or is new (changed) in this
        //generation - the previous log WILL sometimes grab a date that is no longer present in the
        //current generation (all photos moved in this change set from 6/3 to 7/3 - 6/3 would be in the
        //lastGenerationDateList but is no longer a photo date). Do not use lastGenerationDateList as 
        //a definitive list of photo dates! Note that DailyPhotoGalleries below ignores invalid dates...
        var lastGenerationDateList = await db.GenerationDailyPhotoLogs
            .Where(x => x.GenerationVersion == lastGeneration.GenerationVersion)
            .OrderByDescending(x => x.DailyPhotoDate).ToListAsync().ConfigureAwait(false);

        if (!lastGenerationDateList.Any())
        {
            progress?.Report(
                "No Daily Photo generation recorded in last generation - generating All Daily Photo HTML");
            await SiteGenerationAllContent.GenerateAllDailyPhotoGalleriesHtml(generationVersion, progress)
                .ConfigureAwait(false);
            return;
        }

        var changedPhotoDates = db.PhotoContents.Where(x => !x.IsDraft).Join(db.GenerationChangedContentIds,
                x => x.ContentId,
                x => x.ContentId, (x, y) => x).Select(x => x.PhotoCreatedOn.Date).Distinct().OrderByDescending(x => x)
            .ToList();

        var datesToGenerate = new List<DateTime>();

        //Add the changed photo dates to the list to generate since we already know they are considered changed
        datesToGenerate.AddRange(changedPhotoDates);
        datesToGenerate.Sort();

        //DailyPhotoGalleries below ignores invalid dates so in the case where lastGenerationDateList contains
        //dates that no longer have a photo it is 'ok' to add to datesToGenerate and let DailyPhotoGalleries
        //do the work of filtering that out.
        foreach (var loopChangedDates in changedPhotoDates)
        {
            var after = lastGenerationDateList.Where(x => x.DailyPhotoDate > loopChangedDates)
                .MinBy(x => x.DailyPhotoDate);

            if (after != null && !datesToGenerate.Contains(after.DailyPhotoDate))
                datesToGenerate.Add(after.DailyPhotoDate);

            var before = lastGenerationDateList.Where(x => x.DailyPhotoDate < loopChangedDates)
                .MaxBy(x => x.DailyPhotoDate);

            if (before != null && !datesToGenerate.Contains(before.DailyPhotoDate))
                datesToGenerate.Add(before.DailyPhotoDate);
        }

        var resultantPages = await DailyPhotoPageGenerators
            .DailyPhotoGalleries(datesToGenerate, generationVersion, progress).ConfigureAwait(false);
        await Parallel.ForEachAsync(resultantPages, async (x, _) => await x.WriteLocalHtml().ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    public static async Task GenerateChangedListHtml(DateTime lastGenerationDateTime, DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        await SearchListPageGenerators.WriteAllContentCommonSearchListHtml(generationVersion, progress)
            .ConfigureAwait(false);

        //!!Content Type List!!
        var db = await Db.Context().ConfigureAwait(false);
        var filesChanged =
            db.FileContents.Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o)
                .Any() || db.FileContents.Where(x => x.MainPicture != null).Join(db.GenerationChangedContentIds,
                o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var filesDeleted =
            (await Db.DeletedFileContent().ConfigureAwait(false)).Any(
                x => x.ContentVersion > lastGenerationDateTime);
        if (filesChanged || filesDeleted)
        {
            progress?.Report("Found File Changes - generating content list");
            await SearchListPageGenerators.WriteFileContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        }
        else
        {
            progress?.Report("Skipping File List Generation - no file or file main picture changes found");
        }


        var geojsonChanged =
            db.GeoJsonContents.Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o)
                .Any() || db.GeoJsonContents.Where(x => x.MainPicture != null).Join(db.GenerationChangedContentIds,
                o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var geojsonDeleted =
            (await Db.DeletedGeoJsonContent().ConfigureAwait(false)).Any(
                x => x.ContentVersion > lastGenerationDateTime);
        if (geojsonChanged || geojsonDeleted)
            await SearchListPageGenerators.WriteGeoJsonContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        else progress?.Report("Skipping GeoJson List Generation - no file or file main picture changes found");


        var imagesChanged = db.ImageContents
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var imagesDeleted =
            (await Db.DeletedImageContent().ConfigureAwait(false)).Any(x =>
                x.ContentVersion > lastGenerationDateTime);
        if (imagesChanged || imagesDeleted)
            await SearchListPageGenerators.WriteImageContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        else progress?.Report("Skipping Image List Generation - no image changes found");


        var lineChanged =
            db.LineContents.Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o)
                .Any() || db.LineContents.Where(x => x.MainPicture != null).Join(db.GenerationChangedContentIds,
                o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var linesDeleted =
            (await Db.DeletedLineContent().ConfigureAwait(false)).Any(
                x => x.ContentVersion > lastGenerationDateTime);
        if (lineChanged || linesDeleted)
            await SearchListPageGenerators.WriteLineContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        else progress?.Report("Skipping Line List Generation - no file or file main picture changes found");


        var linksChanged = db.LinkContents
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var linksDeleted =
            (await Db.DeletedLinkContent().ConfigureAwait(false)).Any(
                x => x.ContentVersion > lastGenerationDateTime);
        if (linksChanged || linksDeleted)
        {
            var linkListPage = new LinkListPage { GenerationVersion = generationVersion };
            await linkListPage.WriteLocalHtmlRssAndJson().ConfigureAwait(false);
            progress?.Report("Creating Link List Json");
            await Export.WriteLinkListJson(progress).ConfigureAwait(false);
        }
        else
        {
            progress?.Report("Skipping Link List Generation - no image changes found");
        }


        var notesChanged = db.NoteContents
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var notesDeleted =
            (await Db.DeletedNoteContent().ConfigureAwait(false)).Any(
                x => x.ContentVersion > lastGenerationDateTime);
        if (notesChanged || notesDeleted)
            await SearchListPageGenerators.WriteNoteContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        else progress?.Report("Skipping Note List Generation - no image changes found");


        var photosChanged = db.PhotoContents
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var photosDeleted =
            (await Db.DeletedPhotoContent().ConfigureAwait(false)).Any(x =>
                x.ContentVersion > lastGenerationDateTime);
        if (photosChanged || photosDeleted)
            await SearchListPageGenerators.WritePhotoContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        else progress?.Report("Skipping Photo List Generation - no image changes found");


        var pointChanged =
            db.PointContents.Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o)
                .Any() || db.PointContents.Where(x => x.MainPicture != null).Join(db.GenerationChangedContentIds,
                o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var pointsDeleted =
            (await Db.DeletedPointContent().ConfigureAwait(false)).Any(x =>
                x.ContentVersion > lastGenerationDateTime);
        if (pointChanged || pointsDeleted)
            await SearchListPageGenerators.WritePointContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        else progress?.Report("Skipping Point List Generation - no file or file main picture changes found");


        var postChanged =
            db.PostContents.Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o)
                .Any() || db.PostContents.Where(x => x.MainPicture != null).Join(db.GenerationChangedContentIds,
                o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var postsDeleted =
            (await Db.DeletedPostContent().ConfigureAwait(false)).Any(
                x => x.ContentVersion > lastGenerationDateTime);
        if (postChanged || postsDeleted)
            await SearchListPageGenerators.WritePostContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        else progress?.Report("Skipping Post List Generation - no file or file main picture changes found");

        var videoChanged =
            db.VideoContents.Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o)
                .Any() || db.VideoContents.Where(x => x.MainPicture != null).Join(db.GenerationChangedContentIds,
                o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var videosDeleted =
            (await Db.DeletedVideoContent().ConfigureAwait(false)).Any(
                x => x.ContentVersion > lastGenerationDateTime);
        if (videoChanged || videosDeleted)
            await SearchListPageGenerators.WriteVideoContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        else progress?.Report("Skipping Video List Generation - no file or file main picture changes found");
    }

    public static async Task GenerateChangedMainFeedContent(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        //TODO: This current regenerates the Main Feed Content in order to make sure all previous/next content links are correct - this could be improved to just detect changes
        var mainFeedContent = (await Db.MainFeedCommonContent().ConfigureAwait(false)).Select(x => (false, x)).ToList();

        progress?.Report($"{mainFeedContent.Count} Main Feed Content Entries to Check - Checking for Adjacent Changes");

        if (!mainFeedContent.Any()) return;

        var db = await Db.Context().ConfigureAwait(false);

        for (var i = 0; i < mainFeedContent.Count; i++)
        {
            var currentItem = mainFeedContent[i];

            if (!await db.GenerationChangedContentIds.AnyAsync(x => x.ContentId == currentItem.Item2.ContentId)
                    .ConfigureAwait(false)) continue;

            currentItem.Item1 = true;
            if (i > 0)
            {
                // ReSharper disable once NotAccessedVariable Need nextItem as a variable for the assignment
                var previousItem = mainFeedContent[i - 1];
                previousItem.Item1 = true;
            }

            if (i < mainFeedContent.Count - 1)
            {
                // ReSharper disable once NotAccessedVariable Need nextItem as a variable for the assignment
                var nextItem = mainFeedContent[i + 1];
                nextItem.Item1 = true;
            }
        }

        var mainFeedChanges = mainFeedContent.Where(x => x.Item1).Select(x => x.Item2).ToList();

        foreach (var loopMains in mainFeedChanges)
        {
            if (await db.GenerationChangedContentIds.AnyAsync(x => x.ContentId == loopMains.ContentId)
                    .ConfigureAwait(false))
            {
                progress?.Report($"Main Feed - {loopMains.Title} already in Changed List");
                continue;
            }

            progress?.Report($"Main Feed - Generating {loopMains.Title}");
            await GenerateHtmlFromCommonContent(loopMains, generationVersion, progress).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Generates the Changed Tag Html. !!! The DB must be setup with Tag Generation Information before running this !!!
    ///     see SetupTagGenerationDbData()
    /// </summary>
    /// <param name="generationVersion"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task GenerateChangedTagHtml(DateTime generationVersion, IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var lastGeneration = await db.GenerationLogs.Where(x => x.GenerationVersion < generationVersion)
            .OrderByDescending(x => x.GenerationVersion).FirstOrDefaultAsync().ConfigureAwait(false);

        if (lastGeneration == null)
        {
            progress?.Report("No Last Generation Found - generating All Tag Html.");
            await SiteGenerationAllContent.GenerateAllTagHtml(generationVersion, progress).ConfigureAwait(false);
            return;
        }

        //Check for need to generate the tag search list - this list is a text only list so it only matters if it is the same list
        var searchTagsLastGeneration = db.GenerationTagLogs
            .Where(x => x.GenerationVersion == lastGeneration.GenerationVersion).Select(x => x.TagSlug).Distinct()
            .OrderBy(x => x).ToList();

        progress?.Report(
            $"Found {searchTagsLastGeneration.Count} search tags from the {lastGeneration.GenerationVersion} generation");


        var searchTagsThisGeneration = db.GenerationTagLogs.Where(x => x.GenerationVersion == generationVersion)
            .Select(x => x.TagSlug).Distinct().OrderBy(x => x).ToList();

        progress?.Report($"Found {searchTagsThisGeneration.Count} search tags from the this generation");


        var compareLogic = new CompareLogic();
        var searchTagComparisonResult = compareLogic.Compare(searchTagsLastGeneration, searchTagsThisGeneration);

        if (!searchTagComparisonResult.AreEqual)
        {
            progress?.Report(
                "Search Tags are different between this generation and the last generation - creating Tag List.");
            await SearchListPageGenerators.WriteTagList(generationVersion, progress).ConfigureAwait(false);
        }
        else
        {
            progress?.Report("Search Tags are the same as the last generation - skipping Tag List creation.");
        }

        CompareLogic GetTagCompareLogic()
        {
            var tagCompareLogic = new CompareLogic();
            var spec = new Dictionary<Type, IEnumerable<string>>
            {
                { typeof(GenerationTagLog), new[] { "RelatedContentId", "TagIsExcludedFromSearch" } }
            };
            tagCompareLogic.Config.CollectionMatchingSpec = spec;
            tagCompareLogic.Config.MembersToInclude.Add("RelatedContentId");
            tagCompareLogic.Config.MembersToInclude.Add("TagIsExcludedFromSearch");

            return tagCompareLogic;
        }

        async Task GenerateTag(DateTime dateTime, PointlessWaymarksContext pointlessWaymarksContext,
            CompareLogic tagCompareLogic, string tag, IProgress<string>? generateProgress)
        {
            var contentLastGeneration = await pointlessWaymarksContext.GenerationTagLogs.Where(x =>
                    x.GenerationVersion == lastGeneration.GenerationVersion && x.TagSlug == tag)
                .OrderBy(x => x.RelatedContentId).ToListAsync().ConfigureAwait(false);

            var contentThisGeneration = await pointlessWaymarksContext.GenerationTagLogs
                .Where(x => x.GenerationVersion == dateTime && x.TagSlug == tag).OrderBy(x => x.RelatedContentId)
                .ToListAsync().ConfigureAwait(false);

            var generationComparisonResults = tagCompareLogic.Compare(contentLastGeneration, contentThisGeneration);

            //List of content is not the same - page must be rebuilt
            if (!generationComparisonResults.AreEqual)
            {
                generateProgress?.Report($"New content found for tag {tag} - creating page");
                var contentToWrite = await pointlessWaymarksContext
                    .ContentFromContentIds(contentThisGeneration.Select(x => x.RelatedContentId).ToList())
                    .ConfigureAwait(false);
                await SearchListPageGenerators.WriteTagPage(tag, contentToWrite, dateTime, generateProgress)
                    .ConfigureAwait(false);
                return;
            }

            //Direct content Changes
            var primaryContentChanges = await pointlessWaymarksContext.GenerationChangedContentIds.AnyAsync(x =>
                contentThisGeneration.Select(y => y.RelatedContentId).Contains(x.ContentId)).ConfigureAwait(false);

            var directTagContent = await pointlessWaymarksContext
                .ContentFromContentIds(contentThisGeneration.Select(x => x.RelatedContentId).ToList())
                .ConfigureAwait(false);

            //Main Image changes
            var mainImageContentIds = directTagContent.Select(x =>
            {
                return x switch
                {
                    FileContent y => y.MainPicture,
                    PostContent y => y.MainPicture,
                    _ => null
                };
            }).Where(x => x != null).Cast<Guid>().ToList();

            var mainImageContentChanges = await pointlessWaymarksContext.GenerationChangedContentIds
                .AnyAsync(x => mainImageContentIds.Contains(x.ContentId)).ConfigureAwait(false);

            if (primaryContentChanges || mainImageContentChanges)
            {
                generateProgress?.Report($"Content Changes found for tag {tag} - creating page");

                await SearchListPageGenerators.WriteTagPage(tag, directTagContent, dateTime, generateProgress)
                    .ConfigureAwait(false);

                return;
            }

            generateProgress?.Report($"No Changes for tag {tag}");
        }

        //Evaluate each Tag - the tags list is a standard search list showing summary and main image in addition to changes to the linked content also check for changes
        //to the linked content contents...
        var allCurrentTags = db.GenerationTagLogs.Where(x => x.GenerationVersion == generationVersion)
            .Where(x => x.TagSlug != null).Select(x => x.TagSlug!).Distinct().OrderBy(x => x).ToList();

        var partitionedTags = allCurrentTags.Chunk(10).ToList();

        var allTasks = new List<Task>();

        foreach (var loopPartitions in partitionedTags)
            allTasks.Add(Task.Run(async () =>
            {
                var partitionDb = await Db.Context().ConfigureAwait(false);
                var partitionCompareLogic = GetTagCompareLogic();
                foreach (var loopTag in loopPartitions)
                    await GenerateTag(generationVersion, partitionDb, partitionCompareLogic, loopTag, progress)
                        .ConfigureAwait(false);
            }));

        await Task.WhenAll(allTasks).ConfigureAwait(false);
    }

    public static async Task GenerateChangeFilteredFileHtml(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.FileContents.Where(x => !x.IsDraft)
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync()
            .ConfigureAwait(false);

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

    public static async Task GenerateChangeFilteredGeoJsonHtml(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.GeoJsonContents.Where(x => !x.IsDraft)
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync()
            .ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} GeoJson Entries to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Changed GeoJson {loopItem.Title}");

            var htmlModel = new SingleGeoJsonPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteGeoJsonContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateChangeFilteredImageHtml(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.ImageContents.Where(x => !x.IsDraft)
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync()
            .ConfigureAwait(false);

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Changed Image {loopItem.Title}");

            var htmlModel = new SingleImagePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteImageContentData(loopItem).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateChangeFilteredLineHtml(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.LineContents.Where(x => !x.IsDraft)
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync()
            .ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Lines to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Changed Line {loopItem.Title}");

            var htmlModel = new SingleLinePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLineContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);

        if (allItems.Any())
        {
            await MapComponentGenerator.GenerateAllLinesData();
            await new LineMonthlyActivitySummaryPage(generationVersion).WriteLocalHtml();
        }
    }

    public static async Task GenerateChangeFilteredMapData(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.MapComponents
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync()
            .ConfigureAwait(false);

        var allDtoItems = await allItems.SelectInSequenceAsync(async x => await x.ToMapComponentDto(db));

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Map Components to Generate");

        await Parallel.ForEachAsync(allDtoItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing Data for {loopItem.Title}");

            await Export.WriteMapComponentContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateChangeFilteredMapIconJson(DateTime contentVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var changesAfterLocal = contentVersion.ToLocalTime();

        if (db.MapIcons.Any(x => x.LastUpdatedOn >= changesAfterLocal)) await MapIconGenerator.GenerateMapIconsFile();
    }

    public static async Task GenerateChangeFilteredNoteHtml(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.NoteContents.Where(x => !x.IsDraft)
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync()
            .ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Posts to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Changed Note Dated {loopItem.CreatedOn:d}");

            var htmlModel = new SingleNotePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteNoteContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateChangeFilteredPhotoHtml(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.PhotoContents.Where(x => !x.IsDraft)
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync()
            .ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Photos to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Change Photo {loopItem.Title}");

            var htmlModel = new SinglePhotoPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WritePhotoContentData(loopItem).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateChangeFilteredPointHtml(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.PointContents.Where(x => !x.IsDraft)
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync()
            .ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Points to Generate");

        if (allItems.Count > 0)
            await SiteGenerationAllContent.GenerateAllPointHtml(generationVersion, progress).ConfigureAwait(false);

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Changed Point{loopItem.Title}");

            var loopItemAndDetails = await Db.PointContentDto(loopItem.ContentId).ConfigureAwait(false);

            if (loopItemAndDetails == null) return;

            var htmlModel = new SinglePointPage(loopItemAndDetails) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WritePointContentData(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateChangeFilteredPostHtml(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.PostContents.Where(x => !x.IsDraft)
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync()
            .ConfigureAwait(false);

        var loopCount = 1;
        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Posts to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Changed Post {loopItem.Title} - {loopCount} of {totalCount}");

            var htmlModel = new SinglePostPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WritePostContentData(loopItem, progress).ConfigureAwait(false);

            loopCount++;
        }).ConfigureAwait(false);
    }

    public static async Task GenerateChangeFilteredVideoHtml(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.VideoContents.Where(x => !x.IsDraft)
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync()
            .ConfigureAwait(false);

        var loopCount = 1;
        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Videos to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for Changed Video {loopItem.Title} - {loopCount} of {totalCount}");

            var htmlModel = new SingleVideoPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteVideoContentData(loopItem).ConfigureAwait(false);

            loopCount++;
        }).ConfigureAwait(false);
    }

    public static async Task GenerateHtmlFromCommonContent(IContentCommon content, DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        //!!Content Type List!!
        switch (content)
        {
            case FileContent file:
                await FileGenerator.GenerateHtml(file, generationVersion, progress).ConfigureAwait(false);
                break;
            case GeoJsonContent geoJson:
                await GeoJsonGenerator.GenerateHtml(geoJson, generationVersion, progress).ConfigureAwait(false);
                break;
            case ImageContent image:
                await ImageGenerator.GenerateHtml(image, generationVersion, progress).ConfigureAwait(false);
                break;
            case LineContent line:
                await LineGenerator.GenerateHtml(line, generationVersion, progress).ConfigureAwait(false);
                break;
            case NoteContent note:
                await NoteGenerator.GenerateHtml(note, generationVersion, progress).ConfigureAwait(false);
                break;
            case PhotoContent photo:
                await PhotoGenerator.GenerateHtml(photo, generationVersion, progress).ConfigureAwait(false);
                break;
            case PointContent point:
                var dto = await Db.PointContentDto(point.ContentId).ConfigureAwait(false);
                await PointGenerator.GenerateHtml(dto!, generationVersion, progress).ConfigureAwait(false);
                break;
            case PointContentDto pointDto:
                await PointGenerator.GenerateHtml(pointDto, generationVersion, progress).ConfigureAwait(false);
                break;
            case PostContent post:
                await PostGenerator.GenerateHtml(post, generationVersion, progress).ConfigureAwait(false);
                break;
            case VideoContent video:
                await VideoGenerator.GenerateHtml(video, generationVersion, progress).ConfigureAwait(false);
                break;
            default:
                throw new Exception("The method GenerateHtmlFromCommonContent didn't find a content type match");
        }
    }
}