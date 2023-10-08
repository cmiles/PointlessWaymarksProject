using Garmin.Connect.Models;
using NetTopologySuite.IO;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.GeoTaggingService;

/// <summary>
/// </summary>
public class GarminConnectGpxService : IGpxService
{
    public GarminConnectGpxService(string archiveDirectory, IRemoteGpxService connectWrapper)
    {
        ArchiveDirectory = archiveDirectory;
        ConnectWrapper = connectWrapper;
    }

    public string ArchiveDirectory { get; }
    public IRemoteGpxService ConnectWrapper { get; }
    public int SearchSurroundingDays { get; } = 7;

    public async Task<List<WaypointAndSource>> GetGpxPoints(List<DateTime> photoDateTimeUtcList,
        IProgress<string> progress)
    {
        var photoDates = photoDateTimeUtcList.GroupBy(x => DateOnly.FromDateTime(x.Date)).Select(x => x.Key)
            .OrderBy(x => x).ToList();

        //Garmin Connect Unofficial API use has been reliable for me (5/15/2023 - for a year+ now) but it will
        //gladly return too many requests. I don't know the limits and since it is unofficial that seems both
        //fair and not worth trying to figure out. So the code tries to limit the number of requests.

        var photoRangeList = new List<(DateOnly Start, DateOnly End)>();

        var currentRangeStart = photoDates.First();
        var currentRangeEnd = photoDates.First().AddDays(1);

        //Combine consecutive days into a range - then expand that to a search range and record it
        //  Search Range - currently 7 days before and after the photo date - this is very imperfect
        //         and is somewhat untested but basically this is to try to catch most 
        //         multi-day activities (there are many details that I have not explored related to this!)
        foreach (var loopDate in photoDates.Skip(1))
        {
            if (loopDate >= currentRangeStart && loopDate < currentRangeEnd) continue;

            if (loopDate >= currentRangeStart && loopDate < currentRangeEnd.AddDays(1))
            {
                currentRangeEnd = currentRangeEnd.AddDays(1);
                continue;
            }

            photoRangeList.Add((currentRangeStart.AddDays(-7), currentRangeEnd.AddDays(7)));

            currentRangeStart = loopDate;
            currentRangeEnd = loopDate.AddDays(1);
        }

        //Add the last/current range from the loop

        photoRangeList.Add((currentRangeStart.AddDays(-7), currentRangeEnd.AddDays(7)));

        var searchRangeList = new List<(DateOnly Start, DateOnly End)>();

        currentRangeStart = photoRangeList.First().Start;
        currentRangeEnd = photoRangeList.First().End;

        //Combine overlapping Search Ranges
        foreach (var loopRange in photoRangeList.OrderBy(x => x.Start).Skip(1))
        {
            //Start is inside or the next day from the current range - expand range if needed and continue
            if (loopRange.Start >= currentRangeStart && loopRange.Start < currentRangeEnd.AddDays(1))
            {
                currentRangeEnd = loopRange.End > currentRangeEnd ? loopRange.End : currentRangeEnd;
                continue;
            }

            searchRangeList.Add((currentRangeStart, currentRangeEnd));
            currentRangeStart = loopRange.Start;
            currentRangeEnd = loopRange.End;
        }

        //Add the last/current range from the loop
        searchRangeList.Add((currentRangeStart, currentRangeEnd));

        List<GarminActivity> activityList = new();

        foreach (var loopRanges in searchRangeList)
        {
            var searchStartDate = loopRanges.Start;
            var searchEndDate = loopRanges.End;

            progress.Report(
                $"Querying Garmin for Activities Starting {searchStartDate} to {searchEndDate} ({SearchSurroundingDays} Surrounding Days)");

            activityList.AddRange(await ConnectWrapper.GetActivityList(
                searchStartDate.ToDateTime(new TimeOnly(0, 0)),
                searchEndDate.ToDateTime(new TimeOnly(0, 0))));
        }

        var allPointsList = await ActivitiesToWaypointAndSources(activityList, progress);

        progress.Report($"Found {allPointsList.Count} Points from Garmin Connect Activities");

        return allPointsList;
    }

    private async Task<List<WaypointAndSource>> ActivitiesToWaypointAndSources(List<GarminActivity> activities, IProgress<string> progress)
    {
        var allPointsList = new List<WaypointAndSource>();

        progress.Report($"Getting Points from {activities.Count} Activities");

        foreach (var loopActivity in activities)
        {
            var gpxFile = await GarminConnectTools.GetGpx(loopActivity, new DirectoryInfo(ArchiveDirectory),
                false, false, ConnectWrapper, progress);

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
                .Select(x => new WaypointAndSource(x, loopActivity.ActivityName))
                .OrderBy(x => x.Waypoint.TimestampUtc)
                .ToList());
        }

        return allPointsList;
    }
}