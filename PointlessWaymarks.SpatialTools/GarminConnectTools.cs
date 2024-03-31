using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Garmin.Connect.Models;
using PointlessWaymarks.CommonTools;
using Serilog;

namespace PointlessWaymarks.SpatialTools;

public static partial class GarminConnectTools
{
    private static List<(long activityId, FileInfo file)> ActivityIdAndFile(List<FileInfo> toProcess)
    {
        var returnList = new List<(long activityId, FileInfo file)>();

        foreach (var loopFiles in toProcess)
        {
            var idStringMatch = GarminArchiveActivityIdRegex().Match(loopFiles.Name);
            if (!idStringMatch.Groups.ContainsKey("gcID")) continue;

            var stringId = idStringMatch.Groups["gcId"].Value;
            var idParsed = long.TryParse(stringId, out var id);

            if (!idParsed || id < 1) continue;

            returnList.Add((id, loopFiles));
        }

        return returnList;
    }

    public static string ArchiveBaseFileName(GarminActivity activity)
    {
        var name = activity.ActivityName;
        var locationName = activity.LocationName;
        var activityDateString = activity.StartTimeLocal.ToString("yyyy-MM-dd-hh-tt");
        var activityIdString = activity.ActivityId.ToString();
        var nameMaxSafeLength = 240 - activityDateString.Length - activityIdString.Length;
        var activitySafeName = $"{name}-{locationName}".Truncate(nameMaxSafeLength);
        var safeFileName = SlugTools.CreateSlug(false,
            $"{activityDateString}-{activitySafeName}--gc{activityIdString}", 250);

        return safeFileName;
    }

    public static string ArchiveGpxFileName(GarminActivity activity)
    {
        return $"{ArchiveBaseFileName(activity)}.gpx";
    }

    public static string ArchiveJsonFileName(GarminActivity activity)
    {
        return $"{ArchiveBaseFileName(activity)}.json";
    }

