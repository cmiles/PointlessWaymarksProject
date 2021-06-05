using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.Reports;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Content;
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
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData.ContentHtml
{
    public static class HtmlGenerationGroups
    {
        public static async Task CleanupGenerationInformation(IProgress<string>? progress = null)
        {
            progress?.Report("Cleaning up Generation Log Information");

            var db = await Db.Context();

            var generationLogs = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).ToListAsync();
            var generationLogsToKeep = generationLogs.Take(30).ToList();
            var generationLogsToDelete = generationLogs.Skip(30).ToList();

            progress?.Report(
                $"Keeping Top {generationLogsToKeep.Count} Logs, Found {generationLogsToDelete.Count} logs to remove");

            //TODO Integrate into DataNotifications
            db.GenerationLogs.RemoveRange(generationLogsToDelete);

            await db.SaveChangesAsync();


            //Current Generation Versions for Reference

            var currentGenerationVersions = await db.GenerationLogs.Select(x => x.GenerationVersion).ToListAsync();


            //Photo Log Cleanup

            var olderPhotoGenerationInformationToRemove = await db.GenerationDailyPhotoLogs
                .Where(x => !currentGenerationVersions.Contains(x.GenerationVersion)).ToListAsync();

            progress?.Report($"Found {olderPhotoGenerationInformationToRemove.Count} photo generation logs to remove.");

            db.GenerationDailyPhotoLogs.RemoveRange(olderPhotoGenerationInformationToRemove);

            await db.SaveChangesAsync();


            //File Script Cleanup

            var olderScriptGenerations = await db.GenerationFileTransferScriptLogs
                .OrderByDescending(x => x.WrittenOnVersion).Skip(30).ToListAsync();

            progress?.Report($"Found {olderScriptGenerations.Count} logs to remove");

            if (olderScriptGenerations.Any())
            {
                db.GenerationFileTransferScriptLogs.RemoveRange(olderScriptGenerations);

                await db.SaveChangesAsync();

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
                    .ToListAsync();

                progress?.Report($"Found {toRemove.Count} File Write Logs to remove");

                db.GenerationFileWriteLogs.RemoveRange(toRemove);

                await db.SaveChangesAsync();
            }
            else
            {
                progress?.Report("Found zero File Write Logs to remove");
            }


            //Related Contents
            var relatedToRemove = await db.GenerationRelatedContents
                .Where(x => !currentGenerationVersions.Contains(x.GenerationVersion)).ToListAsync();

            progress?.Report($"Found {relatedToRemove.Count} Related Content Entries to Remove");

            await db.SaveChangesAsync();


            //Tag Logs
            var olderTagGenerationInformationToRemove = await db.GenerationTagLogs
                .Where(x => !currentGenerationVersions.Contains(x.GenerationVersion)).ToListAsync();

            progress?.Report($"Found {olderTagGenerationInformationToRemove.Count} tag logs to remove.");

            db.GenerationTagLogs.RemoveRange(olderTagGenerationInformationToRemove);

            await db.SaveChangesAsync(true);

            progress?.Report("Done with Generation Clean Up");
        }

        public static async Task GenerateAllDailyPhotoGalleriesHtml(DateTime? generationVersion,
            IProgress<string>? progress = null)
        {
            var allPages = await DailyPhotoPageGenerators.DailyPhotoGalleries(generationVersion, progress);

            allPages.ForEach(x => x.WriteLocalHtml());
        }

        public static async Task GenerateAllFileHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.FileContents.ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Files to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var htmlModel = new SingleFilePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);
            });
        }

        public static async Task GenerateAllGeoJsonHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.GeoJsonContents.ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} GeoJson Entries to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var htmlModel = new SingleGeoJsonPage(loopItem) {GenerationVersion = generationVersion};
                await htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static async Task<List<GenerationReturn>> GenerateAllHtml(IProgress<string>? progress = null)
        {
            await CleanupGenerationInformation(progress);

            var generationVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime();

            await FileManagement.WriteSiteResourcesToGeneratedSite(progress);

            await RelatedContentReference.GenerateRelatedContentDbTable(generationVersion, progress);
            await SetupTagGenerationDbData(generationVersion, progress);
            await SetupDailyPhotoGenerationDbData(generationVersion, progress);

            await GenerateAllPhotoHtml(generationVersion, progress);
            await GenerateAllImageHtml(generationVersion, progress);

            var generationTasks = new List<Task>
            {
                GenerateAllFileHtml(generationVersion, progress),
                GenerateAllMapData(generationVersion, progress),
                GenerateAllNoteHtml(generationVersion, progress),
                GenerateAllPostHtml(generationVersion, progress),
                GenerateAllPointHtml(generationVersion, progress),
                GenerateAllLineHtml(generationVersion, progress),
                GenerateAllGeoJsonHtml(generationVersion, progress),
                GenerateAllDailyPhotoGalleriesHtml(generationVersion, progress),
                GenerateCameraRollHtml(generationVersion, progress)
            };

            await Task.WhenAll(generationTasks);

            var tagAndListTasks = new List<Action>
            {
                () => GenerateAllTagHtml(generationVersion, progress),
                () => GenerateAllListHtml(generationVersion, progress),
                () => GenerateAllUtilityJson(progress),
                () => GenerateIndex(generationVersion, progress)
            };

            Parallel.ForEach(tagAndListTasks, x => x());

            progress?.Report(
                $"Generation Complete - Writing Generation Date Time of UTC {generationVersion} in Db Generation log as Last Generation");

            await Db.SaveGenerationLogAndRecordSettings(generationVersion);

            return await CommonContentValidation.CheckAllContentForBadContentReferences(progress);
        }


        public static async Task GenerateAllImageHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.ImageContents.ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Images to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var htmlModel = new SingleImagePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static async Task GenerateAllLineHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.LineContents.ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Lines to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var htmlModel = new SingleLinePage(loopItem) {GenerationVersion = generationVersion};
                await htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static void GenerateAllListHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var actionList = new List<Action>
            {
                () => SearchListPageGenerators.WriteAllContentCommonSearchListHtml(generationVersion, progress),
                () => SearchListPageGenerators.WriteFileContentListHtml(generationVersion, progress),
                () => SearchListPageGenerators.WriteImageContentListHtml(generationVersion, progress),
                () => SearchListPageGenerators.WritePhotoContentListHtml(generationVersion, progress),
                () => SearchListPageGenerators.WritePostContentListHtml(generationVersion, progress),
                () => SearchListPageGenerators.WritePointContentListHtml(generationVersion, progress),
                () => SearchListPageGenerators.WriteNoteContentListHtml(generationVersion, progress),
                () =>
                {
                    var linkListPage = new LinkListPage {GenerationVersion = generationVersion};
                    linkListPage.WriteLocalHtmlRssAndJson();
                    progress?.Report("Creating Link List Json");
                    Export.WriteLinkListJson();
                }
            };

            Parallel.ForEach(actionList, x => x());
        }

        public static async Task GenerateAllMapData(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            await PointData.WriteJsonData();

            var db = await Db.Context();

            var allItems = await db.MapComponents.ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Map Components to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing Data for {loopItem.Title}");

                await MapData.WriteJsonData(loopItem.ContentId);
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static async Task GenerateAllNoteHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.NoteContents.ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for Note Dated {loopItem.CreatedOn:d}");

                var htmlModel = new SingleNotePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);
            });
        }

        public static async Task GenerateAllPhotoHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.PhotoContents.ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Photos to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var htmlModel = new SinglePhotoPage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static async Task GenerateAllPointHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            await PointData.WriteJsonData();

            var db = await Db.Context();

            var allItems = await db.PointContents.ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Points to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var dto = await Db.PointAndPointDetails(loopItem.ContentId, await Db.Context());

                if (dto == null)
                {
                    var toThrow = new ArgumentException(
                        $"Tried to retrieve Point Content DTO for {loopItem.ContentId} and found nothing in the database?");
                    toThrow.Data.Add("ContentId", loopItem.ContentId);
                    toThrow.Data.Add("Title", loopItem.Title);

                    throw toThrow;
                }

                var htmlModel = new SinglePointPage(dto) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static async Task GenerateAllPostHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.PostContents.ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var htmlModel = new SinglePostPage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static void GenerateAllTagHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            SearchListPageGenerators.WriteTagListAndTagPages(generationVersion, progress);
        }

        public static void GenerateAllUtilityJson(IProgress<string>? progress = null)
        {
            progress?.Report("Creating Menu Links Json");
            Export.WriteMenuLinksJson();

            progress?.Report("Creating Tag Exclusion Json");
            Export.WriteTagExclusionsJson();
        }

        public static async Task GenerateCameraRollHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var cameraRollPage = await CameraRollGalleryPageGenerator.CameraRoll(generationVersion, progress);
            cameraRollPage.WriteLocalHtml();
        }

        public static async Task GenerateChangedContentIdReferences(DateTime contentAfter, DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            var mainContext = await Db.Context();

            //!!Content Type List!!
            progress?.Report("Clearing GenerationChangedContentIds Table");
            await mainContext.Database.ExecuteSqlRawAsync("DELETE FROM [" + "GenerationChangedContentIds" + "];");

            var guidBag = new ConcurrentBag<List<Guid>>();

            await new List<Func<Task>>
            {
                async () =>
                {
                    var db = await Db.Context();
                    var files = await db.FileContents.Where(x => x.ContentVersion > contentAfter)
                        .Select(x => x.ContentId)
                        .ToListAsync();
                    guidBag.Add(files);
                    progress?.Report($"Found {files.Count} File Content Entries Changed After {contentAfter}");
                },
                async () =>
                {
                    var db = await Db.Context();
                    var geoJson =
                        await db.GeoJsonContents.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId)
                            .ToListAsync();
                    guidBag.Add(geoJson);
                    progress?.Report($"Found {geoJson.Count} GeoJson Content Entries Changed After {contentAfter}");
                },
                async () =>
                {
                    var db = await Db.Context();
                    var images = await db.ImageContents.Where(x => x.ContentVersion > contentAfter)
                        .Select(x => x.ContentId)
                        .ToListAsync();
                    guidBag.Add(images);
                    progress?.Report($"Found {images.Count} Image Content Entries Changed After {contentAfter}");
                },
                async () =>
                {
                    var db = await Db.Context();
                    var lines = await db.LineContents.Where(x => x.ContentVersion > contentAfter)
                        .Select(x => x.ContentId)
                        .ToListAsync();
                    guidBag.Add(lines);
                    progress?.Report($"Found {lines.Count} Line Content Entries Changed After {contentAfter}");
                },
                async () =>
                {
                    var db = await Db.Context();
                    var links = await db.LinkContents.Where(x => x.ContentVersion > contentAfter)
                        .Select(x => x.ContentId)
                        .ToListAsync();
                    guidBag.Add(links);
                    progress?.Report($"Found {links.Count} Link Content Entries Changed After {contentAfter}");
                },
                async () =>
                {
                    var db = await Db.Context();
                    var maps = await db.MapComponents.Where(x => x.ContentVersion > contentAfter)
                        .Select(x => x.ContentId)
                        .ToListAsync();
                    guidBag.Add(maps);
                    progress?.Report($"Found {maps.Count} Map Components Changed After {contentAfter}");
                },
                async () =>
                {
                    var db = await Db.Context();
                    var notes = await db.NoteContents.Where(x => x.ContentVersion > contentAfter)
                        .Select(x => x.ContentId)
                        .ToListAsync();
                    guidBag.Add(notes);
                    progress?.Report($"Found {notes.Count} Note Content Entries Changed After {contentAfter}");
                },
                async () =>
                {
                    var db = await Db.Context();
                    var photos = await db.PhotoContents.Where(x => x.ContentVersion > contentAfter)
                        .Select(x => x.ContentId)
                        .ToListAsync();
                    guidBag.Add(photos);
                    progress?.Report($"Found {photos.Count} Photo Content Entries Changed After {contentAfter}");
                },
                async () =>
                {
                    var db = await Db.Context();
                    var points = await db.PointContents.Where(x => x.ContentVersion > contentAfter)
                        .Select(x => x.ContentId)
                        .ToListAsync();
                    guidBag.Add(points);
                    progress?.Report($"Found {points.Count} Point Content Entries Changed After {contentAfter}");
                },
                async () =>
                {
                    var db = await Db.Context();
                    var posts = await db.PostContents.Where(x => x.ContentVersion > contentAfter)
                        .Select(x => x.ContentId)
                        .ToListAsync();
                    guidBag.Add(posts);
                    progress?.Report($"Found {posts.Count} Post Content Entries Changed After {contentAfter}");
                }
            }.AsyncParallelForEach();

            var contentChanges = guidBag.SelectMany(x => x.Select(y => y)).ToList();

            progress?.Report("Gathering deleted and new content with related content...");

            var previousGenerationVersion = mainContext.GenerationLogs
                .Where(x => x.GenerationVersion < generationVersion)
                .Select(x => x.GenerationVersion).OrderByDescending(x => x).FirstOrDefault();

            var lastLoopPrimaryIds = await mainContext.GenerationRelatedContents
                .Where(x => x.GenerationVersion == previousGenerationVersion).Select(x => x.ContentOne).Distinct()
                .ToListAsync();

            var thisLoopPrimaryIds = await mainContext.GenerationRelatedContents
                .Where(x => x.GenerationVersion == generationVersion).Select(x => x.ContentOne).Distinct()
                .ToListAsync();

            var noLongerPresentIds = lastLoopPrimaryIds.Except(thisLoopPrimaryIds).ToList();
            progress?.Report($"Found {noLongerPresentIds.Count} Content Ids not in current Related Ids");

            var addedIds = thisLoopPrimaryIds.Except(lastLoopPrimaryIds).ToList();
            progress?.Report($"Found {addedIds.Count} new Content Ids in current Related Ids");

            contentChanges = contentChanges.Concat(noLongerPresentIds).Concat(addedIds).Distinct().ToList();

            var originalContentSets = contentChanges.Partition(500).ToList();

            var relatedIds = new List<Guid>();

            var currentCount = 1;

            progress?.Report($"Processing {originalContentSets.Count} Sets of Changed Content for Related Content");

            //This loop should get everything the content touches and everything that touches it refreshing both this time and last time
            foreach (var loopSets in originalContentSets)
            {
                progress?.Report($"Processing Related Content Set {currentCount++}");
                relatedIds.AddRange(await mainContext.GenerationRelatedContents
                    .Where(x => loopSets.Contains(x.ContentTwo) && x.GenerationVersion == generationVersion)
                    .Select(x => x.ContentOne).Distinct().ToListAsync());
                relatedIds.AddRange(await mainContext.GenerationRelatedContents
                    .Where(x => loopSets.Contains(x.ContentTwo) && x.GenerationVersion == previousGenerationVersion)
                    .Select(x => x.ContentOne).Distinct().ToListAsync());
                relatedIds.AddRange(await mainContext.GenerationRelatedContents
                    .Where(x => loopSets.Contains(x.ContentOne) && x.GenerationVersion == generationVersion)
                    .Select(x => x.ContentTwo).Distinct().ToListAsync());
                relatedIds.AddRange(await mainContext.GenerationRelatedContents
                    .Where(x => loopSets.Contains(x.ContentOne) && x.GenerationVersion == previousGenerationVersion)
                    .Select(x => x.ContentTwo).Distinct().ToListAsync());

                relatedIds = relatedIds.Distinct().ToList();
            }

            contentChanges.AddRange(relatedIds.Distinct());

            contentChanges = contentChanges.Distinct().ToList();

            progress?.Report($"Adding {relatedIds.Distinct().Count()} Content Ids Generate");

            await mainContext.GenerationChangedContentIds.AddRangeAsync(contentChanges.Distinct()
                .Select(x => new GenerationChangedContentId {ContentId = x}).ToList());

            await mainContext.SaveChangesAsync();
        }

        public static async Task GenerateChangedDailyPhotoGalleries(DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var lastGeneration = await db.GenerationLogs.Where(x => x.GenerationVersion < generationVersion)
                .OrderByDescending(x => x.GenerationVersion).FirstOrDefaultAsync();

            if (lastGeneration == null)
            {
                progress?.Report("No Last Generation Found - generating All Daily Photo HTML.");
                await GenerateAllDailyPhotoGalleriesHtml(generationVersion, progress);
                return;
            }

            var lastGenerationDateList = await db.GenerationDailyPhotoLogs
                .Where(x => x.GenerationVersion == lastGeneration.GenerationVersion)
                .OrderByDescending(x => x.DailyPhotoDate).ToListAsync();

            if (!lastGenerationDateList.Any())
            {
                progress?.Report(
                    "No Daily Photo generation recorded in last generation - generating All Daily Photo HTML");
                await GenerateAllDailyPhotoGalleriesHtml(generationVersion, progress);
                return;
            }

            var changedPhotoDates = db.PhotoContents.Join(db.GenerationChangedContentIds, x => x.ContentId,
                x => x.ContentId, (x, y) => x).Select(x => x.PhotoCreatedOn.Date).Distinct().ToList();

            var datesToGenerate = new List<DateTime>();

            datesToGenerate.AddRange(changedPhotoDates);

            foreach (var loopChangedDates in changedPhotoDates)
            {
                var after = lastGenerationDateList.Where(x => x.DailyPhotoDate > loopChangedDates)
                    .OrderBy(x => x.DailyPhotoDate).FirstOrDefault();

                if (after != null && !datesToGenerate.Contains(after.DailyPhotoDate))
                    datesToGenerate.Add(after.DailyPhotoDate);

                var before = lastGenerationDateList.Where(x => x.DailyPhotoDate < loopChangedDates)
                    .OrderByDescending(x => x.DailyPhotoDate).FirstOrDefault();

                if (before != null && !datesToGenerate.Contains(before.DailyPhotoDate))
                    datesToGenerate.Add(before.DailyPhotoDate);
            }

            var resultantPages =
                await DailyPhotoPageGenerators.DailyPhotoGalleries(datesToGenerate, generationVersion, progress);
            resultantPages.ForEach(x => x.WriteLocalHtml());
        }

        public static async Task GenerateChangedListHtml(DateTime lastGenerationDateTime, DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            SearchListPageGenerators.WriteAllContentCommonSearchListHtml(generationVersion, progress);

            var db = await Db.Context();
            var filesChanged =
                db.FileContents.Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o)
                    .Any() || db.FileContents.Where(x => x.MainPicture != null).Join(db.GenerationChangedContentIds,
                    o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var filesDeleted = (await Db.DeletedFileContent()).Any(x => x.ContentVersion > lastGenerationDateTime);
            if (filesChanged || filesDeleted)
            {
                progress?.Report("Found File Changes - generating content list");
                SearchListPageGenerators.WriteFileContentListHtml(generationVersion, progress);
            }
            else
            {
                progress?.Report("Skipping File List Generation - no file or file main picture changes found");
            }

            var imagesChanged = db.ImageContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var imagesDeleted = (await Db.DeletedImageContent()).Any(x => x.ContentVersion > lastGenerationDateTime);
            if (imagesChanged || imagesDeleted)
                SearchListPageGenerators.WriteImageContentListHtml(generationVersion, progress);
            else progress?.Report("Skipping Image List Generation - no image changes found");

            var photosChanged = db.PhotoContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var photosDeleted = (await Db.DeletedPhotoContent()).Any(x => x.ContentVersion > lastGenerationDateTime);
            if (photosChanged || photosDeleted)
                SearchListPageGenerators.WritePhotoContentListHtml(generationVersion, progress);
            else progress?.Report("Skipping Photo List Generation - no image changes found");

            var postChanged =
                db.PostContents.Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o)
                    .Any() || db.PostContents.Where(x => x.MainPicture != null).Join(db.GenerationChangedContentIds,
                    o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var postsDeleted = (await Db.DeletedPostContent()).Any(x => x.ContentVersion > lastGenerationDateTime);
            if (postChanged || postsDeleted)
                SearchListPageGenerators.WritePostContentListHtml(generationVersion, progress);
            else progress?.Report("Skipping Post List Generation - no file or file main picture changes found");

            var pointChanged =
                db.PointContents.Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o)
                    .Any() || db.PointContents.Where(x => x.MainPicture != null).Join(db.GenerationChangedContentIds,
                    o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var pointsDeleted = (await Db.DeletedPointContent()).Any(x => x.ContentVersion > lastGenerationDateTime);
            if (pointChanged || pointsDeleted)
                SearchListPageGenerators.WritePointContentListHtml(generationVersion, progress);
            else progress?.Report("Skipping Point List Generation - no file or file main picture changes found");

            var notesChanged = db.NoteContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var notesDeleted = (await Db.DeletedNoteContent()).Any(x => x.ContentVersion > lastGenerationDateTime);
            if (notesChanged || notesDeleted)
                SearchListPageGenerators.WriteNoteContentListHtml(generationVersion, progress);
            else progress?.Report("Skipping Note List Generation - no image changes found");

            var linkListPage = new LinkListPage {GenerationVersion = generationVersion};
            linkListPage.WriteLocalHtmlRssAndJson();
            progress?.Report("Creating Link List Json");
            Export.WriteLinkListJson();
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
            var db = await Db.Context();

            var lastGeneration = await db.GenerationLogs.Where(x => x.GenerationVersion < generationVersion)
                .OrderByDescending(x => x.GenerationVersion).FirstOrDefaultAsync();

            if (lastGeneration == null)
            {
                progress?.Report("No Last Generation Found - generating All Tag Html.");
                GenerateAllTagHtml(generationVersion, progress);
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
                SearchListPageGenerators.WriteTagList(generationVersion, progress);
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
                    {typeof(GenerationTagLog), new[] {"RelatedContentId"}}
                };
                tagCompareLogic.Config.CollectionMatchingSpec = spec;
                tagCompareLogic.Config.MembersToInclude.Add("RelatedContentId");

                return tagCompareLogic;
            }

            async Task GenerateTag(DateTime dateTime, PointlessWaymarksContext pointlessWaymarksContext,
                CompareLogic tagCompareLogic, string tag, IProgress<string>? generateProgress)
            {
                var contentLastGeneration = await pointlessWaymarksContext.GenerationTagLogs.Where(x =>
                        x.GenerationVersion == lastGeneration.GenerationVersion && x.TagSlug == tag)
                    .OrderBy(x => x.RelatedContentId).ToListAsync();

                var contentThisGeneration = await pointlessWaymarksContext.GenerationTagLogs
                    .Where(x => x.GenerationVersion == dateTime && x.TagSlug == tag).OrderBy(x => x.RelatedContentId)
                    .ToListAsync();

                var generationComparisonResults = tagCompareLogic.Compare(contentLastGeneration, contentThisGeneration);

                //List of content is not the same - page must be rebuilt
                if (!generationComparisonResults.AreEqual)
                {
                    generateProgress?.Report($"New content found for tag {tag} - creating page");
                    var contentToWrite =
                        await pointlessWaymarksContext.ContentFromContentIds(contentThisGeneration
                            .Select(x => x.RelatedContentId).ToList());
                    SearchListPageGenerators.WriteTagPage(tag, contentToWrite, dateTime, generateProgress);
                    return;
                }

                //Direct content Changes
                var primaryContentChanges = await pointlessWaymarksContext.GenerationChangedContentIds.AnyAsync(x =>
                    contentThisGeneration.Select(y => y.RelatedContentId).Contains(x.ContentId));

                var directTagContent =
                    await pointlessWaymarksContext.ContentFromContentIds(contentThisGeneration
                        .Select(x => x.RelatedContentId).ToList());

                //Main Image changes
                var mainImageContentIds = directTagContent.Select(x => Db.MainImageContentIdIfPresent(x))
                    .Where(x => x != null).Cast<Guid>().ToList();

                var mainImageContentChanges = await pointlessWaymarksContext.GenerationChangedContentIds.AnyAsync(x =>
                    mainImageContentIds.Contains(x.ContentId));

                if (primaryContentChanges || mainImageContentChanges)
                {
                    generateProgress?.Report($"Content Changes found for tag {tag} - creating page");

                    SearchListPageGenerators.WriteTagPage(tag, directTagContent, dateTime, generateProgress);

                    return;
                }

                generateProgress?.Report($"No Changes for tag {tag}");
            }

            //Evaluate each Tag - the tags list is a standard search list showing summary and main image in addition to changes to the linked content also check for changes
            //to the linked content contents...
            var allCurrentTags = db.GenerationTagLogs.Where(x => x.GenerationVersion == generationVersion)
                .Where(x => x.TagSlug != null).Select(x => x.TagSlug!).Distinct().OrderBy(x => x).ToList();

            var partitionedTags = allCurrentTags.Split(10);

            var allTasks = new List<Task>();

            foreach (var loopPartitions in partitionedTags)
                allTasks.Add(Task.Run(async () =>
                {
                    var partitionDb = await Db.Context();
                    var partitionCompareLogic = GetTagCompareLogic();
                    foreach (var loopTag in loopPartitions)
                        await GenerateTag(generationVersion, partitionDb, partitionCompareLogic, loopTag, progress);
                }));

            await Task.WhenAll(allTasks);
        }

        public static async Task<List<GenerationReturn>> GenerateChangedToHtml(IProgress<string>? progress = null)
        {
            await CleanupGenerationInformation(progress);

            var db = await Db.Context();

            //Get and check the last generation - if there is no value then generate all which should create a valid value for the next
            //run
            var generationVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime();
            var lastGenerationValues = db.GenerationLogs.Where(x => x.GenerationVersion < generationVersion)
                .OrderByDescending(x => x.GenerationVersion).FirstOrDefault();

            if (lastGenerationValues == null || string.IsNullOrWhiteSpace(lastGenerationValues.GenerationSettings))
            {
                progress?.Report("No value for Last Generation in Settings - Generating All HTML");

                return await GenerateAllHtml(progress);
            }

            progress?.Report($"Last Generation - {lastGenerationValues.GenerationVersion}");
            var lastGenerationDateTime = lastGenerationValues.GenerationVersion;

            //The menu is currently written to all pages - if there are changes then generate all
            var menuUpdates = await db.MenuLinks.AnyAsync(x => x.ContentVersion > lastGenerationDateTime);

            if (menuUpdates)
            {
                progress?.Report("Menu Updates detected - menu updates impact all pages, generating All HTML");

                return await GenerateAllHtml(progress);
            }

            //If the generation settings have changed trigger a full rebuild
            var lastGenerationSettings =
                JsonSerializer.Deserialize<UserSettingsGenerationValues>(lastGenerationValues.GenerationSettings);

            var currentGenerationSettings = UserSettingsSingleton.CurrentSettings().GenerationValues();

            var compareLogic = new CompareLogic(new ComparisonConfig {MaxDifferences = 20});
            var generationSettingsComparison = compareLogic.Compare(lastGenerationSettings, currentGenerationSettings);

            var compareReport = new UserFriendlyReport();
            var generationSettingsComparisonDifferences =
                compareReport.OutputString(generationSettingsComparison.Differences);

            if (!generationSettingsComparison.AreEqual)
            {
                progress?.Report(
                    $"Generation Settings Changes detected - generating All HTML: {Environment.NewLine}{generationSettingsComparisonDifferences}");

                return await GenerateAllHtml(progress);
            }

            progress?.Report("Write Site Resources");
            await FileManagement.WriteSiteResourcesToGeneratedSite(progress);

            progress?.Report($"Generation HTML based on changes after UTC - {lastGenerationValues.GenerationVersion}");

            await RelatedContentReference.GenerateRelatedContentDbTable(generationVersion, progress);
            await GenerateChangedContentIdReferences(lastGenerationDateTime, generationVersion, progress);
            await SetupTagGenerationDbData(generationVersion, progress);
            await SetupDailyPhotoGenerationDbData(generationVersion, progress);

            if (!await db.GenerationChangedContentIds.AnyAsync())
                progress?.Report("No Changes Detected - ending HTML generation.");

            await GenerateChangeFilteredPhotoHtml(generationVersion, progress);
            await GenerateChangeFilteredImageHtml(generationVersion, progress);

            var changedPartsList = new List<Task>
            {
                GenerateChangeFilteredFileHtml(generationVersion, progress),
                GenerateChangeFilteredGeoJsonHtml(generationVersion, progress),
                GenerateChangeFilteredLineHtml(generationVersion, progress),
                GenerateChangeFilteredMapData(generationVersion, progress),
                GenerateChangeFilteredNoteHtml(generationVersion, progress),
                GenerateChangeFilteredPointHtml(generationVersion, progress),
                GenerateChangeFilteredPostHtml(generationVersion, progress)
            };

            await Task.WhenAll(changedPartsList);

            await GenerateMainFeedContent(generationVersion, progress);

            var hasDirectPhotoChanges = db.PhotoContents.Join(db.GenerationChangedContentIds, o => o.ContentId,
                i => i.ContentId, (o, i) => o.PhotoCreatedOn).Any();
            var hasRelatedPhotoChanges = db.PhotoContents.Join(db.GenerationRelatedContents, o => o.ContentId,
                i => i.ContentTwo, (o, i) => o.PhotoCreatedOn).Any();
            var hasDeletedPhotoChanges =
                (await Db.DeletedPhotoContent()).Any(x => x.ContentVersion > lastGenerationDateTime);

            if (hasDirectPhotoChanges || hasRelatedPhotoChanges || hasDeletedPhotoChanges)
                await GenerateChangedDailyPhotoGalleries(generationVersion, progress);
            else
                progress?.Report(
                    "No changes to Photos directly or thru related content - skipping Daily Photo Page generation.");

            if (hasDirectPhotoChanges || hasDeletedPhotoChanges)
                await GenerateCameraRollHtml(generationVersion, progress);
            else progress?.Report("No changes to Photo content - skipping Photo Gallery generation.");

            var tagAndListTasks = new List<Task>
            {
                GenerateChangedTagHtml(generationVersion, progress),
                GenerateChangedListHtml(lastGenerationDateTime, generationVersion, progress),
                Task.Run(() => GenerateAllUtilityJson(progress)),
                Task.Run(() => GenerateIndex(generationVersion, progress))
            };

            await Task.WhenAll(tagAndListTasks);

            progress?.Report(
                $"Generation Complete - writing {generationVersion} as Last Generation UTC into db Generation Log");

            await Db.SaveGenerationLogAndRecordSettings(generationVersion);

            var allChangedContentCommon =
                (await db.ContentCommonShellFromContentIds(await db.GenerationChangedContentIds.Select(x => x.ContentId)
                    .ToListAsync())).Cast<IContentCommon>().ToList();

            return await CommonContentValidation.CheckForBadContentReferences(allChangedContentCommon, db, progress);
        }

        public static async Task GenerateChangeFilteredFileHtml(DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.FileContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Files to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var htmlModel = new SingleFilePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);
            });
        }

        public static async Task GenerateChangeFilteredGeoJsonHtml(DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.GeoJsonContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} GeoJson Entries to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var htmlModel = new SingleGeoJsonPage(loopItem) {GenerationVersion = generationVersion};
                await htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static async Task GenerateChangeFilteredImageHtml(DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.ImageContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var totalCount = allItems.Count;

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var htmlModel = new SingleImagePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static async Task GenerateChangeFilteredLineHtml(DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.LineContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Lines to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var htmlModel = new SingleLinePage(loopItem) {GenerationVersion = generationVersion};
                await htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static async Task GenerateChangeFilteredMapData(DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.MapComponents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Map Components to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing Data for {loopItem.Title}");

                await MapData.WriteJsonData(loopItem.ContentId);
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static async Task GenerateChangeFilteredNoteHtml(DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.NoteContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for Note Dated {loopItem.CreatedOn:d}");

                var htmlModel = new SingleNotePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);
            });
        }

        public static async Task GenerateChangeFilteredPhotoHtml(DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.PhotoContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Photos to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var htmlModel = new SinglePhotoPage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static async Task GenerateChangeFilteredPointHtml(DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.PointContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Points to Generate");

            if (allItems.Count > 0)
                await GenerateAllPointHtml(generationVersion, progress);

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title}");

                var loopItemAndDetails = await Db.PointAndPointDetails(loopItem.ContentId);

                if (loopItemAndDetails == null) return;

                var htmlModel = new SinglePointPage(loopItemAndDetails) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);
            });
        }

        public static async Task GenerateChangeFilteredPostHtml(DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            var allItems = await db.PostContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            await allItems.AsyncParallelForEach(async loopItem =>
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SinglePostPage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            });
        }

        public static async Task GenerateHtmlFromCommonContent(IContentCommon content, DateTime generationVersion,
            IProgress<string>? progress = null)
        {
            //!!Content Type List!!
            switch (content)
            {
                case FileContent file:
                    FileGenerator.GenerateHtml(file, generationVersion, progress);
                    break;
                case GeoJsonContent geoJson:
                    await GeoJsonGenerator.GenerateHtml(geoJson, generationVersion, progress);
                    break;
                case ImageContent image:
                    ImageGenerator.GenerateHtml(image, generationVersion, progress);
                    break;
                case LineContent line:
                    await LineGenerator.GenerateHtml(line, generationVersion, progress);
                    break;
                case NoteContent note:
                    NoteGenerator.GenerateHtml(note, generationVersion, progress);
                    break;
                case PhotoContent photo:
                    PhotoGenerator.GenerateHtml(photo, generationVersion, progress);
                    break;
                case PointContent point:
                    var dto = await Db.PointAndPointDetails(point.ContentId);
                    PointGenerator.GenerateHtml(dto!, generationVersion, progress);
                    break;
                case PostContent post:
                    PostGenerator.GenerateHtml(post, generationVersion, progress);
                    break;
                case PointContentDto pointDto:
                    PointGenerator.GenerateHtml(pointDto, generationVersion, progress);
                    break;
                default:
                    throw new Exception("The method GenerateHtmlFromCommonContent didn't find a content type match");
            }
        }

        public static void GenerateIndex(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var index = new IndexPage {GenerationVersion = generationVersion};
            index.WriteLocalHtml();
        }

        public static async Task GenerateMainFeedContent(DateTime generationVersion, IProgress<string>? progress = null)
        {
            //TODO: This current regenerates the Main Feed Content in order to make sure all previous/next content links are correct - this
            //could be improved to just detect changes.
            var mainFeedContent = (await Db.MainFeedCommonContent()).ToList();

            progress?.Report($"{mainFeedContent.Count} Main Feed Content Entries to Check");

            if (!mainFeedContent.Any()) return;

            var db = await Db.Context();

            foreach (var loopMains in mainFeedContent)
            {
                if (await db.GenerationChangedContentIds.AnyAsync(x => x.ContentId == loopMains.ContentId))
                {
                    progress?.Report($"Main Feed - {loopMains.Title} already in Changed List");
                    continue;
                }

                progress?.Report($"Main Feed - Generating {loopMains.Title}");
                await GenerateHtmlFromCommonContent(loopMains, generationVersion, progress);
            }
        }

        public static async Task SetupDailyPhotoGenerationDbData(DateTime currentGenerationVersion,
            IProgress<string>? progress = null)
        {
            var db = await Db.Context();

            progress?.Report("Getting list of all Photo Dates and Content");

            var allPhotoInfo = await db.PhotoContents.AsNoTracking().ToListAsync();

            var datesAndContent = allPhotoInfo.GroupBy(x => x.PhotoCreatedOn.Date)
                .Select(x => new {date = x.Key, contentIds = x.Select(y => y.ContentId)})
                .OrderByDescending(x => x.date).ToList();

            progress?.Report("Processing Photo Dates and Content");

            foreach (var loopDates in datesAndContent)
            foreach (var loopContent in loopDates.contentIds)
                await db.GenerationDailyPhotoLogs.AddAsync(new GenerationDailyPhotoLog
                {
                    DailyPhotoDate = loopDates.date,
                    GenerationVersion = currentGenerationVersion,
                    RelatedContentId = loopContent
                });

            progress?.Report("Saving Photo Dates and Content to db");

            await db.SaveChangesAsync(true);
        }


        public static async Task SetupTagGenerationDbData(DateTime currentGenerationVersion,
            IProgress<string>? progress = null)
        {
            var tagData = await Db.TagSlugsAndContentList(true, false, progress);

            var db = await Db.Context();

            foreach (var (tag, contentObjects) in tagData)
            foreach (var loopContent in contentObjects)
                await db.GenerationTagLogs.AddAsync(new GenerationTagLog
                {
                    GenerationVersion = currentGenerationVersion,
                    RelatedContentId = loopContent.ContentId,
                    TagSlug = tag
                });

            await db.SaveChangesAsync(true);
        }
    }
}