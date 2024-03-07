using NetTopologySuite.Geometries;
using NUnit.Framework;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsTests;

public class ElevationServiceAndGpxTests
{
    [Test]
    public async Task ElevationService_MultiPointMapZenTest()
    {
        var client = new HttpClient();

        var testData = GrandCanyonPointsWithMapZenElevations().Select(x => new CoordinateZ(x.Longitude, x.Latitude, 0))
            .ToList();

        await ElevationService.OpenTopoMapZenElevation(testData, DebugTrackers.DebugProgressTracker());

        Assert.That(testData.All(x => x.Z > 0), "Not all point have an elevation greater than zero.");

        var referenceData = GrandCanyonPointsWithMapZenElevations();

        foreach (var loopTestData in testData)
        {
            var referenceItem =
                referenceData.Single(x => x.Latitude == loopTestData.Y && x.Longitude == loopTestData.X);
            Assert.That(Math.Round(loopTestData.Z, 2), Is.EqualTo(Math.Round(referenceItem.RoundedElevationInMeters, 2)),
                $"{referenceItem.Name} didn't match expected");
        }
    }

    [Test]
    public async Task ElevationService_MultiPointNedTest()
    {
        var client = new HttpClient();

        var testData = GrandCanyonPointsWithNed10Elevations().Select(x => new CoordinateZ(x.Longitude, x.Latitude, 0))
            .ToList();

        await ElevationService.OpenTopoNedElevation(testData, DebugTrackers.DebugProgressTracker());

        Assert.That(testData.All(x => x.Z > 0), "Not all point have an elevation greater than zero.");

        var referenceData = GrandCanyonPointsWithNed10Elevations();

        foreach (var loopTestData in testData)
        {
            var referenceItem =
                referenceData.Single(x => x.Latitude == loopTestData.Y && x.Longitude == loopTestData.X);
            Assert.That(Math.Round(loopTestData.Z, 2), Is.EqualTo(Math.Round(referenceItem.RoundedElevationInMeters, 2)),
                $"{referenceItem.Name} didn't match expected");
        }
    }

