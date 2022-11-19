using System.Text.Json;
using Garmin.Connect;
using Garmin.Connect.Auth;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.LoggingTools;
using PointlessWaymarks.SpatialTools;
using Serilog;

namespace PointlessWaymarks.Task.GarminConnectGpxImport;

public class GpxTrackImport
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

                FileInfo jsonArchiveFile;
                try
                {
                    jsonArchiveFile = await GarminConnectTools.WriteJsonActivityArchiveFile(loopActivity,
                        archiveDirectory, settings.OverwriteExistingArchiveDirectoryFiles);
                }
                catch (Exception e)
                {
                    Log.Error(e,
                        $"Error Writing Json Activity file for Activity Id {loopActivity.ActivityId}, {loopActivity.ActivityName} - skipping and continuing...");
                    continue;
                }

                try
                {
                    var gpxFile = await GarminConnectTools.GetGpx(loopActivity, archiveDirectory,
                        settings.OverwriteExistingArchiveDirectoryFiles, settings.ConnectUserName,
                        settings.ConnectPassword);

                    fileList.Add((jsonArchiveFile, gpxFile));
                }
                catch (Exception e)
                {
                    Log.Error(e,
                        $"Error with the GPX for {loopActivity.ActivityId}, {loopActivity.ActivityName} - skipping and continuing...");
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

            var tracksList = (await GpxTools.TracksFromGpxFile(loopFile.gpxFileInfo, consoleProgress))
                .Where(x => x.Track.Count > 0).ToList();

            var innerLoopCounter = 0;

            var errorList = new List<string>();

            foreach (var loopTracks in tracksList)
            {
                innerLoopCounter++;

                var newEntry = await LineGenerator.NewFromGpxTrack(loopTracks, false, false, consoleProgress);

                var tagList = Db.TagListParse(newEntry.Tags);
                tagList.Add("garmin connect import");
                newEntry.Tags = Db.TagListJoin(tagList);
                newEntry.ShowInMainSiteFeed = settings.ShowInMainSiteFeed;

                var validation =
                    await CommonContentValidation.ValidateSlugLocalAndDb(newEntry.Slug, newEntry.ContentId);
                var renameCount = 1;
                var baseTitle = newEntry.Title;

                while (!validation.Valid && renameCount < 101)
                {
                    renameCount++;
                    newEntry.Title = $"{baseTitle} - {renameCount}";
                    newEntry.Slug = SlugTools.Create(true, newEntry.Title);
                    validation =
                        await CommonContentValidation.ValidateSlugLocalAndDb(newEntry.Slug, newEntry.ContentId);
                }

                if (!string.IsNullOrEmpty(settings.IntersectionTagSettings))
                {
                    var featureToCheck = newEntry.FeatureFromGeoJsonLine();

                    if (featureToCheck != null)
                    {
                        var tagger = new Intersection();
                        var taggerResult = tagger.Tags(settings.IntersectionTagSettings, featureToCheck.AsList(),
                            CancellationToken.None, new ConsoleProgress());

                        if (taggerResult.Any() && taggerResult.First().Tags.Any())
                        {
                            var tagListForIntersection = Db.TagListParse(newEntry.Tags);
                            tagListForIntersection.AddRange(taggerResult.First().Tags);
                            newEntry.Tags = Db.TagListJoin(tagListForIntersection);
                        }
                    }
                }

                if (newEntry.RecordingStartedOnUtc.HasValue && newEntry.RecordingEndedOnUtc.HasValue)
                {
                    var db = await Db.Context();
                    var relatedPhotos = db.PhotoContents.Where(x =>
                        x.PhotoCreatedOnUtc != null && x.PhotoCreatedOnUtc >= newEntry.RecordingStartedOnUtc &&
                        x.PhotoCreatedOnUtc <= newEntry.RecordingEndedOnUtc).ToList();

                    if (relatedPhotos.Any())
                    {
                        var photoBodyAddition = string.Join(Environment.NewLine,
                            relatedPhotos.Select(x => $"{Environment.NewLine}{BracketCodePhotos.Create(x)}"));

                        newEntry.BodyContent =
                            $"{(string.IsNullOrWhiteSpace(newEntry.BodyContent) ? "" : Environment.NewLine)}{photoBodyAddition}";
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