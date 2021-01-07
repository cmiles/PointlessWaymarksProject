using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.LineHtml;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CmsData.Spatial;

namespace PointlessWaymarks.CmsData.Content
{
    public static class LineGenerator
    {
        public static async Task GenerateHtml(LineContent toGenerate, DateTime? generationVersion,
            IProgress<string> progress)
        {
            progress?.Report($"Line Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SingleLinePage(toGenerate) {GenerationVersion = generationVersion};

            await htmlContext.WriteLocalHtml();
        }

        public static async Task<(GenerationReturn generationReturn, LineContent lineContent)> SaveAndGenerateHtml(
            LineContent toSave, DateTime? generationVersion, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            await Db.SaveLineContent(toSave);
            await GenerateHtml(toSave, generationVersion, progress);
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

            try
            {
                var serializer = GeoJsonSerializer.Create(SpatialHelpers.Wgs84GeometryFactory(), 3);

                using var stringReader = new StringReader(lineContent.Line);
                using var jsonReader = new JsonTextReader(stringReader);
                var featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);
                if (featureCollection.Count < 1)
                    return await GenerationReturn.Error(
                        "The GeoJson for the line appears to have an empty Feature Collection?", lineContent.ContentId);
                if (featureCollection.Count > 1)
                    return await GenerationReturn.Error(
                        "The GeoJson for the line appears to contain multiple elements? It should only contain 1 line...",
                        lineContent.ContentId);
                if (featureCollection[0].Geometry is not LineString)
                    return await GenerationReturn.Error(
                        "The GeoJson for the line has one element but it isn't a LineString?", lineContent.ContentId);
                var lineString = featureCollection[0].Geometry as LineString;
                if (lineString == null || lineString.Count < 1 || lineString.Length == 0)
                    return await GenerationReturn.Error("The LineString doesn't have any points or is zero length?",
                        lineContent.ContentId);
            }
            catch (Exception e)
            {
                return await GenerationReturn.Error(
                    $"Error parsing the FeatureCollection and/or problems checking the LineString {e.Message}",
                    lineContent.ContentId);
            }

            return await GenerationReturn.Success("Line Content Validation Successful");
        }
    }
}