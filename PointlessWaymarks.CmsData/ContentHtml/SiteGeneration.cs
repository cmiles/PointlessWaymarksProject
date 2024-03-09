using System.Text.Json;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.Reports;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.ContentGalleryHtml;
using PointlessWaymarks.CmsData.ContentHtml.ErrorHtml;
using PointlessWaymarks.CmsData.ContentHtml.IndexHtml;
using PointlessWaymarks.CmsData.ContentHtml.PhotoGalleryHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentHtml;

public static class SiteGeneration
{
    public static async Task<List<GenerationReturn>> AllSiteContent(IProgress<string>? progress = null)
    {
        await CleanupGenerationInformation(progress).ConfigureAwait(false);

        var generationVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime();

        await FileManagement.WriteSiteResourcesToGeneratedSite(progress).ConfigureAwait(false);

        await RelatedContentReference.GenerateRelatedContentDbTable(generationVersion, progress)
            .ConfigureAwait(false);
        await SetupTagGenerationDbData(generationVersion, progress).ConfigureAwait(false);
        await SetupDailyPhotoGenerationDbData(generationVersion, progress).ConfigureAwait(false);

        await SiteGenerationAllContent.GenerateAllPhotoHtml(generationVersion, progress).ConfigureAwait(false);
        await SiteGenerationAllContent.GenerateAllImageHtml(generationVersion, progress).ConfigureAwait(false);

        //The All Map generation also regenerates the Point Data Json - to avoid conflicts run points before the group...
        await SiteGenerationAllContent.GenerateAllPointHtml(generationVersion, progress);
        await MapIconGenerator.GenerateMapIconsFile().ConfigureAwait(false);

        var generationTasks = new List<Task>
        {
            SiteGenerationAllContent.GenerateAllFileHtml(generationVersion, progress),
            SiteGenerationAllContent.GenerateAllVideoHtml(generationVersion, progress),
            SiteGenerationAllContent.GenerateAllMapData(generationVersion, progress),
            SiteGenerationAllContent.GenerateAllNoteHtml(generationVersion, progress),
            SiteGenerationAllContent.GenerateAllPostHtml(generationVersion, progress),
            SiteGenerationAllContent.GenerateAllLineHtml(generationVersion, progress),
            SiteGenerationAllContent.GenerateAllGeoJsonHtml(generationVersion, progress),
            SiteGenerationAllContent.GenerateAllDailyPhotoGalleriesHtml(generationVersion, progress),
            GenerateCameraRollHtml(generationVersion, progress)
        };

        await Task.WhenAll(generationTasks).ConfigureAwait(false);

        var taskSet = new List<Func<Task>>
        {
            async () => await SiteGenerationAllContent.GenerateAllTagHtml(generationVersion, progress)
                .ConfigureAwait(false),
            async () => await SiteGenerationAllContent.GenerateAllListHtml(generationVersion, progress)
                .ConfigureAwait(false),
            async () => await SiteGenerationAllContent.GenerateAllUtilityJson(progress).ConfigureAwait(false),
            async () => await GenerateIndex(generationVersion, progress).ConfigureAwait(false),
            async () => await GenerateLatestContentGalleryHtml(generationVersion, progress),
            async () => await GenerateErrorPage(generationVersion, progress).ConfigureAwait(false)
        };

        await Parallel.ForEachAsync(taskSet, async (x, _) => await x()).ConfigureAwait(false);

        progress?.Report(
            $"Generation Complete - Writing Generation Date Time of UTC {generationVersion} in Db Generation log as Last Generation");

        await Db.SaveGenerationLogAndRecordSettings(generationVersion).ConfigureAwait(false);

        return await CommonContentValidation.CheckAllContentForBadContentReferences(progress).ConfigureAwait(false);
    }


