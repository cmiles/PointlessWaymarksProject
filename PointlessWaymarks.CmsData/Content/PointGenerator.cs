using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData.Content;

public static class PointGenerator
{
    public static async Task GenerateHtml(PointContentDto toGenerate, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Point Content - Generate HTML for {toGenerate.Title}");

        var htmlContext = new SinglePointPage(toGenerate) { GenerationVersion = generationVersion };

        await htmlContext.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task<(GenerationReturn generationReturn, PointContentDto? pointContent)>
        SaveAndGenerateHtml(PointContentDto toSave, DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);
        toSave.Tags = Db.TagListCleanup(toSave.Tags);

        var savedPoint = await Db.SavePointContent(toSave).ConfigureAwait(false);

        await GenerateHtml(savedPoint!, generationVersion, progress).ConfigureAwait(false);
        await Export.WriteLocalDbJson(Db.PointContentDtoToPointContentAndDetails(savedPoint!).content, progress)
            .ConfigureAwait(false);

        DataNotifications.PublishDataNotification("Point Generator", DataNotificationContentType.Point,
            DataNotificationUpdateType.LocalContent, new List<Guid> { savedPoint!.ContentId });

        return (GenerationReturn.Success($"Saved and Generated Content And Html for {savedPoint.Title}"),
            savedPoint);
    }

    public static async Task<GenerationReturn> Validate(PointContentDto pointContent)
    {
        var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

        if (!rootDirectoryCheck.Valid)
            return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                pointContent.ContentId);

        var commonContentCheck =
            await CommonContentValidation.ValidateContentCommon(pointContent).ConfigureAwait(false);
        if (!commonContentCheck.Valid)
            return GenerationReturn.Error(commonContentCheck.Explanation, pointContent.ContentId);

        var latitudeCheck = await CommonContentValidation.LatitudeValidation(pointContent.Latitude);
        if (!latitudeCheck.Valid)
            return GenerationReturn.Error(latitudeCheck.Explanation, pointContent.ContentId);

        var longitudeCheck = await CommonContentValidation.LongitudeValidation(pointContent.Longitude);
        if (!longitudeCheck.Valid)
            return GenerationReturn.Error(longitudeCheck.Explanation, pointContent.ContentId);

        var elevationCheck = await CommonContentValidation.ElevationValidation(pointContent.Elevation);
        if (!elevationCheck.Valid)
            return GenerationReturn.Error(elevationCheck.Explanation, pointContent.ContentId);

        var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(pointContent.UpdateNotesFormat);
        if (!updateFormatCheck.Valid)
            return GenerationReturn.Error(updateFormatCheck.Explanation, pointContent.ContentId);

        var mapIconNameCheck = await CommonContentValidation.ValidatePointMapIconName(pointContent.MapIconName);
        if (!mapIconNameCheck.Valid)
            return GenerationReturn.Error(mapIconNameCheck.Explanation, pointContent.ContentId);

        var mapMarkerColorCheck = await CommonContentValidation.ValidatePointMapMarkerColor(pointContent.MapMarkerColor);
        if (!mapMarkerColorCheck.Valid)
            return GenerationReturn.Error(mapMarkerColorCheck.Explanation, pointContent.ContentId);

        foreach (var loopDetails in pointContent.PointDetails)
        {
            if (loopDetails.ContentId == Guid.Empty)
                return GenerationReturn.Error("Point Detail Data must have a valid Content Id",
                    loopDetails.ContentId);
            if (loopDetails.PointContentId != pointContent.ContentId)
                return GenerationReturn.Error(
                    $"{loopDetails.DataType} Point Detail isn't assigned to the current point?",
                    loopDetails.ContentId);
            if (string.IsNullOrWhiteSpace(loopDetails.DataType))
                return GenerationReturn.Error("Point Detail Data Type doesn't have a value", loopDetails.ContentId);
            if (string.IsNullOrWhiteSpace(loopDetails.StructuredDataAsJson))
                return GenerationReturn.Error($"{loopDetails.DataType} Point Detail doesn't have any data?",
                    loopDetails.ContentId);
            try
            {
                var content =
                    Db.PointDetailDataFromIdentifierAndJson(loopDetails.DataType, loopDetails.StructuredDataAsJson);

                if (content == null)
                    return GenerationReturn.Error($"{loopDetails.DataType} Point Detail returned null?",
                        pointContent.ContentId);

                var (isValid, validationMessage) = await content.Validate();

                if (!isValid)
                    return GenerationReturn.Error($"{loopDetails.DataType} Point Detail: {validationMessage}",
                        pointContent.ContentId);
            }
            catch (Exception e)
            {
                return GenerationReturn.Error(
                    $"Exception loading the Structured Data for {loopDetails.DataType} Point Detail {e.Message}",
                    pointContent.ContentId);
            }
        }

        return GenerationReturn.Success("Point Content Validation Successful");
    }
}