using System.Text.Json;
using System.Text.Json.Serialization;
using Garmin.Connect;
using Garmin.Connect.Auth;
using Garmin.Connect.Models;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CommonTools;
using Polly;
using Serilog;

namespace PointlessWaymarks.Task.GarminConnectGpxImport;

public static class GarminConnectTools
{
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

    public static async Task<FileInfo?> GetGpx(GarminActivity activity, DirectoryInfo archiveDirectory,
        bool overwriteExistingFile, string connectUserName, string connectPassword)
    {
        if (!archiveDirectory.Exists)
        {
            archiveDirectory.Create();
            archiveDirectory.Refresh();
            if (!archiveDirectory.Exists)
                throw new DirectoryNotFoundException(
                    $"Directory {archiveDirectory.FullName} not found and could not be created.");
        }

        var safeGpxFile =
            new FileInfo(Path.Combine(archiveDirectory.FullName, $"{ArchiveBaseFileName(activity)}.gpx"));

        if (safeGpxFile.Exists && !overwriteExistingFile)
        {
            Console.WriteLine();
            Log.Verbose(
                $"Found GPX File {safeGpxFile.FullName} in existing files.");
            return safeGpxFile;
        }

        if (safeGpxFile.Exists)
        {
            Log.Verbose(
                $"Deleting Garmin GPX Archive File {safeGpxFile.FullName}.");
            safeGpxFile.Delete();
            safeGpxFile.Refresh();
        }

        var authParameters = new BasicAuthParameters(connectUserName, connectPassword);
        var client = new GarminConnectClient(new GarminConnectContext(new HttpClient(), authParameters));

        var file = await Policy.Handle<Exception>().WaitAndRetryAsync(3, i => TimeSpan.FromMinutes(1 * i))
            .ExecuteAsync(async () =>
                await client.DownloadActivity(activity.ActivityId, ActivityDownloadFormat.GPX));

        if (file is null) return null;

        await File.WriteAllBytesAsync(safeGpxFile.FullName, file);
        safeGpxFile.Refresh();

        return safeGpxFile;
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