    public static async Task<List<GenerationReturn>> ChangedSiteContent(IProgress<string>? progress = null)
    {
        await CleanupGenerationInformation(progress).ConfigureAwait(false);

        var db = await Db.Context().ConfigureAwait(false);

        //Get and check the last generation - if there is no value then generate all which should create a valid value for the next
        //run
        var generationVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime();
        var lastGenerationValues = db.GenerationLogs.Where(x => x.GenerationVersion < generationVersion)
            .OrderByDescending(x => x.GenerationVersion).FirstOrDefault();

        if (lastGenerationValues == null || string.IsNullOrWhiteSpace(lastGenerationValues.GenerationSettings))
        {
            progress?.Report("No value for Last Generation in Settings - Generating All HTML");

            return await AllSiteContent(progress).ConfigureAwait(false);
        }

        progress?.Report($"Last Generation - {lastGenerationValues.GenerationVersion}");
        var lastGenerationDateTime = lastGenerationValues.GenerationVersion;

        //The menu is currently written to all pages - if there are changes then generate all
        var menuUpdates = await db.MenuLinks.AnyAsync(x => x.ContentVersion > lastGenerationDateTime)
            .ConfigureAwait(false);

        if (menuUpdates)
        {
            progress?.Report("Menu Updates detected - menu updates impact all pages, generating All HTML");

            return await AllSiteContent(progress).ConfigureAwait(false);
        }

        //If the generation settings have changed trigger a full rebuild
        var lastGenerationSettings =
            JsonSerializer.Deserialize<UserSettingsGenerationValues>(lastGenerationValues.GenerationSettings);

        var currentGenerationSettings = UserSettingsSingleton.CurrentSettings().GenerationValues();

        var compareLogic = new CompareLogic(new ComparisonConfig { MaxDifferences = 20 });
        var generationSettingsComparison = compareLogic.Compare(lastGenerationSettings, currentGenerationSettings);

        var compareReport = new UserFriendlyReport();
        var generationSettingsComparisonDifferences =
            compareReport.OutputString(generationSettingsComparison.Differences);

        if (!generationSettingsComparison.AreEqual)
        {
            progress?.Report(
                $"Generation Settings Changes detected - generating All HTML: {Environment.NewLine}{generationSettingsComparisonDifferences}");

            return await AllSiteContent(progress).ConfigureAwait(false);
        }

        progress?.Report("Write Site Resources");
        await FileManagement.WriteChangedSiteResourcesToGeneratedSite(progress).ConfigureAwait(false);

        progress?.Report($"Generation HTML based on changes after UTC - {lastGenerationValues.GenerationVersion}");

        await RelatedContentReference.GenerateRelatedContentDbTable(generationVersion, progress)
            .ConfigureAwait(false);
        await SiteGenerationChangedContent
            .GenerateChangedContentIdReferences(lastGenerationDateTime, generationVersion, progress)
            .ConfigureAwait(false);
        await SetupTagGenerationDbData(generationVersion, progress).ConfigureAwait(false);
        await SiteGenerationChangedContent.GenerateChangedContentIdReferencesFromTagExclusionChanges(generationVersion,
            lastGenerationDateTime,
            progress).ConfigureAwait(false);

        await SetupDailyPhotoGenerationDbData(generationVersion, progress).ConfigureAwait(false);

        if (!await db.GenerationChangedContentIds.AnyAsync().ConfigureAwait(false))
            progress?.Report("No Changes Detected - ending HTML generation.");

        await SiteGenerationChangedContent.GenerateChangeFilteredPhotoHtml(generationVersion, progress)
            .ConfigureAwait(false);
        await SiteGenerationChangedContent.GenerateChangeFilteredImageHtml(generationVersion, progress)
            .ConfigureAwait(false);

        //Both Maps and Points update the Point Json file - update the Points outside the task list to avoid
        //both processing trying to write to the file.
        await SiteGenerationChangedContent.GenerateChangeFilteredPointHtml(generationVersion, progress);

        if (db.MapIcons.Any(x => x.ContentVersion >= lastGenerationDateTime))
            await MapIconGenerator.GenerateMapIconsFile().ConfigureAwait(false);

        var changedPartsList = new List<Task>
        {
            SiteGenerationChangedContent.GenerateChangeFilteredFileHtml(generationVersion, progress),
            SiteGenerationChangedContent.GenerateChangeFilteredVideoHtml(generationVersion, progress),
            SiteGenerationChangedContent.GenerateChangeFilteredGeoJsonHtml(generationVersion, progress),
            SiteGenerationChangedContent.GenerateChangeFilteredLineHtml(generationVersion, progress),
            SiteGenerationChangedContent.GenerateChangeFilteredMapData(generationVersion, progress),
            SiteGenerationChangedContent.GenerateChangeFilteredNoteHtml(generationVersion, progress),
            SiteGenerationChangedContent.GenerateChangeFilteredPostHtml(generationVersion, progress)
        };

        await Task.WhenAll(changedPartsList).ConfigureAwait(false);

        await SiteGenerationChangedContent.GenerateChangedMainFeedContent(generationVersion, progress)
            .ConfigureAwait(false);

        var hasDirectPhotoChanges = db.PhotoContents.Join(db.GenerationChangedContentIds, o => o.ContentId,
            i => i.ContentId, (o, i) => o.PhotoCreatedOn).Any();
        var hasRelatedPhotoChanges = db.PhotoContents.Join(db.GenerationRelatedContents, o => o.ContentId,
            i => i.ContentTwo, (o, i) => o.PhotoCreatedOn).Any();
        var hasDeletedPhotoChanges =
            (await Db.DeletedPhotoContent().ConfigureAwait(false)).Any(x =>
                x.ContentVersion > lastGenerationDateTime);

        if (hasDirectPhotoChanges || hasRelatedPhotoChanges || hasDeletedPhotoChanges)
            await SiteGenerationChangedContent.GenerateChangedDailyPhotoGalleries(generationVersion, progress)
                .ConfigureAwait(false);
        else
            progress?.Report(
                "No changes to Photos directly or thru related content - skipping Daily Photo Page generation.");

        if (hasDirectPhotoChanges || hasDeletedPhotoChanges)
            await GenerateCameraRollHtml(generationVersion, progress).ConfigureAwait(false);
        else progress?.Report("No changes to Photo content - skipping Photo Gallery generation.");

        var tagAndListTasks = new List<Task>
        {
            SiteGenerationChangedContent.GenerateChangedTagHtml(generationVersion, progress),
            SiteGenerationChangedContent.GenerateChangedListHtml(lastGenerationDateTime, generationVersion, progress),
            SiteGenerationAllContent.GenerateAllUtilityJson(progress),
            GenerateIndex(generationVersion, progress),
            GenerateLatestContentGalleryHtml(generationVersion, progress),
            GenerateErrorPage(generationVersion, progress)
        };

        await Task.WhenAll(tagAndListTasks).ConfigureAwait(false);

        progress?.Report(
            $"Generation Complete - writing {generationVersion} as Last Generation UTC into db Generation Log");

        await Db.SaveGenerationLogAndRecordSettings(generationVersion).ConfigureAwait(false);

        var allChangedContentCommon =
            (await db.ContentCommonShellFromContentIds(await db.GenerationChangedContentIds.Select(x => x.ContentId)
                .ToListAsync().ConfigureAwait(false)).ConfigureAwait(false)).Cast<IContentCommon>().ToList();

        return await CommonContentValidation.CheckForBadContentReferences(allChangedContentCommon, db, progress)
            .ConfigureAwait(false);
    }

