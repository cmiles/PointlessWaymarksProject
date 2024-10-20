using PointlessWaymarks.CmsData.ContentHtml.TrailHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData.ContentGeneration;

public static class TrailGenerator
{
    public static async Task GenerateHtml(TrailContent toGenerate, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Trail Content - Generate HTML for {toGenerate.Title}");

        var htmlContext = new SingleTrailPage(toGenerate) { GenerationVersion = generationVersion };

        await htmlContext.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task<(GenerationReturn generationReturn, TrailContent? trailContent)> SaveAndGenerateHtml(
        TrailContent toSave, DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);
        toSave.Tags = Db.TagListCleanup(toSave.Tags);

        await Db.SaveTrailContent(toSave).ConfigureAwait(false);
        await GenerateHtml(toSave, generationVersion, progress).ConfigureAwait(false);
        await Export.WriteTrailContentData(toSave, progress).ConfigureAwait(false);

        DataNotifications.PublishDataNotification("Trail Generator", DataNotificationContentType.Trail,
            DataNotificationUpdateType.LocalContent, [toSave.ContentId]);

        return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
    }

    public static async Task<GenerationReturn> Validate(TrailContent trailContent)
    {
        var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

        if (!rootDirectoryCheck.Valid)
            return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                trailContent.ContentId);

        var commonContentCheck =
            await CommonContentValidation.ValidateContentCommon(trailContent).ConfigureAwait(false);
        if (!commonContentCheck.Valid)
            return GenerationReturn.Error(commonContentCheck.Explanation, trailContent.ContentId);

        var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(trailContent.UpdateNotesFormat);
        if (!updateFormatCheck.Valid)
            return GenerationReturn.Error(updateFormatCheck.Explanation, trailContent.ContentId);

        return GenerationReturn.Success("Trail Content Validation Successful");
    }
}