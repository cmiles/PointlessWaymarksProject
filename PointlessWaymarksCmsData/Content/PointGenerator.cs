using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.PointHtml;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksCmsData.Content
{
    public static class PointGenerator
    {
        public static void GenerateHtml(PointContent toGenerate, IProgress<string> progress)
        {
            progress?.Report($"Point Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SinglePointPage(toGenerate);

            htmlContext.WriteLocalHtml();
        }

        public static async Task<(GenerationReturn generationReturn, PointContent postContent)> SaveAndGenerateHtml(
            PointContent toSave, List<PointDetail> relatedDetails, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            await Db.SavePointContent(toSave, relatedDetails);
            GenerateHtml(toSave, progress);
            await Export.WriteLocalDbJson(toSave);

            DataNotifications.PublishDataNotification("Point Generator", DataNotificationContentType.Point,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (await GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<GenerationReturn> Validate(PointContent postContent)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Item1)
                return await GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Item2}",
                    postContent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateContentCommon(postContent);
            if (!commonContentCheck.valid)
                return await GenerationReturn.Error(commonContentCheck.explanation, postContent.ContentId);

            var latitudeCheck = CommonContentValidation.LatitudeValidation(postContent.Latitude);
            if (!latitudeCheck.isValid)
                return await GenerationReturn.Error(latitudeCheck.explanation, postContent.ContentId);

            var longitudeCheck = CommonContentValidation.LongitudeValidation(postContent.Longitude);
            if (!longitudeCheck.isValid)
                return await GenerationReturn.Error(longitudeCheck.explanation, postContent.ContentId);

            var elevationCheck = CommonContentValidation.ElevationValidation(postContent.Elevation);
            if (!elevationCheck.isValid)
                return await GenerationReturn.Error(elevationCheck.explanation, postContent.ContentId);

            var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(postContent.UpdateNotesFormat);
            if (!updateFormatCheck.isValid)
                return await GenerationReturn.Error(updateFormatCheck.explanation, postContent.ContentId);

            return await GenerationReturn.Success("Point Content Validation Successful");
        }
    }
}