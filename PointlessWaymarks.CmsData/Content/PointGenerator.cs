using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.PointHtml;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData.Content
{
    public static class PointGenerator
    {
        public static void GenerateHtml(PointContentDto toGenerate, DateTime? generationVersion,
            IProgress<string> progress)
        {
            progress?.Report($"Point Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SinglePointPage(toGenerate) {GenerationVersion = generationVersion};

            htmlContext.WriteLocalHtml();
        }

        public static async Task<(GenerationReturn generationReturn, PointContentDto pointContent)> SaveAndGenerateHtml(
            PointContentDto toSave, DateTime? generationVersion, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            var savedPoint = await Db.SavePointContent(toSave);
            GenerateHtml(savedPoint, generationVersion, progress);
            await Export.WriteLocalDbJson(Db.PointContentDtoToPointContentAndDetails(savedPoint).content);

            DataNotifications.PublishDataNotification("Point Generator", DataNotificationContentType.Point,
                DataNotificationUpdateType.LocalContent, new List<Guid> {savedPoint.ContentId});

            return (await GenerationReturn.Success($"Saved and Generated Content And Html for {savedPoint.Title}"),
                savedPoint);
        }

        public static async Task<GenerationReturn> Validate(PointContentDto pointContent)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Item1)
                return await GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Item2}",
                    pointContent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateContentCommon(pointContent);
            if (!commonContentCheck.valid)
                return await GenerationReturn.Error(commonContentCheck.explanation, pointContent.ContentId);

            var latitudeCheck = CommonContentValidation.LatitudeValidation(pointContent.Latitude);
            if (!latitudeCheck.isValid)
                return await GenerationReturn.Error(latitudeCheck.explanation, pointContent.ContentId);

            var longitudeCheck = CommonContentValidation.LongitudeValidation(pointContent.Longitude);
            if (!longitudeCheck.isValid)
                return await GenerationReturn.Error(longitudeCheck.explanation, pointContent.ContentId);

            var elevationCheck = CommonContentValidation.ElevationValidation(pointContent.Elevation);
            if (!elevationCheck.isValid)
                return await GenerationReturn.Error(elevationCheck.explanation, pointContent.ContentId);

            var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(pointContent.UpdateNotesFormat);
            if (!updateFormatCheck.isValid)
                return await GenerationReturn.Error(updateFormatCheck.explanation, pointContent.ContentId);

            foreach (var loopDetails in pointContent.PointDetails)
            {
                if (loopDetails.ContentId == Guid.Empty)
                    return await GenerationReturn.Error("Point Detail Data must have a valid Content Id",
                        loopDetails.ContentId);
                if (loopDetails.PointContentId != pointContent.ContentId)
                    return await GenerationReturn.Error(
                        $"{loopDetails.DataType} Point Detail isn't assigned to the current point?",
                        loopDetails.ContentId);
                if (string.IsNullOrWhiteSpace(loopDetails.DataType))
                    return await GenerationReturn.Error("Point Detail Data Type doesn't have a value",
                        loopDetails.ContentId);
                if (string.IsNullOrWhiteSpace(loopDetails.StructuredDataAsJson))
                    return await GenerationReturn.Error($"{loopDetails.DataType} Point Detail doesn't have any data?",
                        loopDetails.ContentId);
                try
                {
                    var content =
                        Db.PointDetailDataFromIdentifierAndJson(loopDetails.DataType, loopDetails.StructuredDataAsJson);
                    var contentValidation = content.Validate();

                    if (!contentValidation.isValid)
                        return await GenerationReturn.Error(
                            $"{loopDetails.DataType} Point Detail: {contentValidation.validationMessage}",
                            pointContent.ContentId);
                }
                catch (Exception e)
                {
                    return await GenerationReturn.Error(
                        $"Exception loading the Structured Data for {loopDetails.DataType} Point Detail {e.Message}",
                        pointContent.ContentId);
                }
            }

            return await GenerationReturn.Success("Point Content Validation Successful");
        }
    }
}