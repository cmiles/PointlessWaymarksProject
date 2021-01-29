using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData.ContentHtml.MapComponentData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData.Content
{
    public static class MapComponentGenerator
    {
        public static async Task GenerateData(MapComponentDto toGenerate, IProgress<string>? progress = null)
        {
            progress?.Report($"Map Component - Generate Data for {toGenerate.Map.ContentId}, {toGenerate.Map.Title}");

            await MapData.WriteJsonData(toGenerate);
        }

        public static async Task<(GenerationReturn generationReturn, MapComponentDto? mapDto)> SaveAndGenerateData(
            MapComponentDto toSave, DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);

            var savedComponent = await Db.SaveMapComponent(toSave);

            await GenerateData(savedComponent, progress);

            await Export.WriteLocalDbJson(savedComponent.Map);

            DataNotifications.PublishDataNotification("Map Component Generator", DataNotificationContentType.Map,
                DataNotificationUpdateType.LocalContent, new List<Guid> {savedComponent.Map.ContentId});

            return (
                GenerationReturn.Success(
                    $"Saved and Generated Map Component {savedComponent.Map.ContentId} - {savedComponent.Map.Title}"),
                savedComponent);
        }

        public static async Task<GenerationReturn> Validate(MapComponentDto mapComponent)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Valid)
                return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                    mapComponent.Map.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateMapComponent(mapComponent);
            if (!commonContentCheck.Valid)
                return GenerationReturn.Error(commonContentCheck.Explanation, mapComponent.Map.ContentId);

            var updateFormatCheck =
                CommonContentValidation.ValidateUpdateContentFormat(mapComponent.Map.UpdateNotesFormat);
            if (!updateFormatCheck.Valid)
                return GenerationReturn.Error(updateFormatCheck.Explanation, mapComponent.Map.ContentId);

            return GenerationReturn.Success("GeoJson Content Validation Successful");
        }
    }
}