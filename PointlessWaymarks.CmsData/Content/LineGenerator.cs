using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsData.Spatial.Elevation;

namespace PointlessWaymarks.CmsData.Content;

public static class LineGenerator
{
    public static async Task GenerateHtml(LineContent toGenerate, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Line Content - Generate HTML for {toGenerate.Title}");

        var htmlContext = new SingleLinePage(toGenerate) { GenerationVersion = generationVersion };

        await htmlContext.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task<LineContent> NewFromGpxTrack(SpatialHelpers.GpxTrackInformation trackInformation,
        bool replaceElevations, IProgress<string> progress)
    {
        var lineStatistics = SpatialHelpers.LineStatsInImperialFromCoordinateList(trackInformation.Track);
        var stateCounty =
            await StateCountyService.GetStateCounty(trackInformation.Track.First().Y, trackInformation.Track.First().X);
        var stateCountyTagList = new List<string> { stateCounty.state, stateCounty.county };

        var newEntry = new LineContent
        {
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = trackInformation.StartsOn ?? DateTime.Now,
            FeedOn = trackInformation.StartsOn ?? DateTime.Now,
            Line = await SpatialHelpers.GeoJsonWithLineStringFromCoordinateList(trackInformation.Track,
                replaceElevations, progress),
            Title = trackInformation.Name,
            Summary = string.IsNullOrWhiteSpace(trackInformation.Description)
                ? trackInformation.Name
                : trackInformation.Description,
            LineDistance = lineStatistics.Length,
            MaximumElevation = lineStatistics.MaximumElevation,
            MinimumElevation = lineStatistics.MinimumElevation,
            ClimbElevation = lineStatistics.ElevationClimb,
            DescentElevation = lineStatistics.ElevationDescent,
            RecordingStartedOn = trackInformation.StartsOn,
            RecordingEndedOn = trackInformation.EndsOn,
            Tags = Db.TagListJoinAsSlugs(stateCountyTagList, false)
        };

        if (!string.IsNullOrWhiteSpace(trackInformation.Name))
            newEntry.Slug = SlugUtility.Create(true, trackInformation.Name);
        if (trackInformation.StartsOn != null) newEntry.Folder = trackInformation.StartsOn.Value.Year.ToString();

        return newEntry;
    }

    public static async Task<LineContent> NewFromGpxTrack(SpatialHelpers.GpxRouteInformation trackInformation,
        bool replaceElevations, IProgress<string> progress)
    {
        var lineStatistics = SpatialHelpers.LineStatsInImperialFromCoordinateList(trackInformation.Track);

        var newEntry = new LineContent
        {
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = DateTime.Now,
            FeedOn = DateTime.Now,
            Line = await SpatialHelpers.GeoJsonWithLineStringFromCoordinateList(trackInformation.Track,
                replaceElevations, progress),
            Title = trackInformation.Name,
            Summary = string.IsNullOrWhiteSpace(trackInformation.Description)
                ? trackInformation.Name
                : trackInformation.Description,
            LineDistance = lineStatistics.Length,
            MaximumElevation = lineStatistics.MaximumElevation,
            MinimumElevation = lineStatistics.MinimumElevation,
            ClimbElevation = lineStatistics.ElevationClimb,
            DescentElevation = lineStatistics.ElevationDescent,
            RecordingStartedOn = null,
            RecordingEndedOn = null
        };

        if (!string.IsNullOrWhiteSpace(trackInformation.Name))
            newEntry.Slug = SlugUtility.Create(true, trackInformation.Name);

        return newEntry;
    }

    public static async Task<(GenerationReturn generationReturn, LineContent? lineContent)> SaveAndGenerateHtml(
        LineContent toSave, DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);
        toSave.Tags = Db.TagListCleanup(toSave.Tags);

        await Db.SaveLineContent(toSave).ConfigureAwait(false);
        await GenerateHtml(toSave, generationVersion, progress).ConfigureAwait(false);
        await Export.WriteLocalDbJson(toSave).ConfigureAwait(false);

        DataNotifications.PublishDataNotification("Line Generator", DataNotificationContentType.Line,
            DataNotificationUpdateType.LocalContent, new List<Guid> { toSave.ContentId });

        return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
    }

    public static async Task<GenerationReturn> Validate(LineContent lineContent)
    {
        var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

        if (!rootDirectoryCheck.Valid)
            return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                lineContent.ContentId);

        var commonContentCheck = await CommonContentValidation.ValidateContentCommon(lineContent).ConfigureAwait(false);
        if (!commonContentCheck.Valid)
            return GenerationReturn.Error(commonContentCheck.Explanation, lineContent.ContentId);

        var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(lineContent.UpdateNotesFormat);
        if (!updateFormatCheck.Valid)
            return GenerationReturn.Error(updateFormatCheck.Explanation, lineContent.ContentId);

        if (string.IsNullOrWhiteSpace(lineContent.Line))
            return GenerationReturn.Error("LineContent Line can not be null of empty.");

        try
        {
            var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
                SpatialHelpers.Wgs84GeometryFactory(), 3);

            using var stringReader = new StringReader(lineContent.Line);
            using var jsonReader = new JsonTextReader(stringReader);
            var featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);
            if (featureCollection.Count < 1)
                return GenerationReturn.Error(
                    "The GeoJson for the line appears to have an empty Feature Collection?", lineContent.ContentId);
            if (featureCollection.Count > 1)
                return GenerationReturn.Error(
                    "The GeoJson for the line appears to contain multiple elements? It should only contain 1 line...",
                    lineContent.ContentId);
            if (featureCollection[0].Geometry is not LineString)
                return GenerationReturn.Error("The GeoJson for the line has one element but it isn't a LineString?",
                    lineContent.ContentId);
            var lineString = featureCollection[0].Geometry as LineString;
            if (lineString == null || lineString.Count < 1 || lineString.Length == 0)
                return GenerationReturn.Error("The LineString doesn't have any points or is zero length?",
                    lineContent.ContentId);
        }
        catch (Exception e)
        {
            return GenerationReturn.Error(
                $"Error parsing the FeatureCollection and/or problems checking the LineString {e.Message}",
                lineContent.ContentId);
        }

        return GenerationReturn.Success("Line Content Validation Successful");
    }
}