    [Test]
    public async Task ElevationService_SinglePointMapZenTest()
    {
        var client = new HttpClient();

        var testData = GrandCanyonPointsWithMapZenElevations();

        foreach (var loopTests in testData)
        {
            var result = await ElevationService.OpenTopoMapZenElevation(loopTests.Latitude, loopTests.Longitude,
                DebugTrackers.DebugProgressTracker());

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Null result from {loopTests.Name}");
                Assert.That(Math.Round(result.Value, 0), Is.EqualTo(loopTests.RoundedElevationInMeters), $"{loopTests.Name}");
            });
        }
    }

    [Test]
    public async Task ElevationService_SinglePointNedTest()
    {
        var client = new HttpClient();

        var testData = GrandCanyonPointsWithNed10Elevations();

        foreach (var loopTests in testData)
        {
            var result = await ElevationService.OpenTopoNedElevation(loopTests.Latitude, loopTests.Longitude,
                DebugTrackers.DebugProgressTracker());

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, $"Null result from {loopTests.Name}");
                Assert.That(Math.Round(result.Value, 2), Is.EqualTo(loopTests.RoundedElevationInMeters), $"{loopTests.Name}");
            });
        }
    }

    public List<ElevationTestData> GrandCanyonPointsWithMapZenElevations()
    {
        //These elevations were verified as the return value from MapZen on 12/4 - and confirmed that these are close to the GNIS values
        // ReSharper disable StringLiteralTypo
        return
        [
            new ElevationTestData("Eremita Tank", 36.0716876, -112.2542091, 1940),
            new ElevationTestData("Vesta Temple", 36.0935096, -112.2689987, 1912),
            new ElevationTestData("Whites Butte", 36.0975983, -112.2312066, 1462),
            new ElevationTestData("Bass Tank", 36.094258, -112.3739584, 1906),
            new ElevationTestData("Pima Point", 36.0719269, -112.2001712, 1995),
            new ElevationTestData("Tower of Set", 36.1211306, -112.1780426, 1830)
        ];
        // ReSharper restore StringLiteralTypo
    }

    public List<ElevationTestData> GrandCanyonPointsWithNed10Elevations()
    {
        //These elevations were verified as the return value from MapZen on 12/4 - and confirmed that these are close to the GNIS values
        // ReSharper disable StringLiteralTypo
        return
        [
            new ElevationTestData("Eremita Tank", 36.0716876, -112.2542091, 1939.62),
            new ElevationTestData("Vesta Temple", 36.0935096, -112.2689987, 1920.14),
            new ElevationTestData("Whites Butte", 36.0975983, -112.2312066, 1466.52),
            new ElevationTestData("Bass Tank", 36.094258, -112.3739584, 1900.92),
            new ElevationTestData("Pima Point", 36.0719269, -112.2001712, 1988.03),
            new ElevationTestData("Tower of Set", 36.1211306, -112.1780426, 1830.97)
        ];
        // ReSharper restore StringLiteralTypo
    }

    [Test]
    public async Task Line_GpxLineFromHorseshoeMesaFileWithMeasurements()
    {
        var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
            "GrandCanyonHorseShoeMesaEastSideLoop.gpx"));
        Assert.That(testFile.Exists, "Test File Found");

        var tracks = await GpxTools.TracksFromGpxFile(testFile, DebugTrackers.DebugProgressTracker());

        Assert.That(tracks.Count, Is.EqualTo(1), "Should find 1 track");

        var coordinateList = tracks.First().Track;

        var metricStats = DistanceTools.LineStatsInMetricFromCoordinateList(coordinateList);
        var imperialStats = DistanceTools.LineStatsInImperialFromMetricStats(metricStats);

        Assert.Multiple(() =>
        {
            Assert.That(imperialStats.Length.IsApproximatelyEqualTo(13, .3),
                    $"ExpertGPS Length 13.03, Measured {imperialStats.Length}");
            Assert.That(imperialStats.ElevationClimb.IsApproximatelyEqualTo(9000, 100),
                $"ExpertGPS Climb 9023, Measured {imperialStats.ElevationClimb}");
            Assert.That(imperialStats.ElevationDescent.IsApproximatelyEqualTo(8932, 100),
                $"ExpertGPS Descent 8932, Measured {imperialStats.ElevationDescent}");
            Assert.That(imperialStats.MinimumElevation.IsApproximatelyEqualTo(3591, 30),
                $"ExpertGPS Min Elevation 13.03, Measured {imperialStats.MinimumElevation}");
            Assert.That(imperialStats.MaximumElevation.IsApproximatelyEqualTo(7384, 30),
                $"ExpertGPS Max Elevation 13.03, Measured {imperialStats.MaximumElevation}");
        });
    }

    [Test]
    public async Task Line_SanPedroTwoTrackFile()
    {
        var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
            "TwoTrackGpxNearTheSanPedro.gpx"));
        Assert.That(testFile.Exists, "Test File Found");

        var tracks = await GpxTools.TracksFromGpxFile(testFile, DebugTrackers.DebugProgressTracker());

        Assert.That(tracks.Count, Is.EqualTo(2), "Should find 2 tracks");
        Assert.That(tracks.All(x => !string.IsNullOrWhiteSpace(x.Description)), "Found Tracks with Blank Description?");

        var shortTrack = tracks.OrderBy(x => x.Track.Count).First().Track;

        Assert.That(shortTrack.Count, Is.EqualTo(214), "Unexpected Point Count");

        var preElevationReplacementStats = DistanceTools.LineStatsInImperialFromCoordinateList(shortTrack);

        Assert.Multiple(() =>
        {
            Assert.That(preElevationReplacementStats.Length.IsApproximatelyEqualTo(2.8, .05),
                    $"ExpertGPS Length 2.79, Measured {preElevationReplacementStats.Length}");
            Assert.That(preElevationReplacementStats.ElevationClimb.IsApproximatelyEqualTo(158, 1),
                $"ExpertGPS Climb 158.4, Measured {preElevationReplacementStats.ElevationClimb}");
            Assert.That(preElevationReplacementStats.ElevationDescent.IsApproximatelyEqualTo(285, 1),
                $"ExpertGPS Descent 285.6, Measured {preElevationReplacementStats.ElevationDescent}");
            Assert.That(preElevationReplacementStats.MinimumElevation.IsApproximatelyEqualTo(3795, 1),
                $"ExpertGPS Min Elevation 3795.25, Measured {preElevationReplacementStats.MinimumElevation}");
            Assert.That(preElevationReplacementStats.MaximumElevation.IsApproximatelyEqualTo(3944, 1),
                $"ExpertGPS Max Elevation 3944.76, Measured {preElevationReplacementStats.MaximumElevation}");
        });

        await ElevationService.OpenTopoMapZenElevation(shortTrack,
            DebugTrackers.DebugProgressTracker());

        Assert.That(shortTrack.All(x => x.Z > 0), "After Elevation replacement some 0 values found");

        var postElevationReplacementStats = DistanceTools.LineStatsInImperialFromCoordinateList(shortTrack);

        Assert.Multiple(() =>
        {
            Assert.That(postElevationReplacementStats.Length.IsApproximatelyEqualTo(2.8, .05),
                    $"ExpertGPS Length 2.79, Measured {preElevationReplacementStats.Length}");
            Assert.That(postElevationReplacementStats.ElevationClimb.IsApproximatelyEqualTo(36.08, 1),
                $"Expected 36.08, Measured {preElevationReplacementStats.ElevationClimb}");
            Assert.That(postElevationReplacementStats.ElevationDescent.IsApproximatelyEqualTo(187, 1),
                $"Expected 187, Measured {preElevationReplacementStats.ElevationDescent}");
            Assert.That(postElevationReplacementStats.MinimumElevation.IsApproximatelyEqualTo(3891.07, 1),
                $"Expected 3891, Measured {preElevationReplacementStats.MinimumElevation}");
            Assert.That(postElevationReplacementStats.MaximumElevation.IsApproximatelyEqualTo(4041.99, 1),
                $"Expected 4041, Measured {preElevationReplacementStats.MaximumElevation}");
        });
    }


    [Test]
    public async Task Line_SanPedroTwoTrackFileLineToGeoJsonAndBack()
    {
        var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
            "TwoTrackGpxNearTheSanPedro.gpx"));
        Assert.That(testFile.Exists, "Test File Found");

        var tracks = await GpxTools.TracksFromGpxFile(testFile, DebugTrackers.DebugProgressTracker());

        Assert.That(tracks.Count, Is.EqualTo(2), "Should find 2 tracks");
        Assert.That(tracks.All(x => !string.IsNullOrWhiteSpace(x.Description)), "Found Tracks with Blank Description?");

        var shortTrack = tracks.OrderBy(x => x.Track.Count).First().Track;
        var geoJson =
            await LineTools.GeoJsonWithLineStringFromCoordinateList(shortTrack, false,
                DebugTrackers.DebugProgressTracker());
        var shortTrackFromGeoJson = LineTools.CoordinateListFromGeoJsonFeatureCollectionWithLinestring(geoJson);

        Assert.That(shortTrackFromGeoJson.Count, Is.EqualTo(shortTrack.Count), "Count of Track Points does not match");

        for (var i = 0; i < shortTrack.Count; i++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(shortTrackFromGeoJson[i].X, Is.EqualTo(shortTrack[i].X), $"Point {i} X Values don't match");
                Assert.That(shortTrackFromGeoJson[i].Y, Is.EqualTo(shortTrack[i].Y), $"Point {i} Y Values don't match");
                Assert.That(shortTrackFromGeoJson[i].Z, Is.EqualTo(shortTrack[i].Z), $"Point {i} Z Values don't match");
            });
        }
    }

    public record ElevationTestData(string Name, double Latitude, double Longitude, double RoundedElevationInMeters);
}