using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html;
using PointlessWaymarks.CmsData.Html.NoteHtml;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData.Content
{
    public static class NoteGenerator
    {
        public static void GenerateHtml(NoteContent toGenerate, DateTime? generationVersion, IProgress<string> progress)
        {
            progress?.Report($"Note Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SingleNotePage(toGenerate) {GenerationVersion = generationVersion};

            htmlContext.WriteLocalHtml();
        }

        public static async Task<(GenerationReturn generationReturn, NoteContent noteContent)> SaveAndGenerateHtml(
            NoteContent toSave, DateTime? generationVersion, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            await Db.SaveNoteContent(toSave);
            GenerateHtml(toSave, generationVersion, progress);
            await Export.WriteLocalDbJson(toSave, progress);

            DataNotifications.PublishDataNotification("Note Generator", DataNotificationContentType.Note,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<string> UniqueNoteSlug()
        {
            var attemptCount = 1;

            var db = await Db.Context();

            var currentLength = 6;

            if (await db.NoteContents.AnyAsync())
            {
                var dbMaxLength = await db.NoteContents.MaxAsync(x => x.Slug.Length);
                currentLength = dbMaxLength > currentLength ? dbMaxLength : currentLength;
            }

            var possibleSlug = SlugUtility.RandomLowerCaseString(currentLength);

            async Task<bool> SlugAlreadyExists(string slug)
            {
                return await db.NoteContents.AnyAsync(x => x.Slug == slug);
            }

            while (await SlugAlreadyExists(possibleSlug))
            {
                if (attemptCount > 1000)
                    throw new DataException(
                        "Could not create a unique note slug in 1000 iterations - this almost certainly represents an error.");
                if (attemptCount % 10 == 0) currentLength++;
                attemptCount++;
                possibleSlug = SlugUtility.RandomLowerCaseString(currentLength);
            }

            return possibleSlug;
        }

        public static async Task<GenerationReturn> Validate(NoteContent noteContent)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Valid)
                return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                    noteContent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateContentCommon(noteContent);
            if (!commonContentCheck.Valid)
                return GenerationReturn.Error(commonContentCheck.Explanation, noteContent.ContentId);

            return GenerationReturn.Success("Note Content Validation Successful");
        }
    }
}