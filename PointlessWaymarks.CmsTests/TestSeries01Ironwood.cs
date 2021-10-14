﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AngleSharp;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Import;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;

namespace PointlessWaymarks.CmsTests
{
    public class TestSeries01Ironwood
    {
        public const string ContributorOneName = "Ironwood Enthusiast";
        public const string TestDefaultCreatedBy = "Ironwood Ghost Writer";
        public const string TestSiteAuthors = "Pointless Waymarks Ironwood 'Testers'";
        public const string TestSiteEmailTo = "Ironwood@Forest.Fake";

        public const string TestSiteKeywords =
            "ironwood forest national monument, samaniego hills, waterman mountains, test'ing";

        public const string TestSiteName = "Ironwood Forest's Test Site";

        public const string TestSummary = "'Testing' in the beautiful Sonoran Desert";

        public const string UrlBlmMapPdf =
            "https://www.blm.gov/sites/blm.gov/files/documents/AZ_IronwoodForest_NM_map.pdf";

        public const string UrlBlmSite = "https://www.blm.gov/visit/ironwood";

        public const string UrlFriendsOfIronwood = "https://ironwoodforest.org/";
        public const string UrlProclamationPdf = "https://www.blm.gov/sites/blm.gov/files/documents/ironwood_proc.pdf";

        public static UserSettings TestSiteSettings { get; set; }

        [OneTimeSetUp]
        public async Task A00_CreateTestSite()
        {
            //This is one of the lower answers from the StackOverflow question below - I found this
            //to be a very easy and understandable way to allow WPF GUI oriented code that contains
            //sections that must run on the GUI thread to run without issue.
            //
            //https://stackoverflow.com/questions/1106881/using-the-wpf-dispatcher-in-unit-tests
            var waitForApplicationRun = new TaskCompletionSource<bool>();
#pragma warning disable 4014
            Task.Run(() =>
#pragma warning restore 4014
            {
                var application = new Application();
                application.Startup += (s, e) => { waitForApplicationRun.SetResult(true); };
                application.Run();
            });
            waitForApplicationRun.Task.Wait();

            var outSettings = await UserSettingsUtilities.SetupNewSite(
                $"IronwoodForestTestSite-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}", DebugTrackers.DebugProgressTracker());
            TestSiteSettings = outSettings;
            TestSiteSettings.SiteName = TestSiteName;
            TestSiteSettings.DefaultCreatedBy = TestDefaultCreatedBy;
            TestSiteSettings.SiteAuthors = TestSiteAuthors;
            TestSiteSettings.SiteEmailTo = TestSiteEmailTo;
            TestSiteSettings.SiteKeywords = TestSiteKeywords;
            TestSiteSettings.SiteSummary = TestSummary;
            TestSiteSettings.SiteUrl = "localhost";
            await UserSettingsUtilities.EnsureDbIsPresent(DebugTrackers.DebugProgressTracker());
            await TestSiteSettings.WriteSettings();
            UserSettingsSingleton.CurrentSettings().InjectFrom(TestSiteSettings);

            LogHelpers.InitializeStaticLoggerAsEventLogger();
        }

        [Test]
        public void A01_TestSiteBasicStructureCheck()
        {
            Assert.True(TestSiteSettings.LocalMediaArchiveFileDirectory().Exists);
            Assert.True(TestSiteSettings.LocalMediaArchiveImageDirectory().Exists);
            Assert.True(TestSiteSettings.LocalMediaArchiveFileDirectory().Exists);
            Assert.True(TestSiteSettings.LocalMediaArchivePhotoDirectory().Exists);
            Assert.True(TestSiteSettings.LocalSiteDirectory().Exists);
        }

        [Test]
        public async Task A09_TagExclusionAddTests()
        {
            var (generationReturn, returnContent) =
                await TagExclusionGenerator.Save(new TagExclusion {Tag = "manville road"});

            Assert.IsFalse(generationReturn.HasError);
            Assert.Greater(returnContent.Id, 0);

            var duplicateTagValidationFailureResult =
                await TagExclusionGenerator.Validate(new TagExclusion {Tag = "manville road"});

            Assert.IsTrue(duplicateTagValidationFailureResult.HasError);
        }

