using System.Globalization;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using NetTopologySuite.IO;
using Serilog;
using File = TagLib.File;

namespace PointlessWaymarks.GeoTaggingService;

public class GeoTag
{
    public static bool FileHasLatLong(FileInfo loopFile, IProgress<string>? progress)
    {
        var gpsDirectory = ImageMetadataReader.ReadMetadata(loopFile.FullName).OfType<GpsDirectory>()
            .FirstOrDefault();

        if (gpsDirectory is { IsEmpty: false }) return false;

        var geoLocation = gpsDirectory.GetGeoLocation();

        if (geoLocation?.IsZero ?? true) return false;

        return true;
    }

    public static DateTime? FileUtcCreatedOn(FileInfo loopFile, IProgress<string>? progress)
    {
        var exifSubIfDirectory = ImageMetadataReader.ReadMetadata(loopFile.FullName)
            .OfType<ExifSubIfdDirectory>()
            .FirstOrDefault();
        var gpsDirectory = ImageMetadataReader.ReadMetadata(loopFile.FullName).OfType<GpsDirectory>()
            .FirstOrDefault();

        if (gpsDirectory?.TryGetGpsDate(out var gpsDateTime) ?? false)
            if (gpsDateTime != DateTime.MinValue)
            {
                gpsDateTime = DateTime.SpecifyKind(gpsDateTime, DateTimeKind.Utc);
                progress?.Report($"GeoTag - {loopFile.FullName} using GPS UTC Time {gpsDateTime}");
                Log.Verbose($"GeoTag - {loopFile.FullName} using GPS UTC Time {gpsDateTime}");
                return gpsDateTime;
            }

        var createdOn = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

        if (string.IsNullOrEmpty(createdOn))
        {
            progress?.Report(
                $"GeoTag - No TagDateTimeOriginal found in ExifSubIfdDirectory for {loopFile.FullName} - skipping");
            Log.Verbose(
                $"GeoTag - No TagDateTimeOriginal found in ExifSubIfdDirectory for {loopFile.FullName} - skipping");
            return null;
        }

        var createdOnParsed = DateTime.TryParseExact(createdOn, "yyyy:MM:dd HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate);

        if (!createdOnParsed)
        {
            progress?.Report(
                $"GeoTag - TagDateTimeOriginal - {createdOn} - could not be parsed into a valid DateTime - skipping");
            Log.Verbose(
                $"GeoTag - TagDateTimeOriginal - {createdOn} - could not be parsed into a valid DateTime - skipping");
            return null;
        }

        var createdOnUtcOffsetString = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagTimeZone);
        var createOnUtcOffsetTimespan = new TimeSpan(0);
        var createOnUtcOffsetIsValid = !string.IsNullOrWhiteSpace(createdOnUtcOffsetString) &&
                                       TimeSpan.TryParse(createdOnUtcOffsetString,
                                           out createOnUtcOffsetTimespan);

        if (!createOnUtcOffsetIsValid)
        {
            progress?.Report(
                $"GeoTag - Valid TagTimeZone not found in ExifSubIfdDirectory for {loopFile.FullName} - skipping");
            Log.Verbose(
                $"GeoTag - Valid TagTimeZone not found in ExifSubIfdDirectory for {loopFile.FullName} - skipping");
            return null;
        }

        return parsedDate.Subtract(createOnUtcOffsetTimespan);
    }