    public static async Task CleanupGenerationInformation(IProgress<string>? progress = null)
    {
        progress?.Report("Cleaning up Generation Log Information");

        var db = await Db.Context().ConfigureAwait(false);

        var generationLogs = await db.GenerationLogs.OrderByDescending(x => x.GenerationVersion).ToListAsync()
            .ConfigureAwait(false);
        var generationLogsToKeep = generationLogs.Take(30).ToList();
        var generationLogsToDelete = generationLogs.Skip(30).ToList();

        progress?.Report(
            $"Keeping Top {generationLogsToKeep.Count} Logs, Found {generationLogsToDelete.Count} logs to remove");

        //TODO Integrate into DataNotifications
        db.GenerationLogs.RemoveRange(generationLogsToDelete);

        await db.SaveChangesAsync().ConfigureAwait(false);


        //Current Generation Versions for Reference

        var currentGenerationVersions =
            await db.GenerationLogs.Select(x => x.GenerationVersion).ToListAsync().ConfigureAwait(false);


        //Photo Log Cleanup

        var olderPhotoGenerationInformationToRemove = await db.GenerationDailyPhotoLogs
            .Where(x => !currentGenerationVersions.Contains(x.GenerationVersion)).ToListAsync()
            .ConfigureAwait(false);

        progress?.Report($"Found {olderPhotoGenerationInformationToRemove.Count} photo generation logs to remove.");

        db.GenerationDailyPhotoLogs.RemoveRange(olderPhotoGenerationInformationToRemove);

        await db.SaveChangesAsync().ConfigureAwait(false);


        //File Script Cleanup

        var olderScriptGenerations = await db.GenerationFileTransferScriptLogs
            .OrderByDescending(x => x.WrittenOnVersion).Skip(30).ToListAsync().ConfigureAwait(false);

        progress?.Report($"Found {olderScriptGenerations.Count} logs to remove");

        if (olderScriptGenerations.Any())
        {
            db.GenerationFileTransferScriptLogs.RemoveRange(olderScriptGenerations);

            await db.SaveChangesAsync().ConfigureAwait(false);

            DataNotifications.PublishDataNotification("SiteGeneration.CleanupGenerationInformation",
                DataNotificationContentType.FileTransferScriptLog, DataNotificationUpdateType.Delete,
                new List<Guid>());
        }


        //File Write Logs

        DateTime? oldestDateTimeLog = null;

        if (currentGenerationVersions.Any()) oldestDateTimeLog = currentGenerationVersions.Min();

        if (db.GenerationFileWriteLogs.Any())
        {
            var oldestGenerationLog = db.GenerationFileWriteLogs.Min(x => x.WrittenOnVersion);
            if (oldestDateTimeLog == null || oldestGenerationLog < oldestDateTimeLog)
                oldestDateTimeLog = oldestGenerationLog;
        }

        if (oldestDateTimeLog != null)
        {
            var toRemove = await db.GenerationFileWriteLogs.Where(x => x.WrittenOnVersion < oldestDateTimeLog.Value)
                .ToListAsync().ConfigureAwait(false);

            progress?.Report($"Found {toRemove.Count} File Write Logs to remove");

            db.GenerationFileWriteLogs.RemoveRange(toRemove);

            await db.SaveChangesAsync().ConfigureAwait(false);
        }
        else
        {
            progress?.Report("Found zero File Write Logs to remove");
        }


        //Related Contents
        var relatedToRemove = await db.GenerationRelatedContents
            .Where(x => !currentGenerationVersions.Contains(x.GenerationVersion)).ToListAsync()
            .ConfigureAwait(false);

        progress?.Report($"Found {relatedToRemove.Count} Related Content Entries to Remove");

        await db.SaveChangesAsync().ConfigureAwait(false);


        //Tag Logs
        var olderTagGenerationInformationToRemove = await db.GenerationTagLogs
            .Where(x => !currentGenerationVersions.Contains(x.GenerationVersion)).ToListAsync()
            .ConfigureAwait(false);

        progress?.Report($"Found {olderTagGenerationInformationToRemove.Count} tag logs to remove.");

        db.GenerationTagLogs.RemoveRange(olderTagGenerationInformationToRemove);

        await db.SaveChangesAsync(true).ConfigureAwait(false);

        progress?.Report("Done with Generation Clean Up");
    }

