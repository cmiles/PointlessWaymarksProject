using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using ClosedXML.Excel;
using NUnit.Framework;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.ExcelImport;
using PointlessWaymarksCmsData.Html.CommonHtml;
using PointlessWaymarksCmsWpfControls.PhotoContentEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksTests
{
    public class IronwoodTests
    {
        public const string UrlProclamationPdf = "https://www.blm.gov/sites/blm.gov/files/documents/ironwood_proc.pdf";
        public const string UrlBlmSite = "https://www.blm.gov/visit/ironwood";

        public const string UrlBlmMapPdf =
            "https://www.blm.gov/sites/blm.gov/files/documents/AZ_IronwoodForest_NM_map.pdf";

        public const string UrlFriendsOfIronwood = "https://ironwoodforest.org/";
        public const string ContributorOneName = "Ironwood Enthusiast";

        public const string TestSiteName = "Ironwood Forest's Test Site";
        public const string TestDefaultCreatedBy = "Ironwood Ghost Writer";
        public const string TestSiteAuthors = "Pointless Waymarks Ironwood 'Testers'";
        public const string TestSiteEmailTo = "Ironwood@Forest.Fake";

        public const string TestSiteKeywords =
            "ironwood forest national monument, samaniego hills, waterman mountains, test'ing";

        public const string TestSummary = "'Testing' in the beautiful Sonoran Desert";

        public static UserSettings TestSiteSettings;


        [OneTimeSetUp]
        public async Task A00_CreateTestSite()
        {
            var outSettings =
                await UserSettingsUtilities.SetupNewSite($"IronwoodForestTestSite-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}",
                    DebugProgressTracker());
            TestSiteSettings = outSettings;
            TestSiteSettings.SiteName = TestSiteName;
            TestSiteSettings.DefaultCreatedBy = TestDefaultCreatedBy;
            TestSiteSettings.SiteAuthors = TestSiteAuthors;
            TestSiteSettings.SiteEmailTo = TestSiteEmailTo;
            TestSiteSettings.SiteKeywords = TestSiteKeywords;
            TestSiteSettings.SiteSummary = TestSummary;
            TestSiteSettings.SiteUrl = "IronwoodTest.com";
            await TestSiteSettings.EnsureDbIsPresent(DebugProgressTracker());
            await TestSiteSettings.WriteSettings();
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
        public async Task A10_PhotoLoadTest()
        {
            await IronwoodPhotoInfo.PhotoTest(IronwoodPhotoInfo.AguaBlancaFileName, IronwoodPhotoInfo.AguaBlancaContent,
                IronwoodPhotoInfo.AguaBlancaWidth);
            await IronwoodPhotoInfo.PhotoTest(IronwoodPhotoInfo.IronwoodTreeFileName,
                IronwoodPhotoInfo.IronwoodTreeContent01, IronwoodPhotoInfo.IronwoodTreeWidth);
            await IronwoodPhotoInfo.PhotoTest(IronwoodPhotoInfo.QuarryFileName, IronwoodPhotoInfo.QuarryContent01,
                IronwoodPhotoInfo.QuarryWidth);
            await IronwoodPhotoInfo.PhotoTest(IronwoodPhotoInfo.IronwoodPodFileName,
                IronwoodPhotoInfo.IronwoodPodContent01, IronwoodPhotoInfo.IronwoodPodWidth);
            await IronwoodPhotoInfo.PhotoTest(IronwoodPhotoInfo.DisappearingFileName,
                IronwoodPhotoInfo.DisappearingContent, IronwoodPhotoInfo.DisappearingWidth);
        }

        [Test]
        public async Task A21_PhotoEditorContextEditOfQuarryPhoto()
        {
            ThreadSwitcher.PinnedDispatcher = Dispatcher.CurrentDispatcher;

            var db = await Db.Context();
            var quarryPhoto = db.PhotoContents.Single(x => x.Title == IronwoodPhotoInfo.QuarryContent01.Title);

            var newContext = new PhotoContentEditorContext(null, true);

            await newContext.LoadData(quarryPhoto);

            newContext.TitleSummarySlugFolder.Title = string.Empty;
            Assert.True(newContext.TitleSummarySlugFolder.TitleHasChanges);
            Assert.True(newContext.TitleSummarySlugFolder.TitleHasValidationIssues);
            newContext.TitleSummarySlugFolder.Title = IronwoodPhotoInfo.QuarryContent01.Title;
            Assert.False(newContext.TitleSummarySlugFolder.TitleHasChanges);

            newContext.TitleSummarySlugFolder.Slug += "\\\\";
            Assert.True(newContext.TitleSummarySlugFolder.SlugHasValidationIssues);
            Assert.True(newContext.TitleSummarySlugFolder.SlugHasChanges);
            newContext.TitleSummarySlugFolder.Slug = IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags.Slug;
            Assert.False(newContext.TitleSummarySlugFolder.SlugHasValidationIssues);

            newContext.TitleSummarySlugFolder.Folder =
                IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags.Folder;
            Assert.False(newContext.TitleSummarySlugFolder.SlugHasValidationIssues);

            newContext.TagEdit.Tags = IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags.Tags;
            Assert.False(newContext.TagEdit.TagsHaveValidationIssues);
            Assert.True(newContext.TagEdit.TagsHaveChanges);

            newContext.BodyContent.BodyContent =
                IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags.BodyContent;
            Assert.True(newContext.BodyContent.BodyContentHasChanges);

            newContext.UpdateNotes.UpdateNotes =
                IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags.UpdateNotes;
            Assert.True(newContext.UpdateNotes.UpdateNotesHasChanges);

            await newContext.SaveAndGenerateHtml(true);

            var comparison =
                IronwoodPhotoInfo.CompareContent(IronwoodPhotoInfo.QuarryContent02_BodyContentUpdateNotesTags,
                    newContext.DbEntry);
            Assert.False(comparison.hasInvalidComparison, comparison.comparisonNotes);
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
                IronwoodPhotoInfo.IronwoodPodContent02_CamerModelLensSummary.CameraModel;

            worksheet.Cell(podSheetRow, lensSheetColumn).Value =
                IronwoodPhotoInfo.IronwoodPodContent02_CamerModelLensSummary.Lens;

            worksheet.Cell(podSheetRow, summarySheetColumn).Value =
                IronwoodPhotoInfo.IronwoodPodContent02_CamerModelLensSummary.Summary;

            worksheet.Cell(podSheetRow, updatedBySheetColumn).Value =
                IronwoodPhotoInfo.IronwoodPodContent02_CamerModelLensSummary.LastUpdatedBy;

            workbook.Save();

            var importResult =
                await ExcelContentImports.ImportFromFile(excelFileExport.FullName, DebugProgressTracker());
            Assert.False(importResult.HasError, "Unexpected Excel Import Failure");
            Assert.AreEqual(2, importResult.ToUpdate.Count, "Unexpected number of rows to update");

            var updateSaveResult =
                await ExcelContentImports.SaveAndGenerateHtmlFromExcelImport(importResult, DebugProgressTracker());

            Assert.False(updateSaveResult.hasError);

            var updatedPodPhoto = db.PhotoContents.Single(x =>
                x.Title == IronwoodPhotoInfo.IronwoodPodContent02_CamerModelLensSummary.Title);
            var updatedTreePhoto = db.PhotoContents.Single(x =>
                x.Title == IronwoodPhotoInfo.IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy.Title);

            var podReference = IronwoodPhotoInfo.IronwoodPodContent02_CamerModelLensSummary;
            podReference.LastUpdatedOn = updatedPodPhoto.LastUpdatedOn;

            var updatedPodComparison = IronwoodPhotoInfo.CompareContent(podReference, updatedPodPhoto);
            Assert.False(updatedPodComparison.hasInvalidComparison,
                $"Excel Pod Picture Update Issues: {updatedPodComparison.comparisonNotes}");

            var treeReference = IronwoodPhotoInfo.IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy;
            treeReference.LastUpdatedOn = updatedTreePhoto.LastUpdatedOn;

            var updatedTreeComparison = IronwoodPhotoInfo.CompareContent(treeReference, updatedTreePhoto);
            Assert.False(updatedTreeComparison.hasInvalidComparison,
                $"Excel Tree Picture Update Issues: {updatedPodComparison.comparisonNotes}");
        }

        [Test]
        public async Task B10_FileMapLoadTest()
        {
            await IronwoodFileInfo.FileTest(IronwoodFileInfo.MapFilename, IronwoodFileInfo.MapContent01);
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

            var mapFile = db.FileContents.Single(x => x.Title == IronwoodFileInfo.MapContent01.Title);

            mapFile.BodyContent =
                $"{BracketCodeImages.ImageBracketCode(mapImage)} {Environment.NewLine}{Environment.NewLine}{mapFile.BodyContent}";

            mapFile.LastUpdatedBy = "Test B21";
            mapFile.LastUpdatedOn = DateTime.Now;

            var bodyUpdateReturn = await FileGenerator.SaveAndGenerateHtml(mapFile,
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileContentFile(mapFile), false,
                DebugProgressTracker());

            Assert.False(bodyUpdateReturn.generationReturn.HasError, bodyUpdateReturn.generationReturn.GenerationNote);

            var mapFileRefresh = db.FileContents.Single(x => x.Title == IronwoodFileInfo.MapContent01.Title);

            Assert.AreEqual(mapImage.ContentId, mapFileRefresh.MainPicture,
                "Adding an image code to the Map File Content Body didn't result in Main Image being set.");
        }

        public static IProgress<string> DebugProgressTracker()
        {
            var toReturn = new Progress<string>();
            toReturn.ProgressChanged += DebugProgressTrackerChange;
            return toReturn;
        }

        private static void DebugProgressTrackerChange(object sender, string e)
        {
            Debug.WriteLine(e);
        }
    }
}