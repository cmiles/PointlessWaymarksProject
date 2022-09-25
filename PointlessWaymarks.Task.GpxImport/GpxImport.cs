using System.Text.Json;
using System.Text.Json.Serialization;
using Garmin.Connect;
using Garmin.Connect.Auth;
using Garmin.Connect.Models;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using Serilog;

namespace PointlessWaymarks.Task.GarminConnectGpxImport;

public class GpxImport
{
    public async System.Threading.Tasks.Task Import(string settingsFile)
    {
        if (string.IsNullOrWhiteSpace(settingsFile))
        {
            Log.Error("Settings File is Null or Whitespace?");
            return;
        }

        settingsFile = settingsFile.Trim();

        var settingsFileInfo = new FileInfo(settingsFile);

        if (!settingsFileInfo.Exists)
        {
            Log.Error($"Settings File {settingsFile} Does Not Exist?");
            return;
        }

        GarminConnectGpxImportSettings? settings;
        try
        {
            var settingsFileJsonString = await File.ReadAllTextAsync(settingsFileInfo.FullName);
            var tryReadSettings =
                JsonSerializer.Deserialize<GarminConnectGpxImportSettings>(settingsFileJsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tryReadSettings == null)
            {
                Log.Error("Settings file {settingsFile} deserialized into a null object - is the format correct?",
                    settingsFile);
                return;
            }

            settings = tryReadSettings;

            Log.ForContext("settings",
                settings.Dump(new DumpOptions
                {
                    ExcludeProperties = new List<string>
                        { nameof(settings.ConnectUserName), nameof(settings.ConnectPassword) }
                })).Information($"Using settings from {settingsFileInfo.FullName}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception reading settings file {settingsFile}", settingsFile);
            return;
        }

        if (settings.ImportLines && string.IsNullOrWhiteSpace(settings.PointlessWaymarksSiteSettingsFileFullName))
        {
            Log.Error(
                $"The settings specify {nameof(settings.ImportLines)} but the Pointless Waymarks CMS Site Settings file is empty?");
            return;
        }

        FileInfo? siteSettingsFileInfo = null;

        if (settings.ImportLines)
        {
            siteSettingsFileInfo = new FileInfo(settings.PointlessWaymarksSiteSettingsFileFullName);

            if (!siteSettingsFileInfo.Exists)
            {
                Log.Error(
                    $"The settings specify {nameof(settings.ImportLines)} but the Pointless Waymarks CMS Site Settings file is empty?");
                return;
            }
        }

        var archiveDirectory = new DirectoryInfo(settings.GpxArchiveDirectoryFullName);

        if (!archiveDirectory.Exists)
            try
            {
                archiveDirectory.Create();
            }
            catch (Exception e)
            {
                Log.Error(e, 
                    $"The specified GPX Archive Directory {settings.GpxArchiveDirectoryFullName} does not exist and could not be created.");
                return;
            }

        //9/25/2022 - I haven't done any research or extensive testing but the assumption here is
        //that for large search ranges that it will be better to only query Garmin Connect for a limited
        //number of days...
        var searchEndDate = settings.ImportEndDate.AddDays(1).Date.AddTicks(-1);
        var searchStartDate = searchEndDate.AddDays(-(Math.Abs(settings.ImportPreviousDays) - 1)).Date;

        var searchSegmentLength = 100;

        var searchDateRanges = new List<(DateTime startDate, DateTime endDate)>();

        for (var i = 0; i < settings.ImportPreviousDays / searchSegmentLength; i++)
            searchDateRanges.Add((searchStartDate.AddDays(i * searchSegmentLength),
                searchStartDate.AddDays((i + 1) * searchSegmentLength).AddTicks(-1)));

        if (settings.ImportPreviousDays % searchSegmentLength != 0)
            searchDateRanges.Add((
                settings.ImportEndDate.Date.AddDays(-(settings.ImportPreviousDays % searchSegmentLength) + 1),
                settings.ImportEndDate.AddDays(1).Date.AddTicks(-1)));


        var authParameters = new BasicAuthParameters(settings.ConnectUserName, settings.ConnectPassword);
        var client = new GarminConnectClient(new GarminConnectContext(new HttpClient(), authParameters));

        var gpxFiles = new List<FileInfo>();

        Log.Verbose($"Looping thru {searchDateRanges.Count} Date Range Search Periods");
        int counter = 0;

        foreach (var loopDateSearchRange in searchDateRanges)
        {
            Console.WriteLine();
            Log.Verbose($"Sending Query to Garmin Connect for From {loopDateSearchRange.startDate} to {loopDateSearchRange.endDate} - {++counter} of {searchDateRanges.Count}");

            var activityList = await client.GetActivitiesByDate(loopDateSearchRange.startDate,
                loopDateSearchRange.endDate, string.Empty);

            if (activityList.Length == 0)
            {
                Log.Information(
                    $"No Activities Found From {loopDateSearchRange.startDate} to {loopDateSearchRange.endDate} - Continuing");
                continue;
            }

            Log.Information(
                $"Found {activityList.Length} Activities From {loopDateSearchRange.startDate} to {loopDateSearchRange.endDate} - Downloading and Writing Individual Activities...");

            foreach (var loopActivity in activityList)
            {
                Console.Write(".");
                var name = loopActivity.ActivityName;
                var locationName = loopActivity.LocationName;
                var activityDateString = loopActivity.StartTimeLocal.ToString("yyyy-MM-dd-hh-tt");
                var activityIdString = loopActivity.ActivityId.ToString();
                var nameMaxSafeLength = 240 - activityDateString.Length - activityIdString.Length;
                var activitySafeName = $"{name}-{locationName}".Truncate(nameMaxSafeLength);

                var safeFileName = SlugUtility.Create(false,
                    $"{activityDateString}-{activitySafeName}--gc{activityIdString}", 250);
                var safeGpxFile = new FileInfo(Path.Combine(archiveDirectory.FullName, $"{safeFileName}.gpx"));
                var safeJsonFile = new FileInfo(Path.Combine(archiveDirectory.FullName, $"{safeFileName}.json"));

                if (safeGpxFile.Exists && settings.OverwriteExistingArchiveDirectoryFiles)
                    try
                    {
                        safeGpxFile.Delete();
                        safeGpxFile.Refresh();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine();
                        Log.ForContext("e", e.ToString())
                            .Warning(
                                $"Failed to Delete Existing File {safeGpxFile.FullName} - skipping and continuing...");
                        continue;
                    }

                if (safeJsonFile.Exists && settings.OverwriteExistingArchiveDirectoryFiles)
                    try
                    {
                        safeJsonFile.Delete();
                        safeJsonFile.Refresh();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine();
                        Log.ForContext("e", e.ToString())
                            .Warning(
                                $"Failed to Delete Existing File {safeJsonFile.FullName} - skipping and continuing...");
                        continue;
                    }

                if (safeGpxFile.Exists || safeJsonFile.Exists)
                {
                    Console.WriteLine();
                    Log.Verbose(
                        $"Skipping {safeGpxFile.FullName} and {safeJsonFile.FullName} because of existing files.");
                    continue;
                }

                await File.WriteAllTextAsync(safeJsonFile.FullName,
                    JsonSerializer.Serialize(loopActivity,
                        new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.IgnoreCycles, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
                var file = await client.DownloadActivity(loopActivity.ActivityId, ActivityDownloadFormat.GPX);
                
                if (file == null) continue;
                
                await File.WriteAllBytesAsync(safeGpxFile.FullName, file);
                gpxFiles.Add(safeGpxFile);
            }
        }

        Log.Information(
            $"Downloading and Archiving Connect Activities has Finished - wrote {gpxFiles.Count} activities to {archiveDirectory.FullName}");

        if (!settings.ImportLines)
        {
            Log.Information("Program Ending - no Import Requested");
            return;
        }

        var consoleProgress = new ConsoleProgress();

        UserSettingsUtilities.SettingsFileFullName = siteSettingsFileInfo.FullName;
        var siteSettings = await UserSettingsUtilities.ReadFromCurrentSettingsFile(consoleProgress);
        siteSettings.VerifyOrCreateAllTopLevelFolders();

        await UserSettingsUtilities.EnsureDbIsPresent(consoleProgress);
    }
}