    public static async Task GenerateCameraRollHtml(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var cameraRollPage = await CameraRollGalleryPageGenerator.CameraRoll(generationVersion, progress)
            .ConfigureAwait(false);
        await cameraRollPage.WriteLocalHtml().ConfigureAwait(false);
    }


    public static async Task GenerateErrorPage(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var error = new ErrorPage { GenerationVersion = generationVersion };
        await error.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task GenerateIndex(DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var index = new IndexPage { GenerationVersion = generationVersion };
        await index.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task GenerateLatestContentGalleryHtml(DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        var cameraRollPage = await ContentGalleryPageGenerators.LatestContentGallery(generationVersion, progress)
            .ConfigureAwait(false);
        await cameraRollPage.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task SetupDailyPhotoGenerationDbData(DateTime currentGenerationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        progress?.Report("Getting list of all Photo Dates and Content");

        var allPhotoInfo = await db.PhotoContents.Where(x => !x.IsDraft).AsNoTracking().ToListAsync()
            .ConfigureAwait(false);

        var datesAndContent = allPhotoInfo.GroupBy(x => x.PhotoCreatedOn.Date)
            .Select(x => new { date = x.Key, contentIds = x.Select(y => y.ContentId) })
            .OrderByDescending(x => x.date).ToList();

        progress?.Report("Processing Photo Dates and Content");

        foreach (var loopDates in datesAndContent)
        foreach (var loopContent in loopDates.contentIds)
            await db.GenerationDailyPhotoLogs.AddAsync(new GenerationDailyPhotoLog
            {
                DailyPhotoDate = loopDates.date,
                GenerationVersion = currentGenerationVersion,
                RelatedContentId = loopContent
            }).ConfigureAwait(false);

        progress?.Report("Saving Photo Dates and Content to db");

        await db.SaveChangesAsync(true).ConfigureAwait(false);
    }


    public static async Task SetupTagGenerationDbData(DateTime currentGenerationVersion,
        IProgress<string>? progress = null)
    {
        var tagData = await Db.TagSlugsAndContentList(true, false, progress).ConfigureAwait(false);

        var excludedTagSlugs = await Db.TagExclusionSlugs().ConfigureAwait(false);

        var db = await Db.Context().ConfigureAwait(false);

        foreach (var (tag, contentObjects) in tagData)
        foreach (var loopContent in contentObjects)
            await db.GenerationTagLogs.AddAsync(new GenerationTagLog
            {
                GenerationVersion = currentGenerationVersion,
                RelatedContentId = loopContent.ContentId,
                TagSlug = tag,
                TagIsExcludedFromSearch = excludedTagSlugs.Contains(tag)
            }).ConfigureAwait(false);

        await db.SaveChangesAsync(true).ConfigureAwait(false);
    }
}