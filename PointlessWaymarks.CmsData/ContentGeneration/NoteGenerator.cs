using System.Data;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentHtml.NoteHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentGeneration;

public static class NoteGenerator
{
    public static async Task GenerateHtml(NoteContent toGenerate, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Note Content - Generate HTML for {toGenerate.Title}");

        var htmlContext = new SingleNotePage(toGenerate) { GenerationVersion = generationVersion };

        await htmlContext.WriteLocalHtml().ConfigureAwait(false);
    }

    /// <summary>
    ///     Callers must check the generationReturn for success or failure!
    /// </summary>
    /// <param name="toSave"></param>
    /// <param name="generationVersion"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<(GenerationReturn generationReturn, NoteContent? noteContent)> SaveAndGenerateHtml(
        NoteContent toSave, DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        try
        {
            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);
            await Db.SaveNoteContent(toSave).ConfigureAwait(false);
            await GenerateHtml(toSave, generationVersion, progress).ConfigureAwait(false);
            await Export.WriteNoteContentData(toSave, progress).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return (
                GenerationReturn.Error(
                    $"Error with Note Content {toSave.Title}",
                    toSave.ContentId,
                    e), toSave);
        }

        DataNotifications.PublishDataNotification("Note Generator", DataNotificationContentType.Note,
            DataNotificationUpdateType.Update, [toSave.ContentId]);

        return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
    }

    public static async Task<string> UniqueNoteSlug()
    {
        var attemptCount = 1;

        var db = await Db.Context().ConfigureAwait(false);

        var currentLength = 6;

        if (await db.NoteContents.AnyAsync().ConfigureAwait(false))
        {
            var dbMaxLength = await db.NoteContents.Where(x => x.Slug != null).MaxAsync(x => x.Slug!.Length)
                .ConfigureAwait(false);
            currentLength = dbMaxLength > currentLength ? dbMaxLength : currentLength;
        }

        var possibleSlug = SlugTools.RandomLowerCaseString(currentLength);

        async Task<bool> SlugAlreadyExists(string slug)
        {
            return await db.NoteContents.AnyAsync(x => x.Slug == slug).ConfigureAwait(false);
        }

        while (await SlugAlreadyExists(possibleSlug).ConfigureAwait(false))
        {
            if (attemptCount > 1000)
                throw new DataException(
                    "Could not create a unique note slug in 1000 iterations - this almost certainly represents an error.");
            if (attemptCount % 10 == 0) currentLength++;
            attemptCount++;
            possibleSlug = SlugTools.RandomLowerCaseString(currentLength);
        }

        return possibleSlug;
    }

    public static async Task<GenerationReturn> Validate(NoteContent noteContent)
    {
        var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

        if (!rootDirectoryCheck.Valid)
            return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                noteContent.ContentId);

        var commonContentCheck = await CommonContentValidation.ValidateContentCommon(noteContent).ConfigureAwait(false);
        if (!commonContentCheck.Valid)
            return GenerationReturn.Error(commonContentCheck.Explanation, noteContent.ContentId);

        return GenerationReturn.Success("Note Content Validation Successful");
    }
}