using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.GarminConnect;
using PointlessWaymarks.SpatialTools;
using Polly;
using Serilog;

namespace PointlessWaymarks.Task.GarminConnectGpxImport;

public class GpxTrackImport
{
    public async System.Threading.Tasks.Task Import(string settingsFile)
    {
        var notifier = (await WindowsNotificationBuilders.NewNotifier(GarminConnectGpxImportSettings.ProgramShortName))
            .SetErrorReportAdditionalInformationMarkdown(FileAndFolderTools.ReadAllText(Path.Combine(
                AppContext.BaseDirectory, "README_Task-GarminConnectGpxImport.md"))).SetAutomationLogoNotificationIconUrl();

        var consoleProgress = new ConsoleProgress();

        if (string.IsNullOrWhiteSpace(settingsFile))
        {
            Log.Error("Blank settings file is not valid...");
            await notifier.Error("Blank Settings File Name.",
                "The program should be run with the Settings File as the argument.");
            return;
        }

        settingsFile = settingsFile.Trim();

        var settingsFileInfo = new FileInfo(settingsFile);

        if (!settingsFileInfo.Exists)
        {
            Log.Error("Could not find settings file: {settingsFile}", settingsFile);
            await notifier.Error($"Could not find settings file: {settingsFile}");
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
                await notifier.Error($"Error: Settings file {settingsFile} deserialized into a null object.",
                    "The program found and was able to read the Settings File - {settingsFile} - but nothing was returned when converting the file into program settings - this probably indicates a format problem with the settings file.");
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
            await notifier.Error(e);
            return;
        }

        if (settings.ImportActivitiesToSite &&
            string.IsNullOrWhiteSpace(settings.PointlessWaymarksSiteSettingsFileFullName))
        {
            Log.Error(
                $"The settings specify {nameof(settings.ImportActivitiesToSite)} but the Pointless Waymarks CMS Site Settings file is empty?");
            return;
        }

        var validationContext = new ValidationContext(settings, null, null);
        var simpleValidationResults = new List<ValidationResult>();
        var simpleValidationPassed = Validator.TryValidateObject(
            settings, validationContext, simpleValidationResults,
            true
        );

        if (!simpleValidationPassed)
        {
            Log.ForContext("SimpleValidationErrors", simpleValidationResults.SafeObjectDump())
                .Error("Validating data from {settingsFile} failed.", settingsFile);
            simpleValidationResults.ForEach(Console.WriteLine);
            await notifier.Error($"Validating data from {settingsFile} failed.",
                simpleValidationResults.SafeObjectDump());

            return;
        }

        Log.ForContext("settings",
                settings.Dump(new DumpOptions
                    { ExcludeProperties = new List<string> { nameof(settings.ConnectPassword) } }))
            .Information("Settings Passed Basic Validation - Settings File {settingsFile}", settingsFile);


        FileInfo? siteSettingsFileInfo = null;

        if (settings.ImportActivitiesToSite)
        {
            siteSettingsFileInfo = new FileInfo(settings.PointlessWaymarksSiteSettingsFileFullName);

            if (!siteSettingsFileInfo.Exists)
            {
                Log.Error(
                    "The site settings file {settingsPointlessWaymarksSiteSettingsFileFullName} was specified but not found?",
                    settings.PointlessWaymarksSiteSettingsFileFullName);
                await notifier.Error(
                    $"Site settings file {settings.PointlessWaymarksSiteSettingsFileFullName} was not found");
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
                    "The specified Gpx Archive Directory {settingsGpxArchiveDirectoryFullName} does not exist and could not be created.",
                    settings.GpxArchiveDirectoryFullName);
                await notifier.Error(e,
                    "The specified Photo Archive Directory {settings.PhotoPickupArchiveDirectory} does not exist and could not be created. In addition to checking that the directory exists and there are no typos you may also need to check that the program has permissions to access, read from and write to the directory.");
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

        string username;
        string password;

        if (string.IsNullOrWhiteSpace(settings.LoginCode))
        {
            Log.Verbose("Using User Name and Password from Settings File");
            username = settings.ConnectUserName;
            password = settings.ConnectPassword;
        }
        else
        {
            Log.Verbose($"Using Login Code {settings.LoginCode} and Password Vault");
            var credentials =
                PasswordVaultTools.GetCredentials(
                    GarminConnectGpxImportSettings.PasswordVaultResourceIdentifier(settings.LoginCode));
            username = credentials.username;
            password = credentials.password;
        }

        var client = new GarminConnectClient(new GarminConnectContext(new HttpClient(), username, password));

        var fileList = new List<(FileInfo activityFileInfo, FileInfo? gpxFileInfo)>();

        Log.Verbose($"Looping thru {searchDateRanges.Count} Date Range Search Periods");
        var counter = 0;

        foreach (var loopDateSearchRange in searchDateRanges)
        {
            Console.WriteLine();
            Log.Verbose(
                $"Sending Query to Garmin Connect for From {loopDateSearchRange.startDate} to {loopDateSearchRange.endDate} - {++counter} of {searchDateRanges.Count}");

            var garminRetryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, i => new TimeSpan(0, 0, 0, 10 * i));
            var activityList = await garminRetryPolicy.ExecuteAsync(async () => await client.GetActivitiesByDate(
                loopDateSearchRange.startDate,
                loopDateSearchRange.endDate, string.Empty));

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
                        true, settings.OverwriteExistingArchiveDirectoryFiles,
                        new ConnectGpxService { ConnectUsername = username, ConnectPassword = password }, consoleProgress);

                    fileList.Add((jsonArchiveFile, gpxFile));
                }
                catch (Exception e)
                {
                    Log.Error(e,
                        "Error with the GPX for {activityId}, {activityName} - skipping and continuing...",
                        loopActivity.ActivityId, loopActivity.ActivityName);
                    await notifier.Error(e,
                        $"There was download error for the GPX for {loopActivity.ActivityId}, {loopActivity.ActivityName} - the program skipped this file and continued. This could be due to a transient network error, an unexpected problem with the file or ... If you want to download, archive or import this file you should do it manually.");
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

                var newEntry = await LineGenerator.NewFromGpxTrack(loopTracks, false, false, true, consoleProgress);

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
                    newEntry.Slug = SlugTools.CreateSlug(true, newEntry.Title);
                    validation =
                        await CommonContentValidation.ValidateSlugLocalAndDb(newEntry.Slug, newEntry.ContentId);
                }

                if (!string.IsNullOrEmpty(settings.IntersectionTagSettings))
                {
                    var featureToCheck = newEntry.FeatureFromGeoJsonLine();

                    if (featureToCheck != null)
                    {
                        var tagResult = featureToCheck.IntersectionTags(settings.IntersectionTagSettings,
                            CancellationToken.None,
                            new ConsoleProgress());

                        if (tagResult.Any())
                        {
                            var tagListForIntersection = Db.TagListParse(newEntry.Tags);
                            tagListForIntersection.AddRange(tagResult);
                            newEntry.Tags = Db.TagListJoin(tagListForIntersection);
                        }
                    }
                }

                var (saveGenerationReturn, lineContent) =
                    await LineGenerator.SaveAndGenerateHtml(newEntry, DateTime.Now,
                        consoleProgress);

                if (saveGenerationReturn.HasError)
                {
                    Log.Error(
                        "Save Failed! GPX: {gpxFileFullName}, Activity: {activityFileFullName}",
                        loopFile.gpxFileInfo.FullName, loopFile.activityFileInfo.FullName);
                    errorList.Add(
                        $"Save Failed! GPX: {loopFile.gpxFileInfo.FullName}, Activity: {loopFile.activityFileInfo.FullName}");
                    continue;
                }


                Log.Verbose(
                    $"New Line - {loopFile.gpxFileInfo.FullName} - Track {innerLoopCounter} of {tracksList.Count}");


                if (lineContent?.MainPicture != null)
                {
                    var mainPhotoInformation =
                        PictureAssetProcessing.ProcessPhotoDirectory(lineContent.MainPicture.Value);

                    Debug.Assert(mainPhotoInformation != null, nameof(mainPhotoInformation) + " != null");

                    var closestSize = mainPhotoInformation.SrcsetImages.MinBy(x => Math.Abs(384 - x.Width));

                    if (closestSize is { File: not null, SiteUrl: not null })
                        notifier.Message(
                            $"{UserSettingsSingleton.CurrentSettings().SiteName} - Line Added: '{lineContent.Title}'",
                            closestSize.SiteUrl);
                    else
                        notifier.Message(
                            $"{UserSettingsSingleton.CurrentSettings().SiteName} - Line Added: '{lineContent.Title}'");
                }
                else
                {
                    notifier.Message(
                        $"{UserSettingsSingleton.CurrentSettings().SiteName} - Added Line: '{lineContent?.Title ?? "No Title?"}'");
                }
            }

            if (errorList.Any())
            {
                Console.WriteLine("Save Errors:");
                errorList.ForEach(Console.WriteLine);

                await notifier.Error("Garmin Connect Import Errors. Click for details.",
                    string.Join(Environment.NewLine, errorList));
            }


            Log.Information("Garmin Connect Gpx Import Finished");
        }
    }
}