using System.Collections.Concurrent;
using System.Text.Json;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.Reports;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.ContentGalleryHtml;
using PointlessWaymarks.CmsData.ContentHtml.ErrorHtml;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.ContentHtml.ImageHtml;
using PointlessWaymarks.CmsData.ContentHtml.IndexHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
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
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentHtml;

public static class HtmlGenerationGroups
{
    public static async Task CleanupGenerationInformation(IProgress<string>? progress = null)
    {
        progress?.Report("Cleaning up Generation Log Information");

        var db = await Db.Context().ConfigureAwait(false);

        var generationLogs = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).ToListAsync()
            .ConfigureAwait(false);
        var generationLogsToKeep = generationLogs.Take(30).ToList();
        var generationLogsToDelete = generationLogs.Skip(30).ToList();

        progress?.Report(
            $"Keeping Top {generationLogsToKeep.Count} Logs, Found {generationLogsToDelete.Count} logs to remove");

        //TODO Integrate into DataNotifications
        db.GenerationLogs.RemoveRange(generationLogsToDelete);

        await db.SaveChangesAsync().ConfigureAwait(false);


        //Current Generation Versions for Reference

        var currentGenerationVersions =
            await db.GenerationLogs.Select(x => x.GenerationVersion).ToListAsync().ConfigureAwait(false);


        //Photo Log Cleanup

        var olderPhotoGenerationInformationToRemove = await db.GenerationDailyPhotoLogs
            .Where(x => !currentGenerationVersions.Contains(x.GenerationVersion)).ToListAsync()
            .ConfigureAwait(false);

        progress?.Report($"Found {olderPhotoGenerationInformationToRemove.Count} photo generation logs to remove.");

        db.GenerationDailyPhotoLogs.RemoveRange(olderPhotoGenerationInformationToRemove);

        await db.SaveChangesAsync().ConfigureAwait(false);


        //File Script Cleanup

        var olderScriptGenerations = await db.GenerationFileTransferScriptLogs
            .OrderByDescending(x => x.WrittenOnVersion).Skip(30).ToListAsync().ConfigureAwait(false);

        progress?.Report($"Found {olderScriptGenerations.Count} logs to remove");

        if (olderScriptGenerations.Any())
        {
            db.GenerationFileTransferScriptLogs.RemoveRange(olderScriptGenerations);

            await db.SaveChangesAsync().ConfigureAwait(false);

            DataNotifications.PublishDataNotification("HtmlGenerationGroups.CleanupGenerationInformation",
                DataNotificationContentType.FileTransferScriptLog, DataNotificationUpdateType.Delete,
                new List<Guid>());
        }


        //File Write Logs

        DateTime? oldestDateTimeLog = null;

        if (currentGenerationVersions.Any()) oldestDateTimeLog = currentGenerationVersions.Min();

        if (db.GenerationFileWriteLogs.Any())
        {
            var oldestGenerationLog = db.GenerationFileWriteLogs.Min(x => x.WrittenOnVersion);
            if (oldestDateTimeLog == null || oldestGenerationLog < oldestDateTimeLog)
                oldestDateTimeLog = oldestGenerationLog;
        }

        if (oldestDateTimeLog != null)
        {
            var toRemove = await db.GenerationFileWriteLogs.Where(x => x.WrittenOnVersion < oldestDateTimeLog.Value)
                .ToListAsync().ConfigureAwait(false);

            progress?.Report($"Found {toRemove.Count} File Write Logs to remove");

            db.GenerationFileWriteLogs.RemoveRange(toRemove);

            await db.SaveChangesAsync().ConfigureAwait(false);
        }
        else
        {
            progress?.Report("Found zero File Write Logs to remove");
        }


        //Related Contents
        var relatedToRemove = await db.GenerationRelatedContents
            .Where(x => !currentGenerationVersions.Contains(x.GenerationVersion)).ToListAsync()
            .ConfigureAwait(false);

        progress?.Report($"Found {relatedToRemove.Count} Related Content Entries to Remove");

        await db.SaveChangesAsync().ConfigureAwait(false);


        //Tag Logs
        var olderTagGenerationInformationToRemove = await db.GenerationTagLogs
            .Where(x => !currentGenerationVersions.Contains(x.GenerationVersion)).ToListAsync()
            .ConfigureAwait(false);

        progress?.Report($"Found {olderTagGenerationInformationToRemove.Count} tag logs to remove.");