    [GeneratedRegex(@".*-gc(?<gcId>.*)\..*",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
    private static partial Regex GarminArchiveActivityIdRegex();

    public static async Task<FileInfo?> GetGpx(GarminActivity activity, DirectoryInfo archiveDirectory,
        bool tryCreateDirectoryIfNotFound, bool overwriteExistingFile, IRemoteGpxService connectWrapper, IProgress<string> progress)
    {
        if (!archiveDirectory.Exists)
        {
            progress.Report($"Activity/GPX Archive Directory {archiveDirectory.FullName} not found - Creating...");
            
            if (tryCreateDirectoryIfNotFound)
            {
                archiveDirectory.Create();
                archiveDirectory.Refresh();
                if (!archiveDirectory.Exists)
                    throw new DirectoryNotFoundException(
                        $"Directory {archiveDirectory.FullName} not found and could not be created.");
            }
            else
            {
                throw new DirectoryNotFoundException(
                    $"Directory {archiveDirectory.FullName} not found.");
            }
        }


        var safeGpxFile =
            new FileInfo(Path.Combine(archiveDirectory.FullName, $"{ArchiveBaseFileName(activity)}.gpx"));

        if (safeGpxFile.Exists && !overwriteExistingFile)
        {
            progress.Report(
                $" Found GPX File {safeGpxFile.FullName} in existing files.");
            return safeGpxFile;
        }

        if (safeGpxFile.Exists)
        {
            progress.Report(
                $"Deleting Garmin GPX Archive File {safeGpxFile.FullName}.");
            safeGpxFile.Delete();
            safeGpxFile.Refresh();
        }

        progress.Report($"Downloading Activity Id {activity.ActivityId} to GPX File {safeGpxFile.FullName}.");

        var downloadedGpx = await connectWrapper.DownloadGpxFile(activity.ActivityId, safeGpxFile.FullName);

        return downloadedGpx;
    }

    public static List<(long activityId, FileInfo file)> GpxActivityFilesFromDirectory(string directoryName)
    {
        if (string.IsNullOrWhiteSpace(directoryName) || !Directory.Exists(directoryName))
            return [];

        var archiveDirectory = new DirectoryInfo(directoryName);
        var archiveJsonFiles = archiveDirectory.EnumerateFiles("*-gc*.gpx", SearchOption.TopDirectoryOnly).ToList();

        return ActivityIdAndFile(archiveJsonFiles);
    }

    public static List<(long activityId, FileInfo file)> JsonActivityFilesFromDirectory(string directoryName)
    {
        if (string.IsNullOrWhiteSpace(directoryName) || !Directory.Exists(directoryName))
            return [];

        var archiveDirectory = new DirectoryInfo(directoryName);
        var archiveJsonFiles = archiveDirectory.EnumerateFiles("*-gc*.json", SearchOption.TopDirectoryOnly).ToList();

        return ActivityIdAndFile(archiveJsonFiles);
    }

    public static async Task<List<GarminActivity>> Search(DateTime searchStart, DateTime searchEnd,
        IRemoteGpxService connectWrapper, CancellationToken cancellationToken, IProgress<string>? progress = null)
    {
        //9/25/2022 - I haven't done any research or extensive testing but the assumption here is
        //that for large search ranges that it will be better to only query Garmin Connect for a limited
        //number of days...
        var searchSegmentLength = 100;

        var searchDateRanges = new List<(DateTime startDate, DateTime endDate)>();

        var searchDays = searchEnd.Subtract(searchEnd).Days;

        var searchRanges = searchDays / searchSegmentLength + (searchDays % searchSegmentLength == 0 ? 0 : 1);

        for (var i = 0; i <= searchRanges; i++)
        {
            var start = searchStart.AddDays(searchDays * i);
            var end = i == searchRanges ? searchEnd : searchStart.AddDays(searchDays * (i + 1));
            searchDateRanges.Add((start, end));
        }

        var returnList = new List<GarminActivity>();

        progress?.Report($"Looping thru {searchDateRanges.Count} Date Range Search Periods");
        var counter = 0;

        foreach (var loopDateSearchRange in searchDateRanges)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(
                $"Sending Query to Garmin Connect for From {loopDateSearchRange.startDate} to {loopDateSearchRange.endDate} - {++counter} of {searchDateRanges.Count}");

            var activityList = await connectWrapper.GetActivityList(loopDateSearchRange.startDate,
                loopDateSearchRange.endDate);

            if (!activityList.Any())
            {
                progress?.Report(
                    $"No Activities Found From {loopDateSearchRange.startDate} to {loopDateSearchRange.endDate} - Continuing");
                continue;
            }

            progress?.Report(
                $"Found {activityList.Count} Activities From {loopDateSearchRange.startDate} to {loopDateSearchRange.endDate}");

            returnList.AddRange(activityList);
        }

        return returnList.OrderByDescending(x => x.StartTimeLocal).ToList();
    }

    public static async Task<FileInfo> WriteJsonActivityArchiveFile(GarminActivity activity,
        DirectoryInfo archiveDirectory,
        bool overwriteExistingFile)
    {
        if (!archiveDirectory.Exists)
        {
            archiveDirectory.Create();
            archiveDirectory.Refresh();
            if (!archiveDirectory.Exists)
                throw new DirectoryNotFoundException(
                    $"Directory {archiveDirectory.FullName} not found and could not be created.");
        }

        var safeJsonFile =
            new FileInfo(Path.Combine(archiveDirectory.FullName, $"{ArchiveBaseFileName(activity)}.json"));

        if (safeJsonFile.Exists && !overwriteExistingFile)
        {
            Console.WriteLine();
            Log.Verbose(
                $"Found Garmin Json Activity Archive File {safeJsonFile.FullName} in existing files.");
            return safeJsonFile;
        }

        if (safeJsonFile.Exists)
        {
            Log.Verbose(
                $"Deleting Garmin Json Activity Archive File {safeJsonFile.FullName}.");
            safeJsonFile.Delete();
            safeJsonFile.Refresh();
        }

        await File.WriteAllTextAsync(safeJsonFile.FullName,
            JsonSerializer.Serialize(activity,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }));
        safeJsonFile.Refresh();

        Log.Verbose($"Wrote Garmin Activity Archive Json File - {safeJsonFile.FullName}");

        return safeJsonFile;
    }
}