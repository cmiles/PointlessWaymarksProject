using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public static async Task GenerateAllDailyPhotoGalleriesHtml(IProgress<string> progress)
        {
            var allPages = await DailyPhotoPageGenerators.DailyPhotoGalleries(progress);

            allPages.ForEach(x => x.WriteLocalHtml());
        }

        public static async Task GenerateAllFileHtml(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.FileContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Files to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SingleFilePage(loopItem);
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);

                loopCount++;
            }
        }

        public static async Task GenerateAllHtml(IProgress<string> progress)
        {
            var frozenNow = DateTime.Now.ToUniversalTime();

            await GenerateAllPhotoHtml(progress);
            await GenerateAllImageHtml(progress);
            await GenerateAllFileHtml(progress);
            await GenerateAllNoteHtml(progress);
            await GenerateAllPostHtml(progress);
            await GenerateAllDailyPhotoGalleriesHtml(progress);
            await GenerateCameraRollHtml(progress);
            GenerateAllTagHtml(progress);
            GenerateAllListHtml(progress);
            GenerateAllUtilityJson(progress);
            GenerateIndex(progress);

            progress?.Report(
                $"Generation Complete - Writing Generation Date Time of UTC {frozenNow} in settings as Last Generation");
            UserSettingsSingleton.CurrentSettings().LastGenerationUtc = frozenNow;
            await UserSettingsSingleton.CurrentSettings().WriteSettings();
        }


        public static async Task GenerateAllImageHtml(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.ImageContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Images to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SingleImagePage(loopItem);
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static void GenerateAllListHtml(IProgress<string> progress)
        {
            SearchListPageGenerators.WriteAllContentCommonSearchListHtml();
            SearchListPageGenerators.WriteFileContentListHtml();
            SearchListPageGenerators.WriteImageContentListHtml();
            SearchListPageGenerators.WritePhotoContentListHtml();
            SearchListPageGenerators.WritePostContentListHtml();
            SearchListPageGenerators.WriteNoteContentListHtml();

            var linkListPage = new LinkListPage();
            linkListPage.WriteLocalHtmlRssAndJson();
            progress?.Report("Creating Link List Json");
            Export.WriteLinkListJson();
        }

        public static async Task GenerateAllNoteHtml(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.NoteContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for Note Dated {loopItem.CreatedOn:d} - {loopCount} of {totalCount}");

                var htmlModel = new SingleNotePage(loopItem);
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);

                loopCount++;
            }
        }

        public static async Task GenerateAllPhotoHtml(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.PhotoContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Photos to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SinglePhotoPage(loopItem);
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static async Task GenerateAllPostHtml(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.PostContents.ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SinglePostPage(loopItem);
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static void GenerateAllTagHtml(IProgress<string> progress)
        {
            SearchListPageGenerators.WriteTagListAndTagPages(progress);
        }

        public static void GenerateAllUtilityJson(IProgress<string> progress)
        {
            progress?.Report("Creating Menu Links Json");
            Export.WriteMenuLinksJson();

            progress?.Report("Creating Tag Exclusion Json");
            Export.WriteTagExclusionsJson();
        }

        public static async Task GenerateCameraRollHtml(IProgress<string> progress)
        {
            var cameraRollPage = await CameraRollGalleryPageGenerator.CameraRoll(progress);
            cameraRollPage.WriteLocalHtml();
        }

        public static async Task GenerateChangedContentIdReferencesReferences(DateTime contentAfter,
            IProgress<string> progress)
        {
            var db = await Db.Context();

            progress?.Report("Clearing GenerationContentIdReferences Table");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM [" + "GenerationContentIdReferences" + "];");

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

            progress?.Report($"Processing {originalContentSets.Count()} Sets of Changed Content for Related Content");

            var currentCount = 1;

            foreach (var loopSets in originalContentSets)
            {
                progress?.Report($"Processing Related Content Set {currentCount++}");
                relatedIds.AddRange(await db.RelatedContents.Where(x => loopSets.Contains(x.ContentTwo))
                    .Select(x => x.ContentOne).Distinct().ToListAsync());
            }

            contentChanges.AddRange(relatedIds.Distinct());

            progress?.Report($"Adding {relatedIds.Distinct().Count()} Content Ids Generate");

            await db.GenerationContentIdReferences.AddRangeAsync(contentChanges.Distinct()
                .Select(x => new GenerationContentIdReference {ContentId = x}).ToList());

            await db.SaveChangesAsync();
        }

        public static async Task GenerateChangedListHtml(IProgress<string> progress)
        {
            SearchListPageGenerators.WriteAllContentCommonSearchListHtml();

            var lastGenerationSetting = UserSettingsSingleton.CurrentSettings().LastGenerationUtc;

            var lastGenerationDateTime = lastGenerationSetting ?? DateTime.MinValue;

            var db = await Db.Context();
            var filesChanged =
                db.FileContents.Join(db.GenerationContentIdReferences, o => o.ContentId, i => i.ContentId, (i, o) => o)
                    .Any() || db.FileContents.Where(x => x.MainPicture != null).Join(db.GenerationContentIdReferences,
                    o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var filesDeleted = (await Db.DeletedFileContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);
            if (filesChanged || filesDeleted) SearchListPageGenerators.WriteFileContentListHtml();
            else progress?.Report("Skipping File List Generation - no file or file main picture changes found");

            var imagesChanged = db.ImageContents
                .Join(db.GenerationContentIdReferences, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var imagesDeleted = (await Db.DeletedImageContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);
            if (imagesChanged || imagesDeleted) SearchListPageGenerators.WriteImageContentListHtml();
            else progress?.Report("Skipping Image List Generation - no image changes found");

            var photosChanged = db.PhotoContents
                .Join(db.GenerationContentIdReferences, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var photosDeleted = (await Db.DeletedPhotoContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);
            if (photosChanged || photosDeleted) SearchListPageGenerators.WritePhotoContentListHtml();
            else progress?.Report("Skipping Photo List Generation - no image changes found");

            var postChanged =
                db.PostContents.Join(db.GenerationContentIdReferences, o => o.ContentId, i => i.ContentId, (i, o) => o)
                    .Any() || db.PostContents.Where(x => x.MainPicture != null).Join(db.GenerationContentIdReferences,
                    o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var postsDeleted = (await Db.DeletedPostContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);
            if (postChanged || postsDeleted) SearchListPageGenerators.WritePostContentListHtml();
            else progress?.Report("Skipping Post List Generation - no file or file main picture changes found");

            var notesChanged = db.NoteContents
                .Join(db.GenerationContentIdReferences, o => o.ContentId, i => i.ContentId, (i, o) => o).Any();
            var notesDeleted = (await Db.DeletedNoteContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);
            if (notesChanged || notesDeleted) SearchListPageGenerators.WriteNoteContentListHtml();
            else progress?.Report("Skipping Note List Generation - no image changes found");

            var linkListPage = new LinkListPage();
            linkListPage.WriteLocalHtmlRssAndJson();
            progress?.Report("Creating Link List Json");
            Export.WriteLinkListJson();
        }

        public static async Task GenerateChangedToHtml(IProgress<string> progress)
        {
            var frozenNow = DateTime.Now.ToUniversalTime();

            var lastGenerationSetting = UserSettingsSingleton.CurrentSettings().LastGenerationUtc;

            if (lastGenerationSetting == null)
            {
                progress?.Report("No value for Last Generation in Settings - Generating All HTML");

                await GenerateAllHtml(progress);
                return;
            }

            var lastGenerationDateTime = lastGenerationSetting.Value;

            progress?.Report(
                $"Generation HTML based on changes after UTC - {UserSettingsSingleton.CurrentSettings().LastGenerationUtc}");

            await RelatedContentReference.GenerateRelatedContentDbTable(lastGenerationDateTime, progress);
            await GenerateChangedContentIdReferencesReferences(lastGenerationDateTime, progress);

            var db = await Db.Context();
            if (!(await db.GenerationContentIdReferences.AnyAsync()))
                progress?.Report("No Changes Detected - ending HTML generation.");

            await GenerateChangeFilteredPhotoHtml(progress);
            await GenerateChangeFilteredImageHtml(progress);
            await GenerateChangeFilteredFileHtml(progress);
            await GenerateChangeFilteredNoteHtml(progress);
            await GenerateChangeFilteredPostHtml(progress);

            var hasDirectPhotoChanges = db.PhotoContents.Join(db.GenerationContentIdReferences, o => o.ContentId,
                i => i.ContentId, (o, i) => o.PhotoCreatedOn).Any();
            var hasRelatedPhotoChanges = db.PhotoContents.Join(db.RelatedContents, o => o.ContentId, i => i.ContentTwo,
                (o, i) => o.PhotoCreatedOn).Any();
            var hasDeletedPhotoChanges =
                (await Db.DeletedPhotoContent()).Any(x => x.ContentVersion >= lastGenerationDateTime);

            if (hasDirectPhotoChanges || hasRelatedPhotoChanges || hasDeletedPhotoChanges)
                await GenerateAllDailyPhotoGalleriesHtml(progress);
            else
                progress?.Report(
                    "No changes to Photos directly or thru related content - skipping Daily Photo Page generation.");

            if (hasDirectPhotoChanges || hasDeletedPhotoChanges) await GenerateCameraRollHtml(progress);
            else progress?.Report("No changes to Photo content - skipping Photo Gallery generation.");

            GenerateAllTagHtml(progress);
            await GenerateChangedListHtml(progress);
            GenerateAllUtilityJson(progress);
            GenerateIndex(progress);

            progress?.Report($"Generation Complete - writing {frozenNow} as Last Generation UTC into settings");
            UserSettingsSingleton.CurrentSettings().LastGenerationUtc = frozenNow;
            await UserSettingsSingleton.CurrentSettings().WriteSettings();
        }

        public static async Task GenerateChangeFilteredFileHtml(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.FileContents
                .Join(db.GenerationContentIdReferences, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Files to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SingleFilePage(loopItem);
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);

                loopCount++;
            }
        }

        public static async Task GenerateChangeFilteredImageHtml(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.ImageContents
                .Join(db.GenerationContentIdReferences, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Images to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SingleImagePage(loopItem);
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static async Task GenerateChangeFilteredNoteHtml(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.NoteContents
                .Join(db.GenerationContentIdReferences, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for Note Dated {loopItem.CreatedOn:d} - {loopCount} of {totalCount}");

                var htmlModel = new SingleNotePage(loopItem);
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem, progress);

                loopCount++;
            }
        }

        public static async Task GenerateChangeFilteredPhotoHtml(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.PhotoContents
                .Join(db.GenerationContentIdReferences, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Photos to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SinglePhotoPage(loopItem);
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static async Task GenerateChangeFilteredPostHtml(IProgress<string> progress)
        {
            var db = await Db.Context();

            var allItems = await db.PostContents
                .Join(db.GenerationContentIdReferences, o => o.ContentId, i => i.ContentId, (o, i) => o).ToListAsync();

            var loopCount = 1;
            var totalCount = allItems.Count;

            progress?.Report($"Found {totalCount} Posts to Generate");

            foreach (var loopItem in allItems)
            {
                progress?.Report($"Writing HTML for {loopItem.Title} - {loopCount} of {totalCount}");

                var htmlModel = new SinglePostPage(loopItem);
                htmlModel.WriteLocalHtml();
                await Export.WriteLocalDbJson(loopItem);

                loopCount++;
            }
        }

        public static void GenerateIndex(IProgress<string> progress)
        {
            var index = new IndexPage();
            index.WriteLocalHtml();
        }
    }
}