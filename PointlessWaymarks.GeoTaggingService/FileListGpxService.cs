using NetTopologySuite.IO;

namespace PointlessWaymarks.GeoTaggingService;

public class FileListGpxService : IGpxService
{
    private readonly List<FileInfo> _listOfGpxFiles;
    private List<(DateTime startDateTime, DateTime endDateTime, FileInfo file)>? _gpxFiles;

    public FileListGpxService(List<FileInfo> listOfGpxFiles)
    {
        _listOfGpxFiles = listOfGpxFiles;
    }

    public async Task<List<WaypointAndSource>> GetGpxPoints(DateTime photoDateTimeUtc, IProgress<string>? progress)
    {
        if (_gpxFiles == null) await ScanFiles(progress);

        var possibleFiles =
            _gpxFiles!.Where(x => photoDateTimeUtc >= x.startDateTime && photoDateTimeUtc <= x.endDateTime).ToList();

        progress?.Report($"Found {possibleFiles.Count} Gpx Files");

        if (!possibleFiles.Any())
        {
            progress?.Report("No Gpx Files Found");
            return new List<WaypointAndSource>();
        }

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

            allPointsList.AddRange(gpx.Tracks.SelectMany(x => x.Segments).SelectMany(x => x.Waypoints).Select(x => new WaypointAndSource(x, loopFile.file.Name))
                .OrderBy(x => x.Waypoint.TimestampUtc)
                .ToList());
        }

        progress?.Report($"Found {allPointsList.Count} Points");

        return allPointsList;
    }

    public async Task ScanFiles(IProgress<string>? progress)
    {
        if (!_listOfGpxFiles.Any())
        {
            progress?.Report("No GPX files?");
            _gpxFiles = new List<(DateTime startDateTime, DateTime endDateTime, FileInfo file)>();
        }

        var filesNotPresent = _listOfGpxFiles.Where(x =>
        {
            x.Refresh();
            return x.Exists;
        }).ToList();

        if (filesNotPresent.Any())
            progress?.Report(
                $"Files found in Gpx List that are no longer present - skipping {filesNotPresent.Count} files - {string.Join(" ,", filesNotPresent.Select(x => x.FullName))}");

        var newGpxList = new List<(DateTime startDateTime, DateTime endDateTime, FileInfo file)>();

        var counter = 0;

        var existingGpxFiles = _listOfGpxFiles.Where(x => x.Exists).ToList();

        foreach (var loopGpx in existingGpxFiles)
        {
            if (++counter % 50 == 0) progress?.Report($"File List Gpx Service - {counter} of {existingGpxFiles.Count}");

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
                progress?.Report($"File List Gpx Service - {loopGpx.FullName} no points for use GeoTagging found");
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
                    $"File Gpx Service - {toAdd.loopGpx.FullName} UTC from {toAdd.Item1} to {toAdd.Item2}");
            }
        }

        _gpxFiles = newGpxList;
    }
}