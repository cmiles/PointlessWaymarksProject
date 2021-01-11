using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.ExcelImport;
using PointlessWaymarks.CmsData.Html;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsData.Spatial.Elevation;
using Serilog;
using Serilog.Formatting.Compact;

namespace PointlessWaymarks.CmsTests
{
    public class TestSeries02GrandCanyonPoints
    {
        public const string TestDefaultCreatedBy = "GC Ghost Writer";
        public const string TestSiteAuthors = "Pointless Waymarks Grand Canyon 'Testers'";
        public const string TestSiteEmailTo = "Grand@Canyon.Fake";

        public const string TestSiteKeywords = "grand canyon, points, places";
        public const string TestSiteName = "Grand Canyon Rim Notes";

        public const string TestSummary = "'Testing' in the beautiful Grand Canyon";

        public UserSettings TestSiteSettings;

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

            LogConfiguration.InitializeStaticLoggerAsEventLogger();
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

            var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
                "GrandCanyonPointsImport.xlsx"));
            Assert.True(testFile.Exists, "Test File Found");

            var importResult =
                await ExcelContentImports.ImportFromFile(testFile.FullName, DebugTrackers.DebugProgressTracker());
            Assert.False(importResult.HasError, "Unexpected Excel Import Failure");

            var (hasError, _) = await ExcelContentImports.SaveAndGenerateHtmlFromExcelImport(importResult,
                DebugTrackers.DebugProgressTracker());

            Assert.False(hasError);

            var pointCountAfterImport = db.PointContents.Count();

            var excelFile = new XLWorkbook(testFile.FullName);
            var excelDataRowCount = excelFile.Worksheets.First().RangeUsed().RowCount() - 1;

            Assert.AreEqual(pointCountAfterImport, pointCountBeforeImport + excelDataRowCount);
        }

        [Test]
        public async Task C11_ExcelNewPointImportValidationFailureOnDuplicatingExistingSlug()
        {
            var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
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
            var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
                "HermitsRestDuplicateSlugImport.xlsx"));
            Assert.True(testFile.Exists, "Test File Found");

            var importResult =
                await ExcelContentImports.ImportFromFile(testFile.FullName, DebugTrackers.DebugProgressTracker());
            Assert.True(importResult.HasError,
                "Expected a validation failure due to duplicate slug but not detected...");
        }

        [Test]
        public async Task G01_GeoJsonGrandCanyonWildfireSave()
        {
            var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
                "GrandCanyonHistoricWildfireGeoJson.geojson"));
            Assert.True(testFile.Exists, "GeoJson Test File Found");

            var geoJsonTest = new GeoJsonContent
            {
                ContentId = Guid.NewGuid(),
                BodyContent = "Grand Canyon Historic Wildfire GeoJson Test",
                BodyContentFormat = ContentFormatDefaults.Content.ToString(),
                CreatedOn = DateTime.Now,
                CreatedBy = "GC Test for GeoJson",
                Folder = "GrandCanyon",
                Title = "Grand Canyon Historic Wildfire Boundaries",
                Slug = "grand-canyon-historic-wildfire-boundaries",
                ShowInMainSiteFeed = true,
                Summary = "Boundaries for Grand Canyon Wildfires",
                Tags = "grand-canyon, geojson",
                UpdateNotesFormat = ContentFormatDefaults.Content.ToString(),
                GeoJson = await File.ReadAllTextAsync(testFile.FullName)
            };

            var (generationReturn, _) =
                await GeoJsonGenerator.SaveAndGenerateHtml(geoJsonTest, null, DebugTrackers.DebugProgressTracker());

            Assert.IsFalse(generationReturn.HasError);
        }

        [Test]
        public async Task L01_HorseshoeMesaLineContent()
        {
            var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
                "GrandCanyonHorseShoeMesaEastSideLoop.gpx"));
            Assert.True(testFile.Exists, "GPX Test File Found");

            var lineTest = new LineContent
            {
                ContentId = Guid.NewGuid(),
                BodyContent = "Horseshoe Mesa East Side Loop",
                BodyContentFormat = ContentFormatDefaults.Content.ToString(),
                CreatedOn = DateTime.Now,
                CreatedBy = "GPX Import Test",
                Folder = "GrandCanyon",
                Title = "Horseshoe Mesa East Side Loop",
                Slug = "horseshoe-mesa-east-side-loop",
                ShowInMainSiteFeed = true,
                Summary = "Horseshoe Mesa East Side Loop",
                Tags = "grand-canyon, horse-shoe-mesa",
                UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
            };

            var track = (await SpatialHelpers.TracksFromGpxFile(testFile, DebugTrackers.DebugProgressTracker()))
                .First();

            var stats = SpatialHelpers.LineStatsInMetricFromCoordinateList(track.track);

            lineTest.ClimbElevation = stats.ElevationClimb;
            lineTest.DescentElevation = stats.ElevationDescent;
            lineTest.MinimumElevation = stats.MinimumElevation;
            lineTest.MaximumElevation = stats.MaximumElevation;
            lineTest.LineDistance = stats.Length;

            lineTest.Line =
                await SpatialHelpers.GeoJsonWithLineStringFromCoordinateList(track.track, false,
                    DebugTrackers.DebugProgressTracker());

            var validationResult = await LineGenerator.Validate(lineTest);

            Assert.IsFalse(validationResult.HasError);

            var (generationReturn, _) =
                await LineGenerator.SaveAndGenerateHtml(lineTest, null, DebugTrackers.DebugProgressTracker());

            Assert.IsFalse(generationReturn.HasError);
        }

        [Test]
        public async Task M01_GenerateMap()
        {
            var newMap = new MapComponent
            {
                ContentId = Guid.NewGuid(),
                CreatedBy = "Map Test D01",
                CreatedOn = DateTime.Now,
                Title = "Grand Canyon Map",
                Summary = "Grand Canyon Test Grouping",
                UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
            };

            var db = await Db.Context();
            var piutePoint = await db.PointContents.SingleAsync(x => x.Title == "Piute Point");
            var pointsNearPiutePoint = await db.PointContents.Where(x =>
                Math.Abs(x.Longitude - piutePoint.Longitude) < .1 && Math.Abs(x.Latitude - piutePoint.Latitude) < .1 &&
                x.Title.EndsWith("Point")).ToListAsync();

            var pointElements = new List<MapElement>();

            foreach (var loopPoints in pointsNearPiutePoint)
                pointElements.Add(new MapElement
                {
                    ElementContentId = loopPoints.ContentId,
                    ShowDetailsDefault = false,
                    IncludeInDefaultView = true,
                    MapComponentContentId = newMap.ContentId,
                    IsFeaturedElement = true
                });

            var lineElement = await db.LineContents.FirstAsync();

            pointElements.Add(new MapElement
            {
                ElementContentId = lineElement.ContentId,
                ShowDetailsDefault = false,
                IncludeInDefaultView = true,
                MapComponentContentId = newMap.ContentId,
                IsFeaturedElement = true
            });

            var grandCanyonFireGeoJson = await db.GeoJsonContents.FirstAsync();

            pointElements.Add(new MapElement
            {
                ElementContentId = grandCanyonFireGeoJson.ContentId,
                ShowDetailsDefault = false,
                IncludeInDefaultView = true,
                MapComponentContentId = newMap.ContentId,
                IsFeaturedElement = true
            });


            pointElements.First().ShowDetailsDefault = true;

            var newMapDto = new MapComponentDto(newMap, pointElements);

            var validationResult = await MapComponentGenerator.Validate(newMapDto);

            Assert.IsFalse(validationResult.HasError);

            var (generationReturn, _) =
                await MapComponentGenerator.SaveAndGenerateData(newMapDto, null, DebugTrackers.DebugProgressTracker());

            Assert.IsFalse(generationReturn.HasError);
        }

        [Test]
        public async Task M02_MapInPost()
        {
            var db = await Db.Context();

            var mapItem = await db.MapComponents.FirstAsync();
            var geoJsonItem = await db.GeoJsonContents.FirstAsync();
            var lineItem = await db.LineContents.FirstAsync();

            var post = new PostContent
            {
                Title = "Piute Point Map Test Point",
                Slug = "first-post",
                BodyContent =
                    $@"This post should have a map below this test showing points near Piute Point in the Grand Canyon.
{{{{mapcomponent {mapItem.ContentId}; Piute Point Map}}}}


And then we should have a GeoJson bracket code creating just the fire map:
{{{{geojson {geoJsonItem.ContentId}; GC Fire Info}}}}


And what about a line...
{{{{line {lineItem.ContentId}; Horseshoe}}}}
",
                BodyContentFormat = ContentFormatDefaults.Content.ToString(),
                ContentId = Guid.NewGuid(),
                CreatedBy = "Map Tester",
                CreatedOn = new DateTime(2020, 10, 19, 7, 16, 16),
                Folder = "GrandCanyon",
                ShowInMainSiteFeed = true,
                Summary = "A basic map of points around Piute Point",
                Tags = "grand canyon, piute point, map",
                UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
            };

            await IronwoodPostInfo.PostTest(post);
        }

        [Test]
        public async Task Z10_GenerateAllHtml()
        {
            var db = await Db.Context();
            var forIndex = await db.PointContents.OrderByDescending(x => x.ContentId).Take(4).ToListAsync();
            forIndex.ForEach(x => x.ShowInMainSiteFeed = true);
            await db.SaveChangesAsync(true);

            var currentGenerationCount = db.GenerationLogs.Count();

            await HtmlGenerationGroups.GenerateAllHtml(DebugTrackers.DebugProgressTracker());

            Assert.AreEqual(currentGenerationCount + 1, db.GenerationLogs.Count(),
                $"Expected {currentGenerationCount + 1} generation logs - found {db.GenerationLogs.Count()}");

            var currentGeneration = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).FirstAsync();

            await FileManagement.RemoveContentDirectoriesAndFilesNotFoundInCurrentDatabase(
                DebugTrackers.DebugProgressTracker());

            IronwoodHtmlHelpers.CheckIndexHtmlAndGenerationVersion(currentGeneration.GenerationVersion);
        }
    }
}