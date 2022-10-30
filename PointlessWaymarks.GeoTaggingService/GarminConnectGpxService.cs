using Garmin.Connect;
using Garmin.Connect.Auth;
using NetTopologySuite.IO;
using PointlessWaymarks.Task.GarminConnectGpxImport;

namespace PointlessWaymarks.GeoTaggingService;

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

    public async Task<List<GpxWaypoint>> GetGpxTrack(DateTime photoDateTimeUtc)
    {
        var authParameters = new BasicAuthParameters(ConnectUsername, ConnectPassword);
        var client = new GarminConnectClient(new GarminConnectContext(new HttpClient(), authParameters));

        var activityList = await client.GetActivitiesByDate(photoDateTimeUtc.Date.AddDays(-SearchSurroundingDays),
            photoDateTimeUtc.Date.AddDays(SearchSurroundingDays), string.Empty);

        var activity = activityList.FirstOrDefault(x =>
            photoDateTimeUtc >= x.StartTimeGmt && photoDateTimeUtc <= x.StartTimeGmt.AddSeconds(x.Duration));

        if (activity is null) return new List<GpxWaypoint>();

        var gpxFile = await GarminConnectTools.GetGpx(activity, new DirectoryInfo(ArchiveDirectory), false,
            ConnectUsername, ConnectPassword);

        if (gpxFile is null) return new List<GpxWaypoint>();

        var gpx = GpxFile.Parse(await File.ReadAllTextAsync(gpxFile.FullName),
            new GpxReaderSettings
            {
                BuildWebLinksForVeryLongUriValues = true,
                IgnoreBadDateTime = true,
                IgnoreUnexpectedChildrenOfTopLevelElement = true,
                IgnoreVersionAttribute = true
            });

        if (!gpx.Tracks.Any(t => t.Segments.SelectMany(y => y.Waypoints).Count() > 1)) return new List<GpxWaypoint>();

        return gpx.Tracks.SelectMany(x => x.Segments).SelectMany(x => x.Waypoints).OrderBy(x => x.TimestampUtc)
            .ToList();
    }
}