        db.GenerationTagLogs.RemoveRange(olderTagGenerationInformationToRemove);

        await db.SaveChangesAsync(true).ConfigureAwait(false);

        progress?.Report("Done with Generation Clean Up");
    }

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
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SingleFilePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem, progress).ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SingleGeoJsonPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task<List<GenerationReturn>> GenerateAllHtml(IProgress<string>? progress = null)
    {
        await CleanupGenerationInformation(progress).ConfigureAwait(false);

        var generationVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime();

        await FileManagement.WriteSiteResourcesToGeneratedSite(progress).ConfigureAwait(false);

        await RelatedContentReference.GenerateRelatedContentDbTable(generationVersion, progress)
            .ConfigureAwait(false);
        await SetupTagGenerationDbData(generationVersion, progress).ConfigureAwait(false);
        await SetupDailyPhotoGenerationDbData(generationVersion, progress).ConfigureAwait(false);

        await GenerateAllPhotoHtml(generationVersion, progress).ConfigureAwait(false);
        await GenerateAllImageHtml(generationVersion, progress).ConfigureAwait(false);

        //The All Map generation also regenerates the Point Data Json - to avoid conflicts run points before the group...
        await GenerateAllPointHtml(generationVersion, progress);

        var generationTasks = new List<Task>
        {
            GenerateAllFileHtml(generationVersion, progress),
            GenerateAllVideoHtml(generationVersion, progress),
            GenerateAllMapData(generationVersion, progress),
            GenerateAllNoteHtml(generationVersion, progress),
            GenerateAllPostHtml(generationVersion, progress),
            GenerateAllLineHtml(generationVersion, progress),
            GenerateAllGeoJsonHtml(generationVersion, progress),
            GenerateAllDailyPhotoGalleriesHtml(generationVersion, progress),
            GenerateCameraRollHtml(generationVersion, progress)
        };

        await Task.WhenAll(generationTasks).ConfigureAwait(false);

        var taskSet = new List<Func<Task>>
        {
            async () => await GenerateAllTagHtml(generationVersion, progress).ConfigureAwait(false),
            async () => await GenerateAllListHtml(generationVersion, progress).ConfigureAwait(false),
            async () => await GenerateAllUtilityJson(progress).ConfigureAwait(false),
            async () => await GenerateIndex(generationVersion, progress).ConfigureAwait(false),
            async () => await GenerateErrorPage(generationVersion, progress).ConfigureAwait(false)
        };

        await Parallel.ForEachAsync(taskSet, async (x, _) => await x()).ConfigureAwait(false);

        progress?.Report(
            $"Generation Complete - Writing Generation Date Time of UTC {generationVersion} in Db Generation log as Last Generation");

        await Db.SaveGenerationLogAndRecordSettings(generationVersion).ConfigureAwait(false);

        return await CommonContentValidation.CheckAllContentForBadContentReferences(progress).ConfigureAwait(false);
    }


    public static async Task GenerateAllImageHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.ImageContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Images to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SingleImagePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SingleLinePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
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
                await Export.WriteLinkListJson().ConfigureAwait(false);
            }
        };

        await Parallel.ForEachAsync(taskSet, async (x, _) => await x()).ConfigureAwait(false);
    }

    public static async Task GenerateAllMapData(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        await PointData.WriteJsonData().ConfigureAwait(false);

        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.MapComponents.ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Map Components to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing Data for {loopItem.Title}");

            await MapData.WriteJsonData(loopItem.ContentId).ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);
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
            await Export.WriteLocalDbJson(loopItem, progress).ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SinglePhotoPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for {loopItem.Title}");

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
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SinglePostPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateAllTagHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        await SearchListPageGenerators.WriteTagListAndTagPages(generationVersion, progress).ConfigureAwait(false);
    }

    public static async Task GenerateAllUtilityJson(IProgress<string>? progress = null)
    {
        progress?.Report("Creating Menu Links Json");
        await Export.WriteMenuLinksJson().ConfigureAwait(false);

        progress?.Report("Creating Tag Exclusion Json");
        await Export.WriteTagExclusionsJson().ConfigureAwait(false);
    }

    public static async Task GenerateAllVideoHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.VideoContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Videos to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SingleVideoPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task GenerateCameraRollHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var cameraRollPage = await CameraRollGalleryPageGenerator.CameraRoll(generationVersion, progress)
            .ConfigureAwait(false);
        await cameraRollPage.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task GenerateContentGalleryHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var contentGalleryPage = await ContentGalleryPageGenerators.ContentGallery(generationVersion, progress)
            .ConfigureAwait(false);
        await contentGalleryPage.WriteLocalHtml().ConfigureAwait(false);
    }

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
            await GenerateAllDailyPhotoGalleriesHtml(generationVersion, progress).ConfigureAwait(false);
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
            await GenerateAllDailyPhotoGalleriesHtml(generationVersion, progress).ConfigureAwait(false);
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

        var imagesChanged = db.ImageContents
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var imagesDeleted =
            (await Db.DeletedImageContent().ConfigureAwait(false)).Any(x =>
                x.ContentVersion > lastGenerationDateTime);
        if (imagesChanged || imagesDeleted)
            await SearchListPageGenerators.WriteImageContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        else progress?.Report("Skipping Image List Generation - no image changes found");

        var photosChanged = db.PhotoContents
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var photosDeleted =
            (await Db.DeletedPhotoContent().ConfigureAwait(false)).Any(x =>
                x.ContentVersion > lastGenerationDateTime);
        if (photosChanged || photosDeleted)
            await SearchListPageGenerators.WritePhotoContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        else progress?.Report("Skipping Photo List Generation - no image changes found");

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

        var notesChanged = db.NoteContents
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
        var notesDeleted =
            (await Db.DeletedNoteContent().ConfigureAwait(false)).Any(
                x => x.ContentVersion > lastGenerationDateTime);
        if (notesChanged || notesDeleted)
            await SearchListPageGenerators.WriteNoteContentListHtml(generationVersion, progress)
                .ConfigureAwait(false);
        else progress?.Report("Skipping Note List Generation - no image changes found");

        var linkListPage = new LinkListPage { GenerationVersion = generationVersion };
        await linkListPage.WriteLocalHtmlRssAndJson().ConfigureAwait(false);
        progress?.Report("Creating Link List Json");
        await Export.WriteLinkListJson().ConfigureAwait(false);
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
            await GenerateAllTagHtml(generationVersion, progress).ConfigureAwait(false);
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

    public static async Task<List<GenerationReturn>> GenerateChangedToHtml(IProgress<string>? progress = null)
    {
        await CleanupGenerationInformation(progress).ConfigureAwait(false);

        var db = await Db.Context().ConfigureAwait(false);

        //Get and check the last generation - if there is no value then generate all which should create a valid value for the next
        //run
        var generationVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime();
        var lastGenerationValues = db.GenerationLogs.Where(x => x.GenerationVersion < generationVersion)
            .OrderByDescending(x => x.GenerationVersion).FirstOrDefault();

        if (lastGenerationValues == null || string.IsNullOrWhiteSpace(lastGenerationValues.GenerationSettings))
        {
            progress?.Report("No value for Last Generation in Settings - Generating All HTML");

            return await GenerateAllHtml(progress).ConfigureAwait(false);
        }

        progress?.Report($"Last Generation - {lastGenerationValues.GenerationVersion}");
        var lastGenerationDateTime = lastGenerationValues.GenerationVersion;

        //The menu is currently written to all pages - if there are changes then generate all
        var menuUpdates = await db.MenuLinks.AnyAsync(x => x.ContentVersion > lastGenerationDateTime)
            .ConfigureAwait(false);

        if (menuUpdates)
        {
            progress?.Report("Menu Updates detected - menu updates impact all pages, generating All HTML");

            return await GenerateAllHtml(progress).ConfigureAwait(false);
        }

        //If the generation settings have changed trigger a full rebuild
        var lastGenerationSettings =
            JsonSerializer.Deserialize<UserSettingsGenerationValues>(lastGenerationValues.GenerationSettings);

        var currentGenerationSettings = UserSettingsSingleton.CurrentSettings().GenerationValues();

        var compareLogic = new CompareLogic(new ComparisonConfig { MaxDifferences = 20 });
        var generationSettingsComparison = compareLogic.Compare(lastGenerationSettings, currentGenerationSettings);

        var compareReport = new UserFriendlyReport();
        var generationSettingsComparisonDifferences =
            compareReport.OutputString(generationSettingsComparison.Differences);

        if (!generationSettingsComparison.AreEqual)
        {
            progress?.Report(
                $"Generation Settings Changes detected - generating All HTML: {Environment.NewLine}{generationSettingsComparisonDifferences}");

            return await GenerateAllHtml(progress).ConfigureAwait(false);
        }

        progress?.Report("Write Site Resources");
        await FileManagement.WriteSiteResourcesToGeneratedSite(progress).ConfigureAwait(false);

        progress?.Report($"Generation HTML based on changes after UTC - {lastGenerationValues.GenerationVersion}");

        await RelatedContentReference.GenerateRelatedContentDbTable(generationVersion, progress)
            .ConfigureAwait(false);
        await GenerateChangedContentIdReferences(lastGenerationDateTime, generationVersion, progress)
            .ConfigureAwait(false);
        await SetupTagGenerationDbData(generationVersion, progress).ConfigureAwait(false);
        await GenerateChangedContentIdReferencesFromTagExclusionChanges(generationVersion, lastGenerationDateTime,
            progress).ConfigureAwait(false);

        await SetupDailyPhotoGenerationDbData(generationVersion, progress).ConfigureAwait(false);

        if (!await db.GenerationChangedContentIds.AnyAsync().ConfigureAwait(false))
            progress?.Report("No Changes Detected - ending HTML generation.");

        await GenerateChangeFilteredPhotoHtml(generationVersion, progress).ConfigureAwait(false);
        await GenerateChangeFilteredImageHtml(generationVersion, progress).ConfigureAwait(false);

        //Both Maps and Points update the Point Json file - update the Points outside of the task list to avoid
        //both processing trying to write to the file.
        await GenerateChangeFilteredPointHtml(generationVersion, progress);

        var changedPartsList = new List<Task>
        {
            GenerateChangeFilteredFileHtml(generationVersion, progress),
            GenerateChangeFilteredVideoHtml(generationVersion, progress),
            GenerateChangeFilteredGeoJsonHtml(generationVersion, progress),
            GenerateChangeFilteredLineHtml(generationVersion, progress),
            GenerateChangeFilteredMapData(generationVersion, progress),
            GenerateChangeFilteredNoteHtml(generationVersion, progress),
            GenerateChangeFilteredPostHtml(generationVersion, progress)
        };

        await Task.WhenAll(changedPartsList).ConfigureAwait(false);

        await GenerateMainFeedContent(generationVersion, progress).ConfigureAwait(false);

        var hasDirectPhotoChanges = db.PhotoContents.Join(db.GenerationChangedContentIds, o => o.ContentId,
            i => i.ContentId, (o, i) => o.PhotoCreatedOn).Any();
        var hasRelatedPhotoChanges = db.PhotoContents.Join(db.GenerationRelatedContents, o => o.ContentId,
            i => i.ContentTwo, (o, i) => o.PhotoCreatedOn).Any();
        var hasDeletedPhotoChanges =
            (await Db.DeletedPhotoContent().ConfigureAwait(false)).Any(x =>
                x.ContentVersion > lastGenerationDateTime);

        if (hasDirectPhotoChanges || hasRelatedPhotoChanges || hasDeletedPhotoChanges)
            await GenerateChangedDailyPhotoGalleries(generationVersion, progress).ConfigureAwait(false);
        else
            progress?.Report(
                "No changes to Photos directly or thru related content - skipping Daily Photo Page generation.");

        if (hasDirectPhotoChanges || hasDeletedPhotoChanges)
            await GenerateCameraRollHtml(generationVersion, progress).ConfigureAwait(false);
        else progress?.Report("No changes to Photo content - skipping Photo Gallery generation.");

        var tagAndListTasks = new List<Task>
        {
            GenerateChangedTagHtml(generationVersion, progress),
            GenerateChangedListHtml(lastGenerationDateTime, generationVersion, progress),
            GenerateAllUtilityJson(progress),
            GenerateIndex(generationVersion, progress),
            GenerateErrorPage(generationVersion, progress)
        };

        await Task.WhenAll(tagAndListTasks).ConfigureAwait(false);

        progress?.Report(
            $"Generation Complete - writing {generationVersion} as Last Generation UTC into db Generation Log");

        await Db.SaveGenerationLogAndRecordSettings(generationVersion).ConfigureAwait(false);

        var allChangedContentCommon =
            (await db.ContentCommonShellFromContentIds(await db.GenerationChangedContentIds.Select(x => x.ContentId)
                .ToListAsync().ConfigureAwait(false)).ConfigureAwait(false)).Cast<IContentCommon>().ToList();

        return await CommonContentValidation.CheckForBadContentReferences(allChangedContentCommon, db, progress)
            .ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SingleFilePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem, progress).ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SingleGeoJsonPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SingleImagePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SingleLinePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem, progress).ConfigureAwait(false);
        }).ConfigureAwait(false);

        if (allItems.Any()) await MapComponentGenerator.GenerateAllLinesData();
    }

    public static async Task GenerateChangeFilteredMapData(DateTime generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allItems = await db.MapComponents
            .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync()
            .ConfigureAwait(false);

        var totalCount = allItems.Count;

        progress?.Report($"Found {totalCount} Map Components to Generate");

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing Data for {loopItem.Title}");

            await MapData.WriteJsonData(loopItem.ContentId).ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);
        }).ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for Note Dated {loopItem.CreatedOn:d}");

            var htmlModel = new SingleNotePage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem, progress).ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var htmlModel = new SinglePhotoPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);
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
            await GenerateAllPointHtml(generationVersion, progress).ConfigureAwait(false);

        await Parallel.ForEachAsync(allItems, async (loopItem, _) =>
        {
            progress?.Report($"Writing HTML for {loopItem.Title}");

            var loopItemAndDetails = await Db.PointAndPointDetails(loopItem.ContentId).ConfigureAwait(false);

            if (loopItemAndDetails == null) return;

            var htmlModel = new SinglePointPage(loopItemAndDetails) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);
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
            progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

            var htmlModel = new SinglePostPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);

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
            progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

            var htmlModel = new SingleVideoPage(loopItem) { GenerationVersion = generationVersion };
            await htmlModel.WriteLocalHtml().ConfigureAwait(false);
            await Export.WriteLocalDbJson(loopItem).ConfigureAwait(false);

            loopCount++;
        }).ConfigureAwait(false);
    }

    public static async Task GenerateErrorPage(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var error = new ErrorPage { GenerationVersion = generationVersion };
        await error.WriteLocalHtml().ConfigureAwait(false);
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
                var dto = await Db.PointAndPointDetails(point.ContentId).ConfigureAwait(false);
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

    public static async Task GenerateIndex(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var index = new IndexPage { GenerationVersion = generationVersion };
        await index.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task GenerateMainFeedContent(DateTime generationVersion, IProgress<string>? progress = null)
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

    public static async Task SetupDailyPhotoGenerationDbData(DateTime currentGenerationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        progress?.Report("Getting list of all Photo Dates and Content");

        var allPhotoInfo = await db.PhotoContents.Where(x => !x.IsDraft).AsNoTracking().ToListAsync()
            .ConfigureAwait(false);

        var datesAndContent = allPhotoInfo.GroupBy(x => x.PhotoCreatedOn.Date)
            .Select(x => new { date = x.Key, contentIds = x.Select(y => y.ContentId) })
            .OrderByDescending(x => x.date).ToList();

        progress?.Report("Processing Photo Dates and Content");

        foreach (var loopDates in datesAndContent)
        foreach (var loopContent in loopDates.contentIds)
            await db.GenerationDailyPhotoLogs.AddAsync(new GenerationDailyPhotoLog
            {
                DailyPhotoDate = loopDates.date,
                GenerationVersion = currentGenerationVersion,
                RelatedContentId = loopContent
            }).ConfigureAwait(false);

        progress?.Report("Saving Photo Dates and Content to db");

        await db.SaveChangesAsync(true).ConfigureAwait(false);
    }


    public static async Task SetupTagGenerationDbData(DateTime currentGenerationVersion,
        IProgress<string>? progress = null)
    {
        var tagData = await Db.TagSlugsAndContentList(true, false, progress).ConfigureAwait(false);

        var excludedTagSlugs = await Db.TagExclusionSlugs().ConfigureAwait(false);

        var db = await Db.Context().ConfigureAwait(false);

        foreach (var (tag, contentObjects) in tagData)
        foreach (var loopContent in contentObjects)
            await db.GenerationTagLogs.AddAsync(new GenerationTagLog
            {
                GenerationVersion = currentGenerationVersion,
                RelatedContentId = loopContent.ContentId,
                TagSlug = tag,
                TagIsExcludedFromSearch = excludedTagSlugs.Contains(tag)
            }).ConfigureAwait(false);

        await db.SaveChangesAsync(true).ConfigureAwait(false);
    }
}