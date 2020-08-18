using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.Reports;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.FileHtml;
using PointlessWaymarksCmsData.Html.ImageHtml;
using PointlessWaymarksCmsData.Html.IndexHtml;
using PointlessWaymarksCmsData.Html.LinkListHtml;
using PointlessWaymarksCmsData.Html.NoteHtml;
using PointlessWaymarksCmsData.Html.PhotoGalleryHtml;
using PointlessWaymarksCmsData.Html.PhotoHtml;
using PointlessWaymarksCmsData.Html.PostHtml;
using PointlessWaymarksCmsData.Html.SearchListHtml;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksCmsData.Html
{
    public static class GenerationGroups
    {
        public static async Task CleanupGenerationInformation(IProgress<string> progress)
        {
            progress?.Report("Cleaning up Generation Log Information");

            var db = await Db.Context();

            var olderGenerationLogs =
                await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).Skip(100).ToListAsync();

            progress?.Report($"Keeping Top 100 Logs, Found {olderGenerationLogs.Count} logs to remove");

            db.GenerationLogs.RemoveRange(olderGenerationLogs);

            await db.SaveChangesAsync(true);

            var currentGenerationVersions = await db.GenerationLogs.Select(x => x.GenerationVersion).ToListAsync();

            var olderPhotoGenerationInformationToRemove = await db.GenerationDailyPhotoLogs
                .Where(x => !currentGenerationVersions.Contains(x.GenerationVersion)).ToListAsync();

            progress?.Report($"Found {olderPhotoGenerationInformationToRemove.Count} photo generation logs to remove.");

            db.GenerationDailyPhotoLogs.RemoveRange(olderPhotoGenerationInformationToRemove);

            var olderTagGenerationInformationToRemove = await db.GenerationTagLogs
                .Where(x => !currentGenerationVersions.Contains(x.GenerationVersion)).ToListAsync();

            progress?.Report($"Found {olderTagGenerationInformationToRemove.Count} tag generation logs to remove.");

            db.GenerationTagLogs.RemoveRange(olderTagGenerationInformationToRemove);

            progress?.Report("Saving Photo and Tag log changes");

            await db.SaveChangesAsync(true);
        }

        public static async Task GenerateAllDailyPhotoGalleriesHtml(DateTime? generationVersion,
            IProgress<string> progress)
        {
            var allPages = await DailyPhotoPageGenerators.DailyPhotoGalleries(generationVersion, progress);

            allPages.ForEach(x => x.WriteLocalHtml());
        }

        public static async Task GenerateAllFileHtml(DateTime? generationVersion, IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.FileContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Files to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SingleFilePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);

                loopCount++;
            }
        }

        public static async Task GenerateAllHtml(IProgress<string> progress)
        {
            await CleanupGenerationInformation(progress);

            var generationVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime();

            await SetupTagGenerationDbData(generationVersion, progress);
            await SetupDailyPhotoGenerationDbData(generationVersion, progress);

            await GenerateAllPhotoHtml(generationVersion, progress);
            await GenerateAllImageHtml(generationVersion, progress);
            await GenerateAllFileHtml(generationVersion, progress);
            await GenerateAllNoteHtml(generationVersion, progress);
            await GenerateAllPostHtml(generationVersion, progress);
            await GenerateAllDailyPhotoGalleriesHtml(generationVersion, progress);
            await GenerateCameraRollHtml(generationVersion, progress);
            GenerateAllTagHtml(generationVersion, progress);
            GenerateAllListHtml(generationVersion, progress);
            GenerateAllUtilityJson(progress);
            GenerateIndex(generationVersion, progress);

            progress?.Report(
                $"Generation Complete - Writing Generation Date Time of UTC {generationVersion} in Db Generation log as Last Generation");

            var db = await Db.Context();

            var serializedSettings =
                JsonSerializer.Serialize(UserSettingsSingleton.CurrentSettings().GenerationValues());
            var dbGenerationRecord = new GenerationLog
            {
                GenerationSettings = serializedSettings, GenerationVersion = generationVersion
            };
            await db.GenerationLogs.AddAsync(dbGenerationRecord);
            await db.SaveChangesAsync(true);
        }


        public static async Task GenerateAllImageHtml(DateTime? generationVersion, IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.ImageContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Images to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SingleImagePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static void GenerateAllListHtml(DateTime? generationVersion, IProgress<string> progress)
        {
            SearchListPageGenerators.WriteAllContentCommonSearchListHtml(generationVersion);
            SearchListPageGenerators.WriteFileContentListHtml(generationVersion);
            SearchListPageGenerators.WriteImageContentListHtml(generationVersion);
            SearchListPageGenerators.WritePhotoContentListHtml(generationVersion);
            SearchListPageGenerators.WritePostContentListHtml(generationVersion);
            SearchListPageGenerators.WriteNoteContentListHtml(generationVersion);

            var linkListPage = new LinkListPage {GenerationVersion = generationVersion};
            linkListPage.WriteLocalHtmlRssAndJson();
            progress?.Report("Creating Link List Json");
            Export.WriteLinkListJson();
        }

        public static async Task GenerateAllNoteHtml(DateTime? generationVersion, IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.NoteContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for Note Dated {loopItem.CreatedOn:d} - {loopCount} of {totalCount}");

                var htmlModel = new SingleNotePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);

                loopCount++;
            }
        }

        public static async Task GenerateAllPhotoHtml(DateTime? generationVersion, IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.PhotoContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Photos to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SinglePhotoPage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static async Task GenerateAllPostHtml(DateTime? generationVersion, IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.PostContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SinglePostPage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static void GenerateAllTagHtml(DateTime? generationVersion, IProgress<string> progress)
        {
            SearchListPageGenerators.WriteTagListAndTagPages(generationVersion, progress);
        }

        public static void GenerateAllUtilityJson(IProgress<string> progress)
        {
            progress?.Report("Creating Menu Links Json");
            Export.WriteMenuLinksJson();

            progress?.Report("Creating Tag Exclusion Json");
            Export.WriteTagExclusionsJson();
        }

        public static async Task GenerateCameraRollHtml(DateTime? generationVersion, IProgress<string> progress)
        {
            var cameraRollPage = await CameraRollGalleryPageGenerator.CameraRoll(generationVersion, progress);
            cameraRollPage.WriteLocalHtml();
        }

        public static async Task GenerateChangedContentIdReferences(DateTime contentAfter, IProgress<string> progress)
        {
            var db = await Db.Context();

            progress?.Report("Clearing GenerationChangedContentIds Table");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM [" + "GenerationChangedContentIds" + "];");

            var files = await db.FileContents.Where(x => x.ContentVersion >= contentAfter).Select(x => x.ContentId)
                .ToListAsync();
            progress?.Report($"Found {files.Count} File Content Entries Changed After {contentAfter}");

            var images = await db.ImageContents.Where(x => x.ContentVersion >= contentAfter).Select(x => x.ContentId)
                .ToListAsync();
            progress?.Report($"Found {images.Count} Image Content Entries Changed After {contentAfter}");

            var links = await db.LinkContents.Where(x => x.ContentVersion >= contentAfter).Select(x => x.ContentId)
                .ToListAsync();
            progress?.Report($"Found {links.Count} Link Content Entries Changed After {contentAfter}");

            var notes = await db.NoteContents.Where(x => x.ContentVersion >= contentAfter).Select(x => x.ContentId)
                .ToListAsync();
            progress?.Report($"Found {notes.Count} Note Content Entries Changed After {contentAfter}");

            var photos = await db.PhotoContents.Where(x => x.ContentVersion >= contentAfter).Select(x => x.ContentId)
                .ToListAsync();
            progress?.Report($"Found {photos.Count} Photo Content Entries Changed After {contentAfter}");

            var posts = await db.PostContents.Where(x => x.ContentVersion >= contentAfter).Select(x => x.ContentId)
                .ToListAsync();
            progress?.Report($"Found {posts.Count} Post Content Entries Changed After {contentAfter}");

            var contentChanges = files.Concat(images).Concat(links).Concat(notes).Concat(photos).Concat(posts).ToList();

            var originalContentSets = contentChanges.Partition(500).ToList();

            var relatedIds = new List<Guid>();

            progress?.Report($"Processing {originalContentSets.Count} Sets of Changed Content for Related Content");

            var currentCount = 1;

            foreach (var loopSets in originalContentSets)
            {
                progress?.Report($"Processing Related Content Set {currentCount++}");
                relatedIds.AddRange(await db.GenerationRelatedContents.Where(x => loopSets.Contains(x.ContentTwo))
                    .Select(x => x.ContentOne).Distinct().ToListAsync());
            }

            contentChanges.AddRange(relatedIds.Distinct());

            progress?.Report($"Adding {relatedIds.Distinct().Count()} Content Ids Generate");

            await db.GenerationChangedContentIds.AddRangeAsync(contentChanges.Distinct()
                .Select(x => new GenerationChangedContentId {ContentId = x}).ToList());

            await db.SaveChangesAsync();
        }

        public static async Task GenerateChangedDailyPhotoGalleries(DateTime generationVersion,
            IProgress<string> progress)
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
            IProgress<string> progress)
        {
            SearchListPageGenerators.WriteAllContentCommonSearchListHtml(generationVersion);

            var db = await Db.Context();
            var filesChanged =
                db.FileContents.Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o)
                    .Any() || db.FileContents.Where(x => x.MainPicture != null).Join(db.GenerationChangedContentIds,
                    o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var filesDeleted = (await Db.DeletedFileContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);
            if (filesChanged || filesDeleted) SearchListPageGenerators.WriteFileContentListHtml(generationVersion);
            else progress?.Report("Skipping File List Generation - no file or file main picture changes found");

            var imagesChanged = db.ImageContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var imagesDeleted = (await Db.DeletedImageContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);
            if (imagesChanged || imagesDeleted) SearchListPageGenerators.WriteImageContentListHtml(generationVersion);
            else progress?.Report("Skipping Image List Generation - no image changes found");

            var photosChanged = db.PhotoContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var photosDeleted = (await Db.DeletedPhotoContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);
            if (photosChanged || photosDeleted) SearchListPageGenerators.WritePhotoContentListHtml(generationVersion);
            else progress?.Report("Skipping Photo List Generation - no image changes found");

            var postChanged =
                db.PostContents.Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o)
                    .Any() || db.PostContents.Where(x => x.MainPicture != null).Join(db.GenerationChangedContentIds,
                    o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var postsDeleted = (await Db.DeletedPostContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);
            if (postChanged || postsDeleted) SearchListPageGenerators.WritePostContentListHtml(generationVersion);
            else progress?.Report("Skipping Post List Generation - no file or file main picture changes found");

            var notesChanged = db.NoteContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var notesDeleted = (await Db.DeletedNoteContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);
            if (notesChanged || notesDeleted) SearchListPageGenerators.WriteNoteContentListHtml(generationVersion);
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
        public static async Task GenerateChangedTagHtml(DateTime generationVersion, IProgress<string> progress)
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
                .Where(x => x.GenerationVersion == lastGeneration.GenerationVersion && !x.TagIsExcludedFromSearch)
                .Select(x => x.TagSlug).Distinct().OrderBy(x => x).ToList();

            progress?.Report(
                $"Found {searchTagsLastGeneration.Count} search tags from the {lastGeneration.GenerationVersion} generation");


            var searchTagsThisGeneration = db.GenerationTagLogs
                .Where(x => x.GenerationVersion == generationVersion && !x.TagIsExcludedFromSearch)
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

            //Evaluate each Tag - the tags list is a standard search list showing summary and main image in addition to changes to the linked content also check for changes
            //to the linked content contents...
            var allCurrentTags = db.GenerationTagLogs.Where(x => x.GenerationVersion == generationVersion)
                .Select(x => x.TagSlug).Distinct().ToList();

            var tagCompareLogic = new CompareLogic();
            var spec = new Dictionary<Type, IEnumerable<string>>
            {
                {typeof(GenerationTagLog), new[] {"RelatedContentId"}}
            };
            tagCompareLogic.Config.CollectionMatchingSpec = spec;
            tagCompareLogic.Config.MembersToInclude.Add("RelatedContentId");

            foreach (var loopTags in allCurrentTags)
            {
                var contentLastGeneration = await db.GenerationTagLogs.Where(x =>
                        x.GenerationVersion == lastGeneration.GenerationVersion && x.TagSlug == loopTags)
                    .OrderBy(x => x.RelatedContentId).ToListAsync();

                var contentThisGeneration = await db.GenerationTagLogs.Where(x =>
                        x.GenerationVersion == generationVersion && x.TagSlug == loopTags)
                    .OrderBy(x => x.RelatedContentId)
                    .ToListAsync();

                var generationComparisonResults = tagCompareLogic.Compare(contentLastGeneration, contentThisGeneration);

                //List of content is not the same - page must be rebuilt
                if (!generationComparisonResults.AreEqual)
                {
                    progress?.Report($"New content found for tag {loopTags} - creating page");
                    var contentToWrite =
                        db.ContentFromContentIds(contentThisGeneration.Select(x => x.RelatedContentId).ToList());
                    SearchListPageGenerators.WriteTagPage(loopTags, contentToWrite, generationVersion);
                    continue;
                }

                //Direct content Changes
                var primaryContentChanges = await db.GenerationChangedContentIds.AnyAsync(x =>
                    contentThisGeneration.Select(y => y.RelatedContentId).Contains(x.ContentId));

                var directTagContent =
                    db.ContentFromContentIds(contentThisGeneration.Select(x => x.RelatedContentId).ToList());

                //Main Image changes
                var mainImageContentIds = directTagContent.Select(x => Db.MainImageContentIdIfPresent(x))
                    .Where(x => x != null).Cast<Guid>().ToList();

                var mainImageContentChanges = await db.GenerationChangedContentIds.AnyAsync(x =>
                    mainImageContentIds.Contains(x.ContentId));

                if (primaryContentChanges || mainImageContentChanges)
                {
                    progress?.Report($"Content Changes found for tag {loopTags} - creating page");

                    SearchListPageGenerators.WriteTagPage(loopTags, directTagContent, generationVersion);
                }

                progress?.Report($"No Changes for tag {loopTags}");
            }
        }

        public static async Task GenerateChangedToHtml(IProgress<string> progress)
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

                await GenerateAllHtml(progress);
                return;
            }

            progress?.Report($"Last Generation - {lastGenerationValues.GenerationVersion}");
            var lastGenerationDateTime = lastGenerationValues.GenerationVersion;

            //The menu is currently written to all pages - if there are changes then generate all
            var menuUpdates = await db.MenuLinks.AnyAsync(x => x.ContentVersion > lastGenerationDateTime);

            if (menuUpdates)
            {
                progress?.Report("Menu Updates detected - menu updates impact all pages, generating All HTML");

                await GenerateAllHtml(progress);
                return;
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

                await GenerateAllHtml(progress);
                return;
            }

            progress?.Report($"Generation HTML based on changes after UTC - {lastGenerationValues.GenerationVersion}");

            await RelatedContentReference.GenerateRelatedContentDbTable(lastGenerationDateTime, progress);
            await GenerateChangedContentIdReferences(lastGenerationDateTime, progress);
            await SetupTagGenerationDbData(generationVersion, progress);
            await SetupDailyPhotoGenerationDbData(generationVersion, progress);

            if (!(await db.GenerationChangedContentIds.AnyAsync()))
                progress?.Report("No Changes Detected - ending HTML generation.");

            await GenerateChangeFilteredPhotoHtml(generationVersion, progress);
            await GenerateChangeFilteredImageHtml(generationVersion, progress);
            await GenerateChangeFilteredFileHtml(generationVersion, progress);
            await GenerateChangeFilteredNoteHtml(generationVersion, progress);
            await GenerateChangeFilteredPostHtml(generationVersion, progress);

            var hasDirectPhotoChanges = db.PhotoContents.Join(db.GenerationChangedContentIds, o => o.ContentId,
                i => i.ContentId, (o, i) => o.PhotoCreatedOn).Any();
            var hasRelatedPhotoChanges = db.PhotoContents.Join(db.GenerationRelatedContents, o => o.ContentId,
                i => i.ContentTwo, (o, i) => o.PhotoCreatedOn).Any();
            var hasDeletedPhotoChanges =
                (await Db.DeletedPhotoContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);

            if (hasDirectPhotoChanges || hasRelatedPhotoChanges || hasDeletedPhotoChanges)
                await GenerateChangedDailyPhotoGalleries(lastGenerationDateTime, progress);
            else
                progress?.Report(
                    "No changes to Photos directly or thru related content - skipping Daily Photo Page generation.");

            if (hasDirectPhotoChanges || hasDeletedPhotoChanges)
                await GenerateCameraRollHtml(generationVersion, progress);
            else progress?.Report("No changes to Photo content - skipping Photo Gallery generation.");

            await GenerateChangedTagHtml(generationVersion, progress);
            await GenerateChangedListHtml(lastGenerationDateTime, generationVersion, progress);
            GenerateAllUtilityJson(progress);
            GenerateIndex(generationVersion, progress);

            progress?.Report(
                $"Generation Complete - writing {generationVersion} as Last Generation UTC into db Generation Log");

            var serializedSettings =
                JsonSerializer.Serialize(UserSettingsSingleton.CurrentSettings().GenerationValues());
            var dbGenerationRecord = new GenerationLog
            {
                GenerationSettings = serializedSettings, GenerationVersion = generationVersion
            };
            await db.GenerationLogs.AddAsync(dbGenerationRecord);
            await db.SaveChangesAsync(true);
        }

        public static async Task GenerateChangeFilteredFileHtml(DateTime generationVersion, IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.FileContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Files to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SingleFilePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);

                loopCount++;
            }
        }

        public static async Task GenerateChangeFilteredImageHtml(DateTime generationVersion, IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.ImageContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Images to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SingleImagePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static async Task GenerateChangeFilteredNoteHtml(DateTime generationVersion, IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.NoteContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for Note Dated {loopItem.CreatedOn:d} - {loopCount} of {totalCount}");

                var htmlModel = new SingleNotePage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);

                loopCount++;
            }
        }

        public static async Task GenerateChangeFilteredPhotoHtml(DateTime generationVersion, IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.PhotoContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Photos to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SinglePhotoPage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static async Task GenerateChangeFilteredPostHtml(DateTime generationVersion, IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.PostContents
                .Join(db.GenerationChangedContentIds, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SinglePostPage(loopItem) {GenerationVersion = generationVersion};
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static void GenerateIndex(DateTime? generationVersion, IProgress<string> progress)
        {
            var index = new IndexPage {GenerationVersion = generationVersion};
            index.WriteLocalHtml();
        }

        public static async Task SetupDailyPhotoGenerationDbData(DateTime currentGenerationVersion,
            IProgress<string> progress)
        {
            currentGenerationVersion = currentGenerationVersion.TrimDateTimeToSeconds();

            var db = await Db.Context();

            progress?.Report("Getting list of all Photo Dates and Content");

            var allPhotoInfo = await db.PhotoContents.AsNoTracking().ToListAsync();

            var datesAndContent = allPhotoInfo.GroupBy(x => x.PhotoCreatedOn.Date)
                .Select(x => new {date = x.Key, contentIds = x.Select(y => y.ContentId)}).OrderByDescending(x => x.date)
                .ToList();

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


        public static async Task SetupTagGenerationDbData(DateTime currentGenerationVersion, IProgress<string> progress)
        {
            currentGenerationVersion = currentGenerationVersion.TrimDateTimeToSeconds();

            var tagData = await Db.TagAndContentList(true, progress);

            var db = await Db.Context();

            foreach (var loopTags in tagData)
            foreach (var loopContent in loopTags.contentObjects)
                await db.GenerationTagLogs.AddAsync(new GenerationTagLog
                {
                    GenerationVersion = currentGenerationVersion,
                    RelatedContentId = loopContent.ContentId,
                    TagSlug = loopTags.tag
                });

            await db.SaveChangesAsync(true);
        }
    }
}