        [Test]
        public async Task A10_PhotoLoadTest()
        {
            await IronwoodPhotoInfo.PhotoTest(IronwoodPhotoInfo.AguaBlancaFileName, IronwoodPhotoInfo.AguaBlancaContent,
                IronwoodPhotoInfo.AguaBlancaWidth);
            await IronwoodPhotoInfo.PhotoTest(IronwoodPhotoInfo.IronwoodTreeFileName,
                IronwoodPhotoInfo.IronwoodTreeContent01, IronwoodPhotoInfo.IronwoodTreeWidth);
            await IronwoodPhotoInfo.PhotoTest(IronwoodPhotoInfo.IronwoodFileBarrelFileName,
                IronwoodPhotoInfo.IronwoodFireBarrelContent01, IronwoodPhotoInfo.IronwoodFireBarrelWidth);
            await IronwoodPhotoInfo.PhotoTest(IronwoodPhotoInfo.QuarryFileName, IronwoodPhotoInfo.QuarryContent01,
                IronwoodPhotoInfo.QuarryWidth);
            await IronwoodPhotoInfo.PhotoTest(IronwoodPhotoInfo.IronwoodPodFileName,
                IronwoodPhotoInfo.IronwoodPodContent01, IronwoodPhotoInfo.IronwoodPodWidth);
            await IronwoodPhotoInfo.PhotoTest(IronwoodPhotoInfo.DisappearingFileName,
                IronwoodPhotoInfo.DisappearingContent, IronwoodPhotoInfo.DisappearingWidth);
        }

