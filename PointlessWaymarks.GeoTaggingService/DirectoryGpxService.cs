using NetTopologySuite.IO;

namespace PointlessWaymarks.GeoTaggingService;

/// <summary>
///     Pulls .gpx files from a directory for the GeoTag process. This scans files on the first GetGpxTrack request
///     and on subsequent requests does a light verification that the file list is still valid and rescans only
///     if needed. This Service IS NOT intended for situations where this routine is pulling files while edits
///     are being made to the files.
/// </summary>
public class DirectoryGpxService(string directoryWithGpxFiles, bool includeSubdirectories) : IGpxService
{
    private List<(DateTime startDateTime, DateTime endDateTime, FileInfo file)> _gpxFiles = [];

    public async Task<List<WaypointAndSource>> GetGpxPoints(List<DateTime> photoDateTimeUtcList,
        IProgress<string>? progress)
    {
        await VerifyFilesAndRescanIfNeeded(progress);

        //This is a brute force approach since the expectation is local files.
        List<(DateTime startDateTime, DateTime endDateTime, FileInfo file)> possibleFiles = [];

        foreach (var loopPhotoDateTime in photoDateTimeUtcList)
            possibleFiles.AddRange(_gpxFiles.Where(x =>
                loopPhotoDateTime >= x.startDateTime && loopPhotoDateTime <= x.endDateTime &&
                !possibleFiles.Any(y => x.file.FullName.Equals(y.file.FullName))).ToList());

        var allPointsList = new List<WaypointAndSource>();

        foreach (var loopFile in possibleFiles)
        {
            var gpx = GpxFile.Parse(await File.ReadAllTextAsync(loopFile.file.FullName),
                new GpxReaderSettings
                {
                    BuildWebLinksForVeryLongUriValues = true,
                    IgnoreBadDateTime = true,
                    IgnoreUnexpectedChildrenOfTopLevelElement = true,
                    IgnoreVersionAttribute = true
                });

            if (!gpx.Tracks.Any(t => t.Segments.SelectMany(y => y.Waypoints).Count() > 1)) continue;

            allPointsList.AddRange(gpx.Tracks.SelectMany(x => x.Segments).SelectMany(x => x.Waypoints)
                .Select(x => new WaypointAndSource(x, loopFile.file.Name))
                .OrderBy(x => x.Waypoint.TimestampUtc)
                .ToList());
        }

        progress?.Report($"Found {allPointsList.Count} Points");

        return allPointsList;
    }

    public async Task ScanGpxFiles(IProgress<string>? progress)
    {
        var gpxDirectory = new DirectoryInfo(directoryWithGpxFiles);

        if (!gpxDirectory.Exists)
        {
            progress?.Report($"Directory Gpx Service - {directoryWithGpxFiles} not found - nothing returned...");
            _gpxFiles = [];
            return;
        }

        var newGpxList = new List<(DateTime startDateTime, DateTime endDateTime, FileInfo file)>();

        var gpxFiles = gpxDirectory.EnumerateFiles("*.gpx",
            new EnumerationOptions
            {
                RecurseSubdirectories = includeSubdirectories, MatchCasing = MatchCasing.CaseInsensitive,
                IgnoreInaccessible = true
            }).ToList();

        progress?.Report(
            $"Directory Gpx Service - {gpxFiles.Count} found to scan for tracks and waypoints that can be used for geo-tagging");

        var counter = 0;

        foreach (var loopGpx in gpxFiles)
        {
            if (++counter % 50 == 0) progress?.Report($"Directory Gpx Service - {counter} of {gpxFiles.Count}");

            var gpx = GpxFile.Parse(await File.ReadAllTextAsync(loopGpx.FullName),
                new GpxReaderSettings
                {
                    BuildWebLinksForVeryLongUriValues = true,
                    IgnoreBadDateTime = true,
                    IgnoreUnexpectedChildrenOfTopLevelElement = true,
                    IgnoreVersionAttribute = true
                });

            var allPoints = new List<GpxWaypoint>();

            allPoints.AddRange(gpx.Tracks.SelectMany(x => x.Segments).SelectMany(x => x.Waypoints)
                .Where(x => x.TimestampUtc is not null));

            allPoints.AddRange(gpx.Waypoints.Where(x => x.TimestampUtc is not null));

            if (!allPoints.Any())
            {
                progress?.Report($"Directory Gpx Service - {loopGpx.FullName} no points for use GeoTagging found");
                continue;
            }

            var timestampMin = allPoints.Where(x => x.TimestampUtc != null).MinBy(x => x.TimestampUtc!.Value)!
                .TimestampUtc;
            var timestampMax = allPoints.Where(x => x.TimestampUtc != null).MaxBy(x => x.TimestampUtc!.Value)!
                .TimestampUtc;

            if (timestampMin != null && timestampMax != null)
            {
                var toAdd = (timestampMin.Value,
                    timestampMax.Value, loopGpx);

                newGpxList.Add(toAdd);

                progress?.Report(
                    $"Directory Gpx Service - {toAdd.loopGpx.FullName} UTC from {toAdd.Item1} to {toAdd.Item2}");
            }
        }

        _gpxFiles = newGpxList;
    }

    public async Task VerifyFilesAndRescanIfNeeded(IProgress<string>? progress)
    {
        if (!_gpxFiles.Any())
        {
            progress?.Report("No files currently found - Rescanning");
            await ScanGpxFiles(progress);
            return;
        }

        var filesNotPresent = _gpxFiles.Any(x =>
        {
            x.file.Refresh();
            return x.file.Exists;
        });

        if (filesNotPresent)
        {
            progress?.Report("Files found in Gpx List that are no longer present - Rescanning");
            await ScanGpxFiles(progress);
            return;
        }

        var gpxDirectory = new DirectoryInfo(directoryWithGpxFiles);

        var countOfGpxFiles = gpxDirectory.EnumerateFiles("*.gpx",
            new EnumerationOptions
            {
                RecurseSubdirectories = includeSubdirectories,
                MatchCasing = MatchCasing.CaseInsensitive,
                IgnoreInaccessible = true
            }).Count();

        if (countOfGpxFiles != _gpxFiles.Count)
        {
            progress?.Report("Count of Gpx files in Directory does not match current file list - Rescanning");
            await ScanGpxFiles(progress);
        }
    }
}