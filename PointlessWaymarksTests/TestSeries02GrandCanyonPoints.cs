using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.ExcelImport;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsData.Spatial;
using PointlessWaymarksCmsData.Spatial.Elevation;

namespace PointlessWaymarksTests
{
    public class TestSeries02GrandCanyonPoints
    {
        public const string TestSiteName = "Grand Canyon Rim Notes";
        public const string TestDefaultCreatedBy = "GC Ghost Writer";
        public const string TestSiteAuthors = "Pointless Waymarks Grand Canyon 'Testers'";
        public const string TestSiteEmailTo = "Grand@Canyon.Fake";

        public const string TestSiteKeywords = "grand canyon, points, places";

        public const string TestSummary = "'Testing' in the beautiful Grand Canyon";

        public static UserSettings TestSiteSettings;


        [OneTimeSetUp]
        public async Task A00_CreateTestSite()
        {
            var outSettings = await UserSettingsUtilities.SetupNewSite(
                $"GrandCanyonTestSite-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}", DebugTrackers.DebugProgressTracker());
            TestSiteSettings = outSettings;
            TestSiteSettings.SiteName = TestSiteName;
            TestSiteSettings.DefaultCreatedBy = TestDefaultCreatedBy;
            TestSiteSettings.SiteAuthors = TestSiteAuthors;
            TestSiteSettings.SiteEmailTo = TestSiteEmailTo;
            TestSiteSettings.SiteKeywords = TestSiteKeywords;
            TestSiteSettings.SiteSummary = TestSummary;
            TestSiteSettings.SiteUrl = "localhost";
            await TestSiteSettings.EnsureDbIsPresent(DebugTrackers.DebugProgressTracker());
            await TestSiteSettings.WriteSettings();
            UserSettingsSingleton.CurrentSettings().InjectFrom(TestSiteSettings);
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
        public async Task B10_YumaPointLoadTest()
        {
            await GrandCanyonPointInfo.PointTest(GrandCanyonPointInfo.YumaPointContent01);
        }

        [Test]
        public async Task B11_YumaPointElevationServiceTest()
        {
            var httpClient = new HttpClient();

            var elevation = await ElevationService.OpenTopoNedElevation(httpClient,
                GrandCanyonPointInfo.YumaPointContent02.Latitude, GrandCanyonPointInfo.YumaPointContent02.Longitude,
                DebugTrackers.DebugProgressTracker());

            Assert.NotNull(elevation, "Elevation returned null");

            var concreteElevation = Math.Round(elevation.Value.MetersToFeet(), 0);

            Assert.AreEqual(GrandCanyonPointInfo.YumaPointContent02.Elevation, concreteElevation,
                "Service Elevation does not match");
        }

        [Test]
        public async Task B12_YumaPointUpdateTest()
        {
            var db = await Db.Context();
            var currentYumaPoint = db.PointContents.Single(x => x.Slug == "yuma-point");
            var currentDetails = await Db.PointDetailsForPoint(currentYumaPoint.ContentId, db);

            currentYumaPoint.InjectFromSkippingIds(GrandCanyonPointInfo.YumaPointContent02);
            currentDetails[0].InjectFromSkippingIds(GrandCanyonPointInfo.YumaPointContent02.PointDetails[0]);

            var updatedPoint = Db.PointContentDtoFromPointContentAndDetails(currentYumaPoint, currentDetails);

            await GrandCanyonPointInfo.PointTest(updatedPoint);
        }

        [Test]
        public async Task C10_ExcelNewPointImport()
        {
            var db = await Db.Context();
            var pointCountBeforeImport = db.PointContents.Count();

            var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "IronwoodTestContent",
                "GrandCanyonPointsImport.xlsx"));
            Assert.True(testFile.Exists, "Test File Found");

            var importResult =
                await ExcelContentImports.ImportFromFile(testFile.FullName, DebugTrackers.DebugProgressTracker());
            Assert.False(importResult.HasError, "Unexpected Excel Import Failure");

            var updateSaveResult =
                await ExcelContentImports.SaveAndGenerateHtmlFromExcelImport(importResult,
                    DebugTrackers.DebugProgressTracker());

            Assert.False(updateSaveResult.hasError);

            var pointCountAfterImport = db.PointContents.Count();

            var excelFile = new ClosedXML.Excel.XLWorkbook(testFile.FullName);
            var excelDataRowCount = excelFile.Worksheets.First().RangeUsed().RowCount() - 1;

            Assert.AreEqual(pointCountAfterImport, pointCountBeforeImport + excelDataRowCount);
        }

        [Test]
        public async Task C11_ExcelNewPointImportValidationFailureOnDuplicatingExistingSlug()
        {
            var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "IronwoodTestContent",
                "GrandCanyonPointsImport.xlsx"));
            Assert.True(testFile.Exists, "Test File Found");

            var importResult =
                await ExcelContentImports.ImportFromFile(testFile.FullName, DebugTrackers.DebugProgressTracker());
            Assert.True(importResult.HasError,
                "Expected a validation failure due to duplicate slug but not detected...");
        }

        [Test]
        public async Task C12_ExcelNewPointImportValidationFailureTryingToImportSameSlugMultipleTimes()
        {
            var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "IronwoodTestContent",
                "HermitsRestDuplicateSlugImport.xlsx"));
            Assert.True(testFile.Exists, "Test File Found");

            var importResult =
                await ExcelContentImports.ImportFromFile(testFile.FullName, DebugTrackers.DebugProgressTracker());
            Assert.True(importResult.HasError,
                "Expected a validation failure due to duplicate slug but not detected...");
        }

        [Test]
        public async Task Z10_GenerateAllHtml()
        {
            var db = await Db.Context();
            var forIndex = await db.PointContents.OrderByDescending(x => x.ContentId).Take(4).ToListAsync();
            forIndex.ForEach(x => x.ShowInMainSiteFeed = true);
            await db.SaveChangesAsync(true);

            var currentGenerationCount = db.GenerationLogs.Count();

            await GenerationGroups.GenerateAllHtml(DebugTrackers.DebugProgressTracker());

            Assert.AreEqual(currentGenerationCount + 1, db.GenerationLogs.Count(),
                $"Expected {currentGenerationCount + 1} generation logs - found {db.GenerationLogs.Count()}");

            var currentGeneration = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).FirstAsync();

            await FileManagement.RemoveContentDirectoriesAndFilesNotFoundInCurrentDatabase(
                DebugTrackers.DebugProgressTracker());

            IronwoodHtmlHelpers.CheckIndexHtmlAndGenerationVersion(currentGeneration.GenerationVersion);
        }
    }
}