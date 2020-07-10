using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsData.Html.NoteHtml;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksCmsData.Content
{
    public static class NoteGenerator
    {
        public static void GenerateHtml(NoteContent toGenerate, IProgress<string> progress)
        {
            progress?.Report($"Note Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SingleNotePage(toGenerate);

            htmlContext.WriteLocalHtml();
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
                if(attemptCount > 1000) throw new DataException("Could not create a unique note slug in 1000 iterations - this almost certainly represents an error.");
                if (attemptCount % 10 == 0) currentLength++;
                attemptCount++;
                possibleSlug = SlugUtility.RandomLowerCaseString(currentLength);
            }

            return possibleSlug;
        }

        public static async Task<(GenerationReturn generationReturn, NoteContent NoteContent)> SaveAndGenerateHtml(
            NoteContent toSave, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            await Db.SaveNoteContent(toSave);
            GenerateHtml(toSave, progress);
            await Export.WriteLocalDbJson(toSave, progress);

            await DataNotifications.PublishDataNotification("Note Generator", DataNotificationContentType.Note,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (await GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<GenerationReturn> Validate(NoteContent noteContent)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Item1)
                return await GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Item2}",
                    noteContent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateContentCommon(noteContent);
            if (!commonContentCheck.valid)
                return await GenerationReturn.Error(commonContentCheck.explanation, noteContent.ContentId);

            if (noteContent == null) return await GenerationReturn.Error("Note Content is Null?");

            return await GenerationReturn.Success("Note Content Validation Successful");
        }
    }
}