using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData.ContentGeneration;

public static class SnippetGenerator
{
    public static async Task<(GenerationReturn generationReturn, Snippet? postContent)> SaveAndGenerateHtml(
        Snippet toSave, IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);

        await Db.SaveSnippet(toSave).ConfigureAwait(false);
        await Export.WriteSnippetData(toSave, progress).ConfigureAwait(false);

        DataNotifications.PublishDataNotification("Snippet Generator", DataNotificationContentType.Snippet,
            DataNotificationUpdateType.LocalContent, [toSave.ContentId]);

        return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
    }

    public static async Task<GenerationReturn> Validate(Snippet snippet)
    {
        var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

        if (!rootDirectoryCheck.Valid)
            return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                snippet.ContentId);

        var isNewEntry = snippet.Id < 1;

        var titleValidation = await CommonContentValidation.ValidateTitle(snippet.Title);
        if (!titleValidation.Valid)
            return GenerationReturn.Error(titleValidation.Explanation, snippet.ContentId);

        var summaryValidation = await CommonContentValidation.ValidateSummary(snippet.Summary);
        if (!summaryValidation.Valid) return GenerationReturn.Error(summaryValidation.Explanation, snippet.ContentId);

        var createdUpdatedValidation = await CommonContentValidation.ValidateCreatedAndUpdatedBy(snippet, isNewEntry);
        if (!createdUpdatedValidation.Valid) return GenerationReturn.Error(createdUpdatedValidation.Explanation, snippet.ContentId);

        return GenerationReturn.Success("Post Content Validation Successful");
    }
}