using System.Text.Json;
using System.Text.Json.Serialization;
using Garmin.Connect;
using Garmin.Connect.Auth;
using Garmin.Connect.Models;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Spatial;
using Polly;
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

        if (settings.ImportActivitiesToSite &&
            string.IsNullOrWhiteSpace(settings.PointlessWaymarksSiteSettingsFileFullName))
        {
            Log.Error(
                $"The settings specify {nameof(settings.ImportActivitiesToSite)} but the Pointless Waymarks CMS Site Settings file is empty?");
            return;
        }

        FileInfo? siteSettingsFileInfo = null;

        if (settings.ImportActivitiesToSite)
        {
            siteSettingsFileInfo = new FileInfo(settings.PointlessWaymarksSiteSettingsFileFullName);

            if (!siteSettingsFileInfo.Exists)
            {
                Log.Error(
                    $"The settings specify {nameof(settings.ImportActivitiesToSite)} but the Pointless Waymarks CMS Site Settings file is empty?");
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
        var searchEndDate = settings.DownloadEndDate.AddDays(1).Date.AddTicks(-1);
        var searchStartDate = searchEndDate.AddDays(-(Math.Abs(settings.DownloadDaysBack) - 1)).Date;

        var searchSegmentLength = 100;

        var searchDateRanges = new List<(DateTime startDate, DateTime endDate)>();

        for (var i = 0; i < settings.DownloadDaysBack / searchSegmentLength; i++)
            searchDateRanges.Add((searchStartDate.AddDays(i * searchSegmentLength),
                searchStartDate.AddDays((i + 1) * searchSegmentLength).AddTicks(-1)));

        if (settings.DownloadDaysBack % searchSegmentLength != 0)
            searchDateRanges.Add((
                settings.DownloadEndDate.Date.AddDays(-(settings.DownloadDaysBack % searchSegmentLength) + 1),
                settings.DownloadEndDate.AddDays(1).Date.AddTicks(-1)));

        var authParameters = new BasicAuthParameters(settings.ConnectUserName, settings.ConnectPassword);
        var client = new GarminConnectClient(new GarminConnectContext(new HttpClient(), authParameters));

        var fileList = new List<(FileInfo activityFileInfo, FileInfo? gpxFileInfo)>();

        Log.Verbose($"Looping thru {searchDateRanges.Count} Date Range Search Periods");
        var counter = 0;

        foreach (var loopDateSearchRange in searchDateRanges)
        {
            Console.WriteLine();
            Log.Verbose(
                $"Sending Query to Garmin Connect for From {loopDateSearchRange.startDate} to {loopDateSearchRange.endDate} - {++counter} of {searchDateRanges.Count}");

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
                        new JsonSerializerOptions
                        {
                            WriteIndented = true, ReferenceHandler = ReferenceHandler.IgnoreCycles,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        }));
                safeJsonFile.Refresh();

                byte[]? file = null;

                try
                {
                    file = await Policy.Handle<Exception>().WaitAndRetryAsync(3, i => TimeSpan.FromMinutes(1 * i))
                        .ExecuteAsync(async () =>
                            await client.DownloadActivity(loopActivity.ActivityId, ActivityDownloadFormat.GPX));
                }
                catch (Exception e)
                {
                    Log.Error(e,
                        $"File Download Failed - ActivityId {loopActivity.ActivityId} - Activity File {safeJsonFile.FullName}");
                }

                if (file == null)
                {
                    fileList.Add((safeJsonFile, null));
                }
                else
                {
                    await File.WriteAllBytesAsync(safeGpxFile.FullName, file);
                    safeGpxFile.Refresh();
                    fileList.Add((safeJsonFile, safeGpxFile));
                }
            }
        }

        Log.Information(
            $"Downloading and Archiving Connect Activities has Finished - wrote {fileList.Count} activities to {archiveDirectory.FullName}");

        if (!settings.ImportActivitiesToSite)
        {
            Log.Information("Program Ending - no Import Requested");
            return;
        }

        if (!fileList.Any())
        {
            Log.Information("Program Ending - no files to Import");
            return;
        }

        var consoleProgress = new ConsoleProgress();

        UserSettingsUtilities.SettingsFileFullName = siteSettingsFileInfo!.FullName;
        var siteSettings = await UserSettingsUtilities.ReadFromCurrentSettingsFile(consoleProgress);
        siteSettings.VerifyOrCreateAllTopLevelFolders();

        await UserSettingsUtilities.EnsureDbIsPresent(consoleProgress);

        Log.Information($"Starting import of {fileList.Count} GPX Files");

        foreach (var loopFile in fileList.Where(x => x.gpxFileInfo != null))
        {
            var gpxFile = GpxFile.Parse(await File.ReadAllTextAsync(loopFile.gpxFileInfo!.FullName),
                new GpxReaderSettings
                {
                    BuildWebLinksForVeryLongUriValues = true,
                    IgnoreBadDateTime = true,
                    IgnoreUnexpectedChildrenOfTopLevelElement = true,
                    IgnoreVersionAttribute = true
                });

            if (!gpxFile.Tracks.Any(t => t.Segments.SelectMany(y => y.Waypoints).Count() > 1)) continue;

            var tracksList = (await SpatialHelpers.TracksFromGpxFile(loopFile.gpxFileInfo, consoleProgress))
                .Where(x => x.Track.Count > 0).ToList();

            var innerLoopCounter = 0;

            var errorList = new List<string>();

            foreach (var loopTracks in tracksList)
            {
                innerLoopCounter++;

                var newEntry = await LineGenerator.NewFromGpxTrack(loopTracks, false, consoleProgress);

                var tagList = Db.TagListParseToSlugs(newEntry.Tags, false);
                tagList.Add("garmin connect import");
                newEntry.Tags = Db.TagListJoinAsSlugs(tagList, false);
                newEntry.ShowInMainSiteFeed = settings.ShowInMainSiteFeed;

                var validation =
                    await CommonContentValidation.ValidateSlugLocalAndDb(newEntry.Slug, newEntry.ContentId);
                var renameCount = 1;
                var baseTitle = newEntry.Title;

                while (!validation.Valid && renameCount < 101)
                {
                    renameCount++;
                    newEntry.Title = $"{baseTitle} - {renameCount}";
                    newEntry.Slug = SlugUtility.Create(true, newEntry.Title);
                    validation =
                        await CommonContentValidation.ValidateSlugLocalAndDb(newEntry.Slug, newEntry.ContentId);
                }

                if (!string.IsNullOrEmpty(settings.IntersectionTagSettings))
                {
                    var featureToCheck = newEntry.FeatureFromGeoJsonLine();

                    if (featureToCheck != null)
                    {
                        var tagger = new FeatureIntersectionTags.Intersection();
                        var taggerResult = tagger.FindTagsFromIntersections(settings.IntersectionTagSettings, featureToCheck.AsList());

                        if (taggerResult.Any() && taggerResult.First().Tags.Any())
                        {
                            var tagListForIntersection = Db.TagListParseToSlugs(newEntry.Tags, false);
                            tagListForIntersection.AddRange(taggerResult.First().Tags);
                            newEntry.Tags = Db.TagListJoinAsSlugs(tagListForIntersection, false);
                        }
                    }
                }

                var (saveGenerationReturn, _) =
                    await LineGenerator.SaveAndGenerateHtml(newEntry, DateTime.Now,
                        consoleProgress);

                if (saveGenerationReturn.HasError)
                    //TODO: Need alerting on this that would actually be seen...
                {
                    Log.Error(
                        $"Save Failed! GPX: {loopFile.gpxFileInfo.FullName}, Activity: {loopFile.activityFileInfo.FullName}");
                    errorList.Add(
                        $"Save Failed! GPX: {loopFile.gpxFileInfo.FullName}, Activity: {loopFile.activityFileInfo.FullName}");
                    continue;
                }

                Log.Verbose(
                    $"New Line - {loopFile.gpxFileInfo.FullName} - Track {innerLoopCounter} of {tracksList.Count}");
            }

            if (errorList.Any())
            {
                Console.WriteLine("Save Errors:");
                errorList.ForEach(Console.WriteLine);
            }

            Log.Information("Garmin Connect Gpx Import Finished");
        }
    }
}