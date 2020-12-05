using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using PointlessWaymarksCmsData.Spatial.Elevation;

namespace PointlessWaymarksTests
{
    public class ElevationServiceTests
    {
        public List<ElevationTestData> GrandCanyonMapZenElevations()
        {
            //These elevations were verified as the return value from MapZen on 12/4 - and confirmed that these are close to the GNIS values
            return new List<ElevationTestData>
            {
                new("Eremita Tank", 36.0716876, -112.2542091, 1940),
                new("Vesta Temple", 36.0935096, -112.2689987, 1912),
                new("Whites Butte", 36.0975983, -112.2312066, 1462),
                new("Bass Tank", 36.094258, -112.3739584, 1906),
                new("Pima Point", 36.0719269, -112.2001712, 1995),
                new("Tower of Set", 36.1211306, -112.1780426, 1830),
            };
        }

        public List<ElevationTestData> GrandCanyonNedElevations()
        {
            //These elevations were verified as the return value from MapZen on 12/4 - and confirmed that these are close to the GNIS values
            return new List<ElevationTestData>
            {
                new("Eremita Tank", 36.0716876, -112.2542091, 1939.62),
                new("Vesta Temple", 36.0935096, -112.2689987, 1920.14),
                new("Whites Butte", 36.0975983, -112.2312066, 1466.52),
                new("Bass Tank", 36.094258, -112.3739584, 1900.92),
                new("Pima Point", 36.0719269, -112.2001712, 1988.03),
                new("Tower of Set", 36.1211306, -112.1780426, 1830.97),
            };
        }

        [Test]
        public async Task MultiPointMapZenTest()
        {
            var client = new HttpClient();

            var testData = GrandCanyonMapZenElevations().Select(x => new CoordinateZ(x.Longitude, x.Latitude, 0))
                .ToList();

            await ElevationService.OpenTopoMapZenElevation(client, testData, DebugTrackers.DebugProgressTracker());

            Assert.IsTrue(testData.All(x => x.Z > 0), "Not all point have an elevation greater than zero.");

            var referenceData = GrandCanyonMapZenElevations();

            foreach (var loopTestData in testData)
            {
                var referenceItem =
                    referenceData.Single(x => x.Latitude == loopTestData.Y && x.Longitude == loopTestData.X);
                Assert.AreEqual(Math.Round(referenceItem.RoundedElevationInMeters, 2), Math.Round(loopTestData.Z, 2),
                    $"{referenceItem.Name} didn't match expected");
            }
        }

        [Test]
        public async Task MultiPointNedTest()
        {
            var client = new HttpClient();

            var testData = GrandCanyonNedElevations().Select(x => new CoordinateZ(x.Longitude, x.Latitude, 0)).ToList();

            await ElevationService.OpenTopoNedElevation(client, testData, DebugTrackers.DebugProgressTracker());

            Assert.IsTrue(testData.All(x => x.Z > 0), "Not all point have an elevation greater than zero.");

            var referenceData = GrandCanyonNedElevations();

            foreach (var loopTestData in testData)
            {
                var referenceItem =
                    referenceData.Single(x => x.Latitude == loopTestData.Y && x.Longitude == loopTestData.X);
                Assert.AreEqual(Math.Round(referenceItem.RoundedElevationInMeters, 2), Math.Round(loopTestData.Z, 2),
                    $"{referenceItem.Name} didn't match expected");
            }
        }

        [Test]
        public async Task SinglePointMapZenTest()
        {
            var client = new HttpClient();

            var testData = GrandCanyonMapZenElevations();

            foreach (var loopTests in testData)
            {
                var result = await ElevationService.OpenTopoMapZenElevation(client, loopTests.Latitude,
                    loopTests.Longitude, DebugTrackers.DebugProgressTracker());

                Assert.NotNull(result, $"Null result from {loopTests.Name}");
                Assert.AreEqual(loopTests.RoundedElevationInMeters, Math.Round(result.Value, 2), $"{loopTests.Name}");
            }
        }

        [Test]
        public async Task SinglePointNedTest()
        {
            var client = new HttpClient();

            var testData = GrandCanyonNedElevations();

            foreach (var loopTests in testData)
            {
                var result = await ElevationService.OpenTopoNedElevation(client, loopTests.Latitude,
                    loopTests.Longitude, DebugTrackers.DebugProgressTracker());

                Assert.NotNull(result, $"Null result from {loopTests.Name}");
                Assert.AreEqual(loopTests.RoundedElevationInMeters, Math.Round(result.Value, 2), $"{loopTests.Name}");
            }
        }

        public record ElevationTestData(string Name, double Latitude, double Longitude,
            double RoundedElevationInMeters);
    }
}