    public async System.Threading.Tasks.Task Tag(List<FileInfo> filesToTag, List<IGpxService> gpxServices,
        int pointMustBeWithinMinutes, int adjustCreatedTimeInMinutes, bool overwriteExistingLatLong,
        IProgress<string>? progress = null)
    {
        Log.Information(
            $"GeoTag - Starting - {filesToTag.Count} Files, {gpxServices.Count} GpxServices, {pointMustBeWithinMinutes} Within Minutes, {adjustCreatedTimeInMinutes} Adjustment, Overwrite {overwriteExistingLatLong}");

        if (!filesToTag.Any())
        {
            progress?.Report("GeoTag - No files to tag, ending...");
            return;
        }

        if (!gpxServices.Any())
        {
            progress?.Report("GeoTag - No GPX Services to tag with, ending...");
            return;
        }

        var counter = 0;

        var toTag = new List<(DateTime createdUtc, FileInfo file)>();

        progress?.Report("GeoTag - Getting Metadata");

        foreach (var loopFile in filesToTag)
        {
            counter++;

            if (counter % 50 == 0)
                progress?.Report($"GeoTag - Processing Metadata - File {counter} of {filesToTag.Count}");

            if (!loopFile.Exists)
            {
                progress?.Report($"GeoTag - File {loopFile.FullName} doesn't exist - skipping");
                Log.Information($"GeoTag - Ignoring {loopFile.FullName} - Does Not Exist");
                return;
            }

            var hasExistingLatLong = FileHasLatLong(loopFile, progress);

            if (!overwriteExistingLatLong && hasExistingLatLong)
            {
                progress?.Report(
                    $"GeoTag - File {loopFile.FullName} Already Has a GeoLocation and Overwrite Existing is Set to {overwriteExistingLatLong} - skipping");
                Log.Information($"GeoTag - Ignoring {loopFile.FullName} - Existing GeoLocation");
                continue;
            }

            var createdOnUtc = FileUtcCreatedOn(loopFile, progress);

            if (createdOnUtc == null)
            {
                progress?.Report(
                    $"GeoTag - Valid TagTimeZone not found in ExifSubIfdDirectory for {loopFile.FullName} - skipping");
                Log.Verbose(
                    $"GeoTag - Valid TagTimeZone not found in ExifSubIfdDirectory for {loopFile.FullName} - skipping");
                continue;
            }

            toTag.Add((createdOnUtc.Value.AddMinutes(adjustCreatedTimeInMinutes), loopFile));
        }

        var pointLists = new List<(DateTime start, DateTime end, List<GpxWaypoint> waypoints)>();

        foreach (var loopTagItem in toTag)
        {
            if (pointLists.Any(x => loopTagItem.createdUtc >= x.start && loopTagItem.createdUtc <= x.end)) continue;

            var pointsCollection = new List<GpxWaypoint>();

            foreach (var loopService in gpxServices)
                pointsCollection.AddRange(
                    (await loopService.GetGpxTrack(loopTagItem.createdUtc)).Where(x => x.TimestampUtc != null));

            pointLists.Add((pointsCollection.MinBy(x => x.TimestampUtc!.Value).TimestampUtc.Value,
                pointsCollection.MaxBy(x => x.TimestampUtc!.Value).TimestampUtc.Value, pointsCollection));
        }

        var allPoints = pointLists.SelectMany(x => x.waypoints).ToList();

        foreach (var loopTagItem in toTag)
        {
            var possibleTagPoints = allPoints.Where(x =>
                loopTagItem.createdUtc.AddMinutes(-Math.Abs(pointMustBeWithinMinutes)) >= x.TimestampUtc &&
                x.TimestampUtc <= loopTagItem.createdUtc.AddMinutes(Math.Abs(pointMustBeWithinMinutes))).ToList();

            if (!possibleTagPoints.Any())
            {
                progress?.Report(
                    $"GeoTag - No Points Found for {loopTagItem.file.FullName}");
                Log.Verbose(
                    $"GeoTag - No Points Found for {loopTagItem.file.FullName}");
                continue;
            }

            var closest = possibleTagPoints.MinBy(x =>
                Math.Abs(loopTagItem.createdUtc.Subtract(x.TimestampUtc.Value).TotalMicroseconds));

            var tagSharpFile = File.Create(loopTagItem.file.FullName) as TagLib.Image.File;
            tagSharpFile.ImageTag.Latitude = closest.Latitude;
            tagSharpFile.ImageTag.Longitude = closest.Longitude;
            tagSharpFile.Save();
        }
    }
}