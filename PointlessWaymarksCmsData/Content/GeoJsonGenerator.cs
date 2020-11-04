using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.GeoJsonHtml;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksCmsData.Content
{
    public static class GeoJsonGenerator
    {
        public static void GenerateHtml(GeoJsonContent toGenerate, DateTime? generationVersion, IProgress<string> progress)
        {
            progress?.Report($"GeoJson Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SingleGeoJsonPage(toGenerate) {GenerationVersion = generationVersion};

            htmlContext.WriteLocalHtml();
        }

        public static async Task<(GenerationReturn generationReturn, GeoJsonContent geoJsonContent)> SaveAndGenerateHtml(
            GeoJsonContent toSave, DateTime? generationVersion, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            await Db.SaveGeoJsonContent(toSave);
            GenerateHtml(toSave, generationVersion, progress);
            await Export.WriteLocalDbJson(toSave);

            DataNotifications.PublishDataNotification("GeoJson Generator", DataNotificationContentType.GeoJson,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (await GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<GenerationReturn> Validate(GeoJsonContent geoJsonContent)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Item1)
                return await GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Item2}",
                    geoJsonContent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateContentCommon(geoJsonContent);
            if (!commonContentCheck.valid)
                return await GenerationReturn.Error(commonContentCheck.explanation, geoJsonContent.ContentId);

            var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(geoJsonContent.UpdateNotesFormat);
            if (!updateFormatCheck.isValid)
                return await GenerationReturn.Error(updateFormatCheck.explanation, geoJsonContent.ContentId);

            return await GenerationReturn.Success("GeoJson Content Validation Successful");
        }
    }
}