        [Test]
        public async Task A21_PhotoEditorGuiContextEditOfQuarryPhoto()
        {
            DataNotifications.SuspendNotifications = false;
            DataNotifications.NewDataNotificationChannel().MessageReceived += DebugTrackers.DataNotificationDiagnostic;

            var db = await Db.Context();
            var quarryPhoto = db.PhotoContents.Single(x => x.Title == IronwoodPhotoInfo.QuarryContent01.Title);

            var newContext = await PhotoContentEditorContext.CreateInstance(null);

            await newContext.LoadData(quarryPhoto);

            newContext.TitleSummarySlugFolder.TitleEntry.UserValue = string.Empty;
            Assert.True(newContext.TitleSummarySlugFolder.TitleEntry.HasChanges);
            Assert.True(newContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues);
            newContext.TitleSummarySlugFolder.TitleEntry.UserValue = IronwoodPhotoInfo.QuarryContent01.Title;
            Assert.False(newContext.TitleSummarySlugFolder.TitleEntry.HasChanges);

            newContext.TitleSummarySlugFolder.SlugEntry.UserValue += "\\\\";
            Assert.True(newContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
            Assert.True(newContext.TitleSummarySlugFolder.SlugEntry.HasChanges);
            newContext.TitleSummarySlugFolder.SlugEntry.UserValue =
                IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags.Slug;
            Assert.False(newContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);

            newContext.TitleSummarySlugFolder.FolderEntry.UserValue =
                IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags.Folder;
            Assert.False(newContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);

            newContext.TagEdit.Tags = IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags.Tags;
            Assert.False(newContext.TagEdit.HasValidationIssues);
            Assert.True(newContext.TagEdit.HasChanges);

            newContext.BodyContent.BodyContent =
                IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags.BodyContent;
            Assert.True(newContext.BodyContent.BodyContentHasChanges);

            newContext.UpdateNotes.UpdateNotes =
                IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags.UpdateNotes;
            Assert.True(newContext.UpdateNotes.UpdateNotesHasChanges);

            await newContext.SaveAndGenerateHtml(true);

            var (areEqual, comparisonNotes) = IronwoodPhotoInfo.CompareContent(
                IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags, newContext.DbEntry);
            Assert.True(areEqual, comparisonNotes);
        }

        [Test]
        public async Task A22_PhotoExcelUpdate()
        {
            var db = await Db.Context();
            var podPhoto = db.PhotoContents.Single(x => x.Title == IronwoodPhotoInfo.IronwoodPodContent01.Title);
            var treePhoto = db.PhotoContents.Single(x => x.Title == IronwoodPhotoInfo.IronwoodTreeContent01.Title);

            var items = new List<object> {podPhoto, treePhoto};

            var excelFileExport = ExcelHelpers.ContentToExcelFileAsTable(items, "IronwoodTestExport01", false);

            var workbook = new XLWorkbook(excelFileExport.FullName);
            var worksheet = workbook.Worksheets.First();
            var headerRow = worksheet.RangeUsed().Rows(1, 1);

            var contentIdSheetColumn = headerRow.Cells().First(x => x.Value.ToString() == "ContentId").WorksheetColumn()
                .ColumnNumber();
            var slugSheetColumn = headerRow.Cells().First(x => x.Value.ToString() == "Slug").WorksheetColumn()
                .ColumnNumber();
            var titleSheetColumn = headerRow.Cells().First(x => x.Value.ToString() == "Title").WorksheetColumn()
                .ColumnNumber();
            var summarySheetColumn = headerRow.Cells().First(x => x.Value.ToString() == "Summary").WorksheetColumn()
                .ColumnNumber();
            var tagsSheetColumn = headerRow.Cells().First(x => x.Value.ToString() == "Tags").WorksheetColumn()
                .ColumnNumber();
            var updateNotesSheetColumn = headerRow.Cells().First(x => x.Value.ToString() == "UpdateNotes")
                .WorksheetColumn().ColumnNumber();
            var updatedBySheetColumn = headerRow.Cells().First(x => x.Value.ToString() == "LastUpdatedBy")
                .WorksheetColumn().ColumnNumber();
            var lensSheetColumn = headerRow.Cells().First(x => x.Value.ToString() == "Lens").WorksheetColumn()
                .ColumnNumber();
            var cameraModelSheetColumn = headerRow.Cells().First(x => x.Value.ToString() == "CameraModel")
                .WorksheetColumn().ColumnNumber();

            var idColumn = worksheet.Column(contentIdSheetColumn).Intersection(worksheet.RangeUsed()).AsRange();

            var treeSheetPossibleRow =
                idColumn.Cells().FirstOrDefault(x => x.Value.ToString() == treePhoto.ContentId.ToString());

            Assert.NotNull(treeSheetPossibleRow, "No Row found for the tree photo in the Excel Import?");

            var treeSheetRow = treeSheetPossibleRow.WorksheetRow().RowNumber();

            worksheet.Cell(treeSheetRow, slugSheetColumn).Value =
                IronwoodPhotoInfo.IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy.Slug;

            worksheet.Cell(treeSheetRow, titleSheetColumn).Value = IronwoodPhotoInfo
                .IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy.Title;

            worksheet.Cell(treeSheetRow, summarySheetColumn).Value = IronwoodPhotoInfo
                .IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy.Summary;

            worksheet.Cell(treeSheetRow, tagsSheetColumn).Value =
                IronwoodPhotoInfo.IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy.Tags;

            worksheet.Cell(treeSheetRow, updateNotesSheetColumn).Value = IronwoodPhotoInfo
                .IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy.UpdateNotes;

            worksheet.Cell(treeSheetRow, updatedBySheetColumn).Value = IronwoodPhotoInfo
                .IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy.LastUpdatedBy;

            var podSheetRow = idColumn.Cells().First(x => x.Value.ToString() == podPhoto.ContentId.ToString())
                .WorksheetRow().RowNumber();

            worksheet.Cell(podSheetRow, cameraModelSheetColumn).Value =
                IronwoodPhotoInfo.IronwoodPodContent02_CameraModelLensSummary.CameraModel;

            worksheet.Cell(podSheetRow, lensSheetColumn).Value =
                IronwoodPhotoInfo.IronwoodPodContent02_CameraModelLensSummary.Lens;

            worksheet.Cell(podSheetRow, summarySheetColumn).Value =
                IronwoodPhotoInfo.IronwoodPodContent02_CameraModelLensSummary.Summary;

            worksheet.Cell(podSheetRow, updatedBySheetColumn).Value =
                IronwoodPhotoInfo.IronwoodPodContent02_CameraModelLensSummary.LastUpdatedBy;

            workbook.Save();

            var importResult =
                await ContentImport.ImportFromFile(excelFileExport.FullName, DebugTrackers.DebugProgressTracker());
            Assert.False(importResult.HasError, "Unexpected Excel Import Failure");
            Assert.AreEqual(2, importResult.ToUpdate.Count, "Unexpected number of rows to update");

            var (hasError, _) = await ContentImport.SaveAndGenerateHtmlFromExcelImport(importResult,
                DebugTrackers.DebugProgressTracker());

            Assert.False(hasError);

            var updatedPodPhoto = db.PhotoContents.Single(x =>
                x.Title == IronwoodPhotoInfo.IronwoodPodContent02_CameraModelLensSummary.Title);
            var updatedTreePhoto = db.PhotoContents.Single(x =>
                x.Title == IronwoodPhotoInfo.IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy.Title);

            var podReference = IronwoodPhotoInfo.IronwoodPodContent02_CameraModelLensSummary;
            podReference.LastUpdatedOn = updatedPodPhoto.LastUpdatedOn;

            var (areEqual, comparisonNotes) = IronwoodPhotoInfo.CompareContent(podReference, updatedPodPhoto);
            Assert.True(areEqual, $"Excel Pod Picture Update Issues: {comparisonNotes}");

            var treeReference = IronwoodPhotoInfo.IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy;
            treeReference.LastUpdatedOn = updatedTreePhoto.LastUpdatedOn;

            var updatedTreeComparison = IronwoodPhotoInfo.CompareContent(treeReference, updatedTreePhoto);
            Assert.True(updatedTreeComparison.areEqual, $"Excel Tree Picture Update Issues: {comparisonNotes}");
        }

        [Test]
        public async Task A23_PhotoDeleteAndRestoreTest()
        {
            var db = await Db.Context();
            var treePhoto = db.PhotoContents.Single(x =>
                x.Title == IronwoodPhotoInfo.IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy.Title);

            var preDeleteTreePhotoHistoricEntryCount =
                db.HistoricPhotoContents.Count(x => x.ContentId == treePhoto.ContentId);

            await Db.DeletePhotoContent(treePhoto.ContentId, DebugTrackers.DebugProgressTracker());

            var postDeleteTreePhotoHistoricEntryCount =
                db.HistoricPhotoContents.Count(x => x.ContentId == treePhoto.ContentId);

            Assert.AreEqual(preDeleteTreePhotoHistoricEntryCount + 1, postDeleteTreePhotoHistoricEntryCount,
                "After deleting the historic entry count should have increased by one but " +
                $"found {preDeleteTreePhotoHistoricEntryCount} entries before and {postDeleteTreePhotoHistoricEntryCount} entries after?");

            Assert.IsEmpty(db.PhotoContents.Where(x => x.ContentId == treePhoto.ContentId).ToList(),
                $"Photo Content Id {treePhoto.ContentId} still" + "found in DB after delete.");

            var deletedItem = await Db.DeletedPhotoContent();

            Assert.AreEqual(1, deletedItem.Count,
                $"There should be one deleted content return - found {deletedItem.Count}");
            Assert.AreEqual(treePhoto.ContentId, deletedItem.First().ContentId,
                "Deleted Item doesn't have the correct Content Id");

            var latestHistoricEntry = db.HistoricPhotoContents.Where(x => x.ContentId == treePhoto.ContentId)
                .OrderByDescending(x => x.ContentVersion).First();

            Assert.AreEqual(latestHistoricEntry.Id, latestHistoricEntry.Id,
                "Deleted Item doesn't match the Id of the last historic entry?");

            var (generationReturn, _) = await PhotoGenerator.SaveAndGenerateHtml(treePhoto,
                UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoContentFile(treePhoto), true, null,
                DebugTrackers.DebugProgressTracker());

            Assert.IsFalse(generationReturn.HasError,
                $"Error Saving after Deleting? {generationReturn.GenerationNote}");
        }

        [Test]
        public async Task B10_FileMapLoadTest()
        {
            await TestFileInfo.FileTest(TestFileInfo.MapFilename, TestFileInfo.MapContent01);
        }

        [Test]
        public async Task B20_ImageMapLoadTest()
        {
            await IronwoodImageInfo.ImageTest(IronwoodImageInfo.MapFilename, IronwoodImageInfo.MapContent01,
                IronwoodImageInfo.MapWidth);
        }

        [Test]
        public async Task B21_FileMapAddingImageMapBracketCodeToBody()
        {
            var db = await Db.Context();

            var mapImage = db.ImageContents.Single(x => x.Title == IronwoodImageInfo.MapContent01.Title);

            var mapFile = db.FileContents.Single(x => x.Title == TestFileInfo.MapContent01.Title);

            mapFile.BodyContent =
                $"{BracketCodeImages.Create(mapImage)} {Environment.NewLine}{Environment.NewLine}{mapFile.BodyContent}";

            mapFile.LastUpdatedBy = "Test B21";
            mapFile.LastUpdatedOn = DateTime.Now;

            var (generationReturn, _) = await FileGenerator.SaveAndGenerateHtml(mapFile,
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileContentFile(mapFile), false, null,
                DebugTrackers.DebugProgressTracker());

            Assert.False(generationReturn.HasError, generationReturn.GenerationNote);

            var mapFileRefresh = db.FileContents.Single(x => x.Title == TestFileInfo.MapContent01.Title);

            Assert.AreEqual(mapImage.ContentId, mapFileRefresh.MainPicture,
                "Adding an image code to the Map File Content Body didn't result in Main Image being set.");
        }

        [Test]
        public async Task C10_NoteLinkLoadTest()
        {
            await IronwoodNoteInfo.NoteTest(IronwoodNoteInfo.LinkNoteContent01);
        }

        [Test]
        public async Task D10_FirstGeneration()
        {
            var db = await Db.Context();

            Assert.True(!db.GenerationLogs.Any(), "Unexpected Content in Generation Logs");

            await HtmlGenerationGroups.GenerateChangedToHtml(DebugTrackers.DebugProgressTracker());

            Assert.AreEqual(1, db.GenerationLogs.Count(),
                $"Expected 1 generation log - found {db.GenerationLogs.Count()}");

            var currentGeneration = await db.GenerationLogs.FirstAsync();

            //Index File

            IronwoodHtmlHelpers.CheckIndexHtmlAndGenerationVersion(currentGeneration.GenerationVersion);

            //Tags

            var tags = await Db.TagSlugsAndContentList(true, false, DebugTrackers.DebugProgressTracker());

            var tagFiles = UserSettingsSingleton.CurrentSettings().LocalSiteTagsDirectory().GetFiles("*.html").ToList();

            Assert.AreEqual(tagFiles.Count - 1, tags.Select(x => x.tag).Count(),
                "Did not find the expected number of Tag Files after generation.");

            foreach (var loopDbTags in tags.Select(x => x.tag).ToList())
                Assert.True(tagFiles.Exists(x => x.Name == $"TagList-{loopDbTags}.html"),
                    $"Didn't find a file for Tag {loopDbTags}");


            //DailyPhotos

            var photoRecords = await db.PhotoContents.ToListAsync();

            var photoDates = photoRecords.GroupBy(x => x.PhotoCreatedOn.Date).Select(x => x.Key).ToList();

            var dailyPhotoFiles = UserSettingsSingleton.CurrentSettings().LocalSiteDailyPhotoGalleryDirectory()
                .GetFiles("*.html").ToList();

            Assert.AreEqual(photoDates.Count, dailyPhotoFiles.Count,
                "Didn't find the expected number of Daily Photo Files");

            foreach (var loopPhotoDates in photoDates)
                Assert.True(dailyPhotoFiles.Exists(x => x.Name == $"DailyPhotos-{loopPhotoDates:yyyy-MM-dd}.html"),
                    $"Didn't find a file for Daily Photos {loopPhotoDates:yyyy-MM-dd}");


            //Camera Roll
            var cameraRollFile = UserSettingsSingleton.CurrentSettings().LocalSiteCameraRollPhotoGalleryFileInfo();

            Assert.True(cameraRollFile.Exists, "Camera Roll File not found");

            var cameraRollDocument = IronwoodHtmlHelpers.DocumentFromFile(cameraRollFile);

            var cameraRollGenerationVersionAttributeString = cameraRollDocument.Head.Attributes
                .Single(x => x.Name == "data-generationversion").Value;

            Assert.AreEqual(currentGeneration.GenerationVersion.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff"),
                cameraRollGenerationVersionAttributeString,
                "Generation Version of Camera Roll Does not match expected Log");

            //Note Check
            var noteContent = UserSettingsSingleton.CurrentSettings().LocalSiteNoteDirectory()
                .GetFiles("*.html", SearchOption.AllDirectories).ToList();

            noteContent.ForEach(x =>
                IronwoodHtmlHelpers.CheckGenerationVersionEquals(x, currentGeneration.GenerationVersion));
        }


        [Test]
        public async Task E10_PostTest()
        {
            await IronwoodPostInfo.PostTest(IronwoodPostInfo.WikiQuotePostContent01);
        }

        [Test]
        public async Task F11_HtmlChangedGenerationAfterPostAddedTest()
        {
            var db = await Db.Context();

            var currentGenerationCount = db.GenerationLogs.Count();

            var currentGeneration = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).FirstAsync();

            await HtmlGenerationGroups.GenerateChangedToHtml(DebugTrackers.DebugProgressTracker());

            currentGeneration = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).FirstAsync();

