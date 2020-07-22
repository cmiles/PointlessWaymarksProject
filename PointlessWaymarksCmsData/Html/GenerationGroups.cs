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
            GenerateIndex(progress);

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

            var files = await db.FileContents.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId)
                .ToListAsync();
            progress?.Report($"Found {files.Count} File Content Entries Changed After {contentAfter}");

            var images = await db.ImageContents.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId)
                .ToListAsync();
            progress?.Report($"Found {images.Count} Image Content Entries Changed After {contentAfter}");

            var links = await db.LinkStreams.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId)
                .ToListAsync();
            progress?.Report($"Found {links.Count} Link Content Entries Changed After {contentAfter}");

            var notes = await db.NoteContents.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId)
                .ToListAsync();
            progress?.Report($"Found {notes.Count} Note Content Entries Changed After {contentAfter}");

            var photos = await db.PhotoContents.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId)
                .ToListAsync();
            progress?.Report($"Found {photos.Count} Photo Content Entries Changed After {contentAfter}");

            var posts = await db.PostContents.Where(x => x.ContentVersion > contentAfter).Select(x => x.ContentId)
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

        public static async Task GenerateChangesToHtml(IProgress<string> progress)
        {
            var frozenNow = DateTime.Now.ToUniversalTime();

            await RelatedContentReference.GenerateRelatedContentDbTable(frozenNow, progress);
            await GenerateChangedContentIdReferencesReferences(frozenNow, progress);

            await GenerateChangeFilteredPhotoHtml(progress);
            await GenerateChangeFilteredImageHtml(progress);
            await GenerateChangeFilteredFileHtml(progress);
            await GenerateChangeFilteredNoteHtml(progress);
            await GenerateChangeFilteredPostHtml(progress);
            await GenerateAllDailyPhotoGalleriesHtml(progress);
            await GenerateCameraRollHtml(progress);
            GenerateAllTagHtml(progress);
            GenerateAllListHtml(progress);
            GenerateIndex(progress);

            UserSettingsSingleton.CurrentSettings().LastGenerationUtc = frozenNow;
            await UserSettingsSingleton.CurrentSettings().WriteSettings();
        }

        public static void GenerateIndex(IProgress<string> progress)
        {
            var index = new IndexPage();
            index.WriteLocalHtml();
        }
    }
}