using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.MapComponentData;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksCmsData.Content
{
    public static class MapComponentGenerator
    {
        public static async Task GenerateData(MapComponentDto toGenerate, IProgress<string> progress)
        {
            progress?.Report($"Map Component - Generate Data for {toGenerate.Map.ContentId}, {toGenerate.Map.Title}");

            await MapData.WriteLocalJsonData(toGenerate);
        }

        public static async Task<(GenerationReturn generationReturn, MapComponentDto mapDto)> SaveAndGenerateData(
            MapComponentDto toSave, DateTime? generationVersion, IProgress<string> progress)
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
                await GenerationReturn.Success(
                    $"Saved and Generated Map Component {savedComponent.Map.ContentId} - {savedComponent.Map.Title}"),
                savedComponent);
        }

        public static async Task<GenerationReturn> Validate(MapComponentDto mapComponent)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Item1)
                return await GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Item2}",
                    mapComponent.Map.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateMapComponent(mapComponent);
            if (!commonContentCheck.valid)
                return await GenerationReturn.Error(commonContentCheck.explanation, mapComponent.Map.ContentId);

            var updateFormatCheck =
                CommonContentValidation.ValidateUpdateContentFormat(mapComponent.Map.UpdateNotesFormat);
            if (!updateFormatCheck.isValid)
                return await GenerationReturn.Error(updateFormatCheck.explanation, mapComponent.Map.ContentId);

            return await GenerationReturn.Success("GeoJson Content Validation Successful");
        }
    }
}