            Assert.AreEqual(currentGenerationCount + 1, db.GenerationLogs.Count(),
                $"Expected {currentGenerationCount + 1} generation logs - found {db.GenerationLogs.Count()}");

            await FileManagement.RemoveContentDirectoriesAndFilesNotFoundInCurrentDatabase(
                DebugTrackers.DebugProgressTracker());

            IronwoodHtmlHelpers.CheckIndexHtmlAndGenerationVersion(currentGeneration.GenerationVersion);

            var tagFiles = UserSettingsSingleton.CurrentSettings().LocalSiteTagsDirectory().GetFiles("*.html").ToList();

            var changedTags =
                Db.TagListParseToSlugs(await db.PostContents.SingleAsync(x => x.Title == "First Post"), false)
                    .Select(x => $"TagList-{x}").ToList();

            var notChanged = tagFiles.Where(x => !changedTags.Contains(Path.GetFileNameWithoutExtension(x.Name)))
                .ToList();

            notChanged.ForEach(x =>
                IronwoodHtmlHelpers.CheckGenerationVersionLessThan(x, currentGeneration.GenerationVersion));

            tagFiles.Where(x => changedTags.Contains(Path.GetFileNameWithoutExtension(x.Name))).ToList().ForEach(x =>
                IronwoodHtmlHelpers.CheckGenerationVersionEquals(x, currentGeneration.GenerationVersion));

