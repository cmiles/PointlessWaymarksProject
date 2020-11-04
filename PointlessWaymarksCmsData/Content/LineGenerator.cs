using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.LineHtml;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksCmsData.Content
{
    public static class LineGenerator
    {
        public static void GenerateHtml(LineContent toGenerate, DateTime? generationVersion, IProgress<string> progress)
        {
            progress?.Report($"Line Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SingleLinePage(toGenerate) {GenerationVersion = generationVersion};

            htmlContext.WriteLocalHtml();
        }

        public static async Task<(GenerationReturn generationReturn, LineContent lineContent)> SaveAndGenerateHtml(
            LineContent toSave, DateTime? generationVersion, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            await Db.SaveLineContent(toSave);
            GenerateHtml(toSave, generationVersion, progress);
            await Export.WriteLocalDbJson(toSave);

            DataNotifications.PublishDataNotification("Line Generator", DataNotificationContentType.Line,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (await GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<GenerationReturn> Validate(LineContent lineContent)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Item1)
                return await GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Item2}",
                    lineContent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateContentCommon(lineContent);
            if (!commonContentCheck.valid)
                return await GenerationReturn.Error(commonContentCheck.explanation, lineContent.ContentId);

            var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(lineContent.UpdateNotesFormat);
            if (!updateFormatCheck.isValid)
                return await GenerationReturn.Error(updateFormatCheck.explanation, lineContent.ContentId);

            return await GenerationReturn.Success("Line Content Validation Successful");
        }
    }
}