using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.GeoJsonHtml;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksCmsData.Content
{
    public static class GeoJsonGenerator
    {
        public static async Task GenerateHtml(GeoJsonContent toGenerate, DateTime? generationVersion,
            IProgress<string> progress)
        {
            progress?.Report($"GeoJson Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SingleGeoJsonPage(toGenerate) {GenerationVersion = generationVersion};

            await htmlContext.WriteLocalHtml();
        }

        public static async Task<(GenerationReturn generationReturn, GeoJsonContent geoJsonContent)>
            SaveAndGenerateHtml(GeoJsonContent toSave, DateTime? generationVersion, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            await Db.SaveGeoJsonContent(toSave);
            await GenerateHtml(toSave, generationVersion, progress);
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

            var updateFormatCheck =
                CommonContentValidation.ValidateUpdateContentFormat(geoJsonContent.UpdateNotesFormat);
            if (!updateFormatCheck.isValid)
                return await GenerationReturn.Error(updateFormatCheck.explanation, geoJsonContent.ContentId);

            try
            {
                var serializer = GeoJsonSerializer.Create();

                using var stringReader = new StringReader(geoJsonContent.GeoJson);
                using var jsonReader = new JsonTextReader(stringReader);
                var featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);
                if (featureCollection.Count < 1)
                    return await GenerationReturn.Error("The GeoJson appears to have an empty Feature Collection?",
                        geoJsonContent.ContentId);
            }
            catch (Exception e)
            {
                return await GenerationReturn.Error(
                    $"Error parsing a Feature Collection from the GeoJson, this CMS needs even single GeoJson types to be wrapped into a FeatureCollection... {e.Message}",
                    geoJsonContent.ContentId);
            }

            return await GenerationReturn.Success("GeoJson Content Validation Successful");
        }
    }
}