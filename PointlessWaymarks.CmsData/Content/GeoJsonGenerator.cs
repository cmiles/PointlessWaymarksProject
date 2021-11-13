using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData.Content;

public static class GeoJsonGenerator
{
    public static async Task GenerateHtml(GeoJsonContent toGenerate, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        progress?.Report($"GeoJson Content - Generate HTML for {toGenerate.Title}");

        var htmlContext = new SingleGeoJsonPage(toGenerate) {GenerationVersion = generationVersion};

        await htmlContext.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task<(GenerationReturn generationReturn, GeoJsonContent? geoJsonContent)>
        SaveAndGenerateHtml(GeoJsonContent toSave, DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);
        toSave.Tags = Db.TagListCleanup(toSave.Tags);

        await Db.SaveGeoJsonContent(toSave).ConfigureAwait(false);
        await GenerateHtml(toSave, generationVersion, progress).ConfigureAwait(false);
        await Export.WriteLocalDbJson(toSave).ConfigureAwait(false);

        DataNotifications.PublishDataNotification("GeoJson Generator", DataNotificationContentType.GeoJson,
            DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

        return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
    }

    public static async Task<GenerationReturn> Validate(GeoJsonContent geoJsonContent)
    {
        var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

        if (!rootDirectoryCheck.Valid)
            return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                geoJsonContent.ContentId);

        var commonContentCheck = await CommonContentValidation.ValidateContentCommon(geoJsonContent).ConfigureAwait(false);
        if (!commonContentCheck.Valid)
            return GenerationReturn.Error(commonContentCheck.Explanation, geoJsonContent.ContentId);

        var (b, s) = CommonContentValidation.ValidateUpdateContentFormat(geoJsonContent.UpdateNotesFormat);
        if (!b)
            return GenerationReturn.Error(s, geoJsonContent.ContentId);

        var (isValid, explanation) = CommonContentValidation.GeoJsonValidation(geoJsonContent.GeoJson);
        if (!isValid)
            return GenerationReturn.Error(explanation, geoJsonContent.ContentId);

        return GenerationReturn.Success("GeoJson Content Validation Successful");
    }
}