            var photoContent = UserSettingsSingleton.CurrentSettings().LocalSitePhotoDirectory()
                .GetFiles("*.html", SearchOption.AllDirectories).ToList();

            photoContent.ForEach(x =>
                IronwoodHtmlHelpers.CheckGenerationVersionLessThan(x, currentGeneration.GenerationVersion));

            var noteContent = UserSettingsSingleton.CurrentSettings().LocalSiteNoteDirectory()
                .GetFiles("*.html", SearchOption.AllDirectories).Where(x => !x.Name.Contains("List")).ToList();

            noteContent.ForEach(x =>
                IronwoodHtmlHelpers.CheckGenerationVersionEquals(x, currentGeneration.GenerationVersion));
        }

        [Test]
        public async Task G10_PostUpdateChangedDetectionTest()
        {
            var db = await Db.Context();

            var wikiQuotePost = db.PostContents.Single(x => x.Slug == IronwoodPostInfo.WikiQuotePostContent01.Slug);

            var allPhotos = db.PhotoContents.ToList();

            foreach (var loopPhotos in allPhotos) wikiQuotePost.BodyContent += BracketCodePhotos.Create(loopPhotos);

            wikiQuotePost.LastUpdatedBy = "Changed Html Test";
            wikiQuotePost.LastUpdatedOn = DateTime.Now;

            var saveResult =
                await PostGenerator.SaveAndGenerateHtml(wikiQuotePost, null, DebugTrackers.DebugProgressTracker());

            Assert.IsFalse(saveResult.generationReturn.HasError);

            var currentGenerationCount = db.GenerationLogs.Count();

            var currentGeneration = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).FirstAsync();

            await HtmlGenerationGroups.GenerateChangedToHtml(DebugTrackers.DebugProgressTracker());

            currentGeneration = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).FirstAsync();

            Assert.AreEqual(currentGenerationCount + 1, db.GenerationLogs.Count(),
                $"Expected {currentGenerationCount + 1} generation logs - found {db.GenerationLogs.Count()}");

            var relatedContentEntries = await db.GenerationRelatedContents
                .Where(x => x.GenerationVersion == currentGeneration.GenerationVersion).ToListAsync();

            Assert.AreEqual(relatedContentEntries.Count, allPhotos.Count + 1);
            Assert.AreEqual(relatedContentEntries.Select(x => x.ContentOne).Distinct().Count(), 2);
            Assert.AreEqual(relatedContentEntries.Select(x => x.ContentTwo).Count(), allPhotos.Count + 1);
            Assert.AreEqual(
                relatedContentEntries.Select(x => x.ContentTwo).Except(allPhotos.Select(x => x.ContentId)).Count(), 1);
            Assert.AreEqual(
                allPhotos.Select(x => x.ContentId).Except(relatedContentEntries.Select(x => x.ContentTwo)).Count(), 0);

            var photoContent = UserSettingsSingleton.CurrentSettings().LocalSitePhotoDirectory()
                .GetFiles("*.html", SearchOption.AllDirectories).ToList().Where(x =>
                    !x.Name.Contains("Daily") && !x.Name.Contains("Roll") && !x.Name.Contains("List")).ToList();

            photoContent.ForEach(x =>
                IronwoodHtmlHelpers.CheckGenerationVersionEquals(x, currentGeneration.GenerationVersion));


            wikiQuotePost = db.PostContents.Single(x => x.Slug == IronwoodPostInfo.WikiQuotePostContent01.Slug);

            wikiQuotePost.BodyContent =
                wikiQuotePost.BodyContent.Replace(BracketCodePhotos.Create(allPhotos.First()), "");

            wikiQuotePost.LastUpdatedBy = "Changed Html Test 02";
            wikiQuotePost.LastUpdatedOn = DateTime.Now;

            saveResult =
                await PostGenerator.SaveAndGenerateHtml(wikiQuotePost, null, DebugTrackers.DebugProgressTracker());

            Assert.IsFalse(saveResult.generationReturn.HasError);

            currentGenerationCount = db.GenerationLogs.Count();

            currentGeneration = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).FirstAsync();

            await HtmlGenerationGroups.GenerateChangedToHtml(DebugTrackers.DebugProgressTracker());

            currentGeneration = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).FirstAsync();

            Assert.AreEqual(currentGenerationCount + 1, db.GenerationLogs.Count(),
                $"Expected {currentGenerationCount + 1} generation logs - found {db.GenerationLogs.Count()}");

            relatedContentEntries = await db.GenerationRelatedContents
                .Where(x => x.GenerationVersion == currentGeneration.GenerationVersion).ToListAsync();

            Assert.AreEqual(relatedContentEntries.Count, allPhotos.Count - 1 + 1);
            Assert.AreEqual(relatedContentEntries.Select(x => x.ContentOne).Distinct().Count(), 2);
            Assert.AreEqual(relatedContentEntries.Select(x => x.ContentTwo).Count(), allPhotos.Count - 1 + 1);
            Assert.AreEqual(
                relatedContentEntries.Select(x => x.ContentTwo).Except(allPhotos.Select(x => x.ContentId)).Count(), 1);
            Assert.AreEqual(
                allPhotos.Select(x => x.ContentId).Except(relatedContentEntries.Select(x => x.ContentTwo)).Count(), 1);

            photoContent = UserSettingsSingleton.CurrentSettings().LocalSitePhotoDirectory()
                .GetFiles("*.html", SearchOption.AllDirectories).ToList().Where(x =>
                    !x.Name.Contains("Daily") && !x.Name.Contains("Roll") && !x.Name.Contains("List")).ToList();

            photoContent.ForEach(x =>
                IronwoodHtmlHelpers.CheckGenerationVersionEquals(x, currentGeneration.GenerationVersion));

            wikiQuotePost = db.PostContents.Single(x => x.Slug == IronwoodPostInfo.WikiQuotePostContent01.Slug);

            wikiQuotePost.BodyContent += $"{Environment.NewLine}Visit Ironwood Today!";

            wikiQuotePost.LastUpdatedBy = "Changed Html Test 02";
            wikiQuotePost.LastUpdatedOn = DateTime.Now;

            saveResult =
                await PostGenerator.SaveAndGenerateHtml(wikiQuotePost, null, DebugTrackers.DebugProgressTracker());

            Assert.IsFalse(saveResult.generationReturn.HasError);

            currentGenerationCount = db.GenerationLogs.Count();

            currentGeneration = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).FirstAsync();

            await HtmlGenerationGroups.GenerateChangedToHtml(DebugTrackers.DebugProgressTracker());

            currentGeneration = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).FirstAsync();

            Assert.AreEqual(currentGenerationCount + 1, db.GenerationLogs.Count(),
                $"Expected {currentGenerationCount + 1} generation logs - found {db.GenerationLogs.Count()}");

            relatedContentEntries = await db.GenerationRelatedContents
                .Where(x => x.GenerationVersion == currentGeneration.GenerationVersion).ToListAsync();

            Assert.AreEqual(relatedContentEntries.Count, allPhotos.Count - 1 + 1);
            Assert.AreEqual(relatedContentEntries.Select(x => x.ContentOne).Distinct().Count(), 2);
            Assert.AreEqual(relatedContentEntries.Select(x => x.ContentTwo).Count(), allPhotos.Count - 1 + 1);
            Assert.AreEqual(
                relatedContentEntries.Select(x => x.ContentTwo).Except(allPhotos.Select(x => x.ContentId)).Count(), 1);
            Assert.AreEqual(
                allPhotos.Select(x => x.ContentId).Except(relatedContentEntries.Select(x => x.ContentTwo)).Count(), 1);

            //Todo: Check that the excluded photo is not regenerated
        }

        public async Task H10_PostUpdateChangedDetectionTest()
        {
            var dailyGalleryDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteDailyPhotoGalleryDirectory();

            var multiPictureTestFile =
                new FileInfo(Path.Combine(dailyGalleryDirectory.FullName, @"DailyPhotos-2020-05-28.html"));

            //Use the default configuration for AngleSharp
            var config = Configuration.Default;

            //Create a new context for evaluating webpages with the given config
            var context = BrowsingContext.New(config);

            //Just get the DOM representation
            var document = await context.OpenAsync(x => x.Content(File.ReadAllText(multiPictureTestFile.FullName)));

            var relatedItems = document.QuerySelectorAll(".related-posts-list-container .related-post-container");

            Assert.AreEqual(1, relatedItems.Length);

            var dailyBeforeAfterItems =
                document.QuerySelectorAll(".post-related-posts-container .related-post-container");

            Assert.AreEqual(2, dailyBeforeAfterItems.Length);
        }

        [OneTimeTearDown]
        public void Z01_TearDown()
        {
            //See the note and code in the setup method
            //
            //https://stackoverflow.com/questions/1106881/using-the-wpf-dispatcher-in-unit-tests
            Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
        }
    }
}