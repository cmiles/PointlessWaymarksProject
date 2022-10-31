using Garmin.Connect;
using Garmin.Connect.Auth;
using Garmin.Connect.Models;
using NetTopologySuite.IO;
using PointlessWaymarks.Task.GarminConnectGpxImport;

namespace PointlessWaymarks.GeoTaggingService;

/// <summary>
/// </summary>
public class GarminConnectGpxService : IGpxService
{
    public GarminConnectGpxService(string archiveDirectory, string connectUsername, string connectPassword)
    {
        ArchiveDirectory = archiveDirectory;
        ConnectUsername = connectUsername;
        ConnectPassword = connectPassword;
    }

    public string ArchiveDirectory { get; }
    public string ConnectPassword { get; }
    public string ConnectUsername { get; }
    public int SearchSurroundingDays { get; set; } = 7;

    public async Task<List<GpxWaypoint>> GetGpxTrack(DateTime photoDateTimeUtc, IProgress<string>? progress)
    {
        var authParameters = new BasicAuthParameters(ConnectUsername, ConnectPassword);
        var client = new GarminConnectClient(new GarminConnectContext(new HttpClient(), authParameters));

        var searchStateDate = photoDateTimeUtc.Date.AddDays(-SearchSurroundingDays);
        var searchEndDate = photoDateTimeUtc.Date.AddDays(SearchSurroundingDays);

        progress?.Report(
            $"Querying Garmin for Activities Starting {searchStateDate} to {searchEndDate} ({SearchSurroundingDays} Surrounding Days)");

        var activityList = await client.GetActivitiesByDate(searchStateDate, searchEndDate, string.Empty) ??
                           Array.Empty<GarminActivity>();

        if (!activityList.Any())
        {
            progress?.Report("No Activities Found in Surrounding Time Period");
            return new List<GpxWaypoint>();
        }

        var activities = activityList.Where(x =>
            photoDateTimeUtc >= x.StartTimeGmt && photoDateTimeUtc <= x.StartTimeGmt.AddSeconds(x.Duration)).ToList();

        if (!activities.Any())
        {
            progress?.Report($"No Activities Found for Photo UTC Time - {photoDateTimeUtc}");
            return new List<GpxWaypoint>();
        }

        progress?.Report($"Found {activities.Count} Activity");

        var allPointsList = new List<GpxWaypoint>();

        foreach (var loopActivity in activities)
        {
            var gpxFile = await GarminConnectTools.GetGpx(loopActivity, new DirectoryInfo(ArchiveDirectory), false,
                ConnectUsername, ConnectPassword);

            if (gpxFile is null) continue;

            var gpx = GpxFile.Parse(await File.ReadAllTextAsync(gpxFile.FullName),
                new GpxReaderSettings
                {
                    BuildWebLinksForVeryLongUriValues = true,
                    IgnoreBadDateTime = true,
                    IgnoreUnexpectedChildrenOfTopLevelElement = true,
                    IgnoreVersionAttribute = true
                });

            if (!gpx.Tracks.Any(t => t.Segments.SelectMany(y => y.Waypoints).Count() > 1)) continue;

            allPointsList.AddRange(gpx.Tracks.SelectMany(x => x.Segments).SelectMany(x => x.Waypoints)
                .OrderBy(x => x.TimestampUtc)
                .ToList());
        }

        progress?.Report($"Found {allPointsList.Count} Points from Garmin Connect Activities");

        return allPointsList;
    }
}