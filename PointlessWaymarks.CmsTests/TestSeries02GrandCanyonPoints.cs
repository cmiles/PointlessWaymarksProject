using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.ExcelImport;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsTests;

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
        TestSiteSettings.SiteDomainName = "localhost";
        await UserSettingsUtilities.EnsureDbIsPresent(DebugTrackers.DebugProgressTracker());
        await TestSiteSettings.WriteSettings();
        UserSettingsSingleton.CurrentSettings().InjectFrom(TestSiteSettings);

        PointlessWaymarksLogTools.InitializeStaticLoggerAsEventLogger();
    }

    [Test]
    public void A01_TestSiteBasicStructureCheck()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TestSiteSettings.LocalMediaArchiveFileDirectory().Exists);
            Assert.That(TestSiteSettings.LocalMediaArchiveImageDirectory().Exists);
        });
        Assert.That(TestSiteSettings.LocalMediaArchiveFileDirectory().Exists);
        Assert.That(TestSiteSettings.LocalMediaArchivePhotoDirectory().Exists);
        Assert.That(TestSiteSettings.LocalSiteDirectory().Exists);
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

        var elevation = await ElevationService.OpenTopoNedElevation(
            GrandCanyonPointInfo.YumaPointContent02.Latitude, GrandCanyonPointInfo.YumaPointContent02.Longitude,
            DebugTrackers.DebugProgressTracker());

        Assert.That(elevation, Is.Not.Null, "Elevation returned null");

        var concreteElevation = Math.Round(elevation.Value.MetersToFeet(), 0);

        Assert.That(concreteElevation, Is.EqualTo(GrandCanyonPointInfo.YumaPointContent02.Elevation),
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
        Assert.That(testFile.Exists, "Test File Found");

        var importResult =
            await ContentImport.ImportFromFile(testFile.FullName, DebugTrackers.DebugProgressTracker());
        Assert.That(importResult.HasError, Is.False, "Unexpected Excel Import Failure");

        var (hasError, _) = await ContentImport.SaveAndGenerateHtmlFromExcelImport(importResult,
            DebugTrackers.DebugProgressTracker());

        Assert.That(hasError, Is.False);

        var pointCountAfterImport = db.PointContents.Count();

        var excelFile = new XLWorkbook(testFile.FullName);
        var excelDataRowCount = excelFile.Worksheets.First().RangeUsed().RowCount() - 1;

        Assert.That(pointCountBeforeImport + excelDataRowCount, Is.EqualTo(pointCountAfterImport));
    }

    [Test]
    public async Task C11_ExcelNewPointImportValidationFailureOnDuplicatingExistingSlug()
    {
        var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
            "GrandCanyonPointsImport.xlsx"));
        Assert.That(testFile.Exists, "Test File Found");

        var importResult =
            await ContentImport.ImportFromFile(testFile.FullName, DebugTrackers.DebugProgressTracker());
        Assert.That(importResult.HasError,
            "Expected a validation failure due to duplicate slug but not detected...");
    }

    [Test]
    public async Task C12_ExcelNewPointImportValidationFailureTryingToImportSameSlugMultipleTimes()
    {
        var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
            "HermitsRestDuplicateSlugImport.xlsx"));
        Assert.That(testFile.Exists, "Test File Found");

        var importResult =
            await ContentImport.ImportFromFile(testFile.FullName, DebugTrackers.DebugProgressTracker());
        Assert.That(importResult.HasError,
            "Expected a validation failure due to duplicate slug but not detected...");
    }

    [Test]
    public async Task G01_GeoJsonGrandCanyonWildfireSave()
    {
        var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
            "GrandCanyonHistoricWildfireGeoJson.geojson"));
        Assert.That(testFile.Exists, "GeoJson Test File Found");

        var geoJsonTest = new GeoJsonContent
        {
            ContentId = Guid.NewGuid(),
            BodyContent = "Grand Canyon Historic Wildfire GeoJson Test",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CreatedOn = DateTime.Now,
            FeedOn = DateTime.Now,
            ContentVersion = Db.ContentVersionDateTime(),
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

        Assert.That(generationReturn.HasError, Is.False);
    }

    [Test]
    public async Task L01_HorseshoeMesaLineContent()
    {
        var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
            "GrandCanyonHorseShoeMesaEastSideLoop.gpx"));
        Assert.That(testFile.Exists, "GPX Test File Found");

        var track = (await GpxTools.TracksFromGpxFile(testFile, DebugTrackers.DebugProgressTracker()))
            .First();

        var lineTest = await LineGenerator.NewFromGpxTrack(track, false, true, false, null);

        var stats = DistanceTools.LineStatsInMetricFromCoordinateList(track.Track);

        lineTest.ContentId = Guid.NewGuid();
        lineTest.BodyContent = "Horseshoe Mesa East Side Loop";
        lineTest.BodyContentFormat = ContentFormatDefaults.Content.ToString();
        lineTest.CreatedBy = "GPX Import Test";
        lineTest.Folder = "GrandCanyon";
        lineTest.Title = "Horseshoe Mesa East Side Loop";
        lineTest.Slug = "horseshoe-mesa-east-side-loop";
        lineTest.ShowInMainSiteFeed = true;
        lineTest.Summary = "Horseshoe Mesa East Side Loop";
        lineTest.Tags = "grand-canyon; horse-shoe-mesa";
        lineTest.UpdateNotesFormat = ContentFormatDefaults.Content.ToString();

        var validationResult = await LineGenerator.Validate(lineTest);

        Assert.That(validationResult.HasError, Is.False);

        var (generationReturn, _) =
            await LineGenerator.SaveAndGenerateHtml(lineTest, null, DebugTrackers.DebugProgressTracker());

        Assert.That(generationReturn.HasError, Is.False);
    }

    [Test]
    public async Task M01_GenerateMap()
    {
        var newMap = MapComponent.CreateInstance();

        newMap.CreatedBy = "Map Test D01";
        newMap.Title = "Grand Canyon Map";
        newMap.Summary = "Grand Canyon Test Grouping";

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

        Assert.That(validationResult.HasError, Is.False);

        var (generationReturn, _) =
            await MapComponentGenerator.SaveAndGenerateData(newMapDto, null, DebugTrackers.DebugProgressTracker());

        Assert.That(generationReturn.HasError, Is.False);
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
            FeedOn = new DateTime(2020, 10, 19, 7, 16, 16),
            ContentVersion = Db.ContentVersionDateTime(),
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

        await SiteGeneration.AllSiteContent(DebugTrackers.DebugProgressTracker());

        Assert.That(db.GenerationLogs.Count(), Is.EqualTo(currentGenerationCount + 1),
            $"Expected {currentGenerationCount + 1} generation logs - found {db.GenerationLogs.Count()}");

        var currentGeneration = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).FirstAsync();

        await FileManagement.RemoveContentDirectoriesAndFilesNotFoundInCurrentDatabase(
            DebugTrackers.DebugProgressTracker());

        IronwoodHtmlHelpers.CheckIndexHtmlAndGenerationVersion(currentGeneration.GenerationVersion);
    }
}