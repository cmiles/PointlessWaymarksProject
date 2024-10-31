using System.Diagnostics;
using Garmin.Connect;
using Garmin.Connect.Auth;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WindowsTools;
using Polly;
using Serilog;

namespace PointlessWaymarks.CmsTask.GarminConnectGpxImport;

public class GpxTrackImport
{
    public async Task Import(GarminConnectGpxImportSettings settings)
    {
        var notifier =
            (await WindowsNotificationBuilders.NewNotifier(GarminConnectGpxImportSettings.ProgramShortName()))
            .SetErrorReportAdditionalInformationMarkdown(EmbeddedResourceTools.GetEmbeddedResourceText("README.md"))
            .SetAutomationLogoNotificationIconUrl();

        var consoleProgress = new ConsoleProgress();

        Log.ForContext("settings",
                settings.Dump(new DumpOptions
                {
                    ExcludeProperties = new List<string>
                        { nameof(settings.ConnectPassword), nameof(settings.ConnectUserName) }
                }))
            .Information("Starting with Settings - {daysBack} Days Back", settings.DownloadDaysBack);


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
        var searchEndDate = DateTime.Now.Date.AddTicks(-1);
        var searchStartDate = searchEndDate.AddDays(-(Math.Abs(settings.DownloadDaysBack) - 1)).Date;

        var searchSegmentLength = 100;

        var searchDateRanges = new List<(DateTime startDate, DateTime endDate)>();

        for (var i = 0; i < settings.DownloadDaysBack / searchSegmentLength; i++)
            searchDateRanges.Add((searchStartDate.AddDays(i * searchSegmentLength),
                searchStartDate.AddDays((i + 1) * searchSegmentLength).AddTicks(-1)));

        if (settings.DownloadDaysBack % searchSegmentLength != 0)
            searchDateRanges.Add((
                searchEndDate.Date.AddDays(-(settings.DownloadDaysBack % searchSegmentLength) + 1),
                searchEndDate.AddDays(1).Date.AddTicks(-1)));

        var client = new GarminConnectClient(new GarminConnectContext(new HttpClient(),
            new BasicAuthParameters(settings.ConnectUserName, settings.ConnectPassword)));

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
                        new ConnectGpxService
                            { ConnectUsername = settings.ConnectUserName, ConnectPassword = settings.ConnectPassword },
                        consoleProgress, CancellationToken.None);

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

                if (siteSettings.FeatureIntersectionTagOnImport &&
                    !string.IsNullOrWhiteSpace(siteSettings.FeatureIntersectionTagSettingsFile))
                {
                    var featureToCheck = newEntry.FeatureFromGeoJsonLine();

                    if (featureToCheck != null)
                    {
                        var tagResult = featureToCheck.IntersectionTags(siteSettings.FeatureIntersectionTagSettingsFile,
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