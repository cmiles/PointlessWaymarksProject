using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsData.Spatial.Elevation;
using PointlessWaymarks.FeatureIntersectionTags;
using Serilog;

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

        var tagList = new List<string>();

        if (trackInformation.Track.Any())
        {
            var stateCounty =
                await StateCountyService.GetStateCounty(trackInformation.Track.First().Y,
                    trackInformation.Track.First().X);
            tagList = new List<string> { stateCounty.state, stateCounty.county };
        }

        if (trackInformation.Track.Any() &&
            !string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile))
            try
            {
                var tagger = new Intersection();
                tagList.AddRange(tagger.Tags(
                    UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile,
                    new List<IFeature>
                    {
                        // ReSharper disable once CoVariantArrayConversion It appears from testing that a linestring will reflect CoordinateZ
                        new Feature(new LineString(trackInformation.Track.ToArray()), new AttributesTable())
                    }, CancellationToken.None, progress).SelectMany(x => x.Tags).ToList());
            }
            catch (Exception e)
            {
                Log.Error(e, "Silent Error with FeatureIntersectionTags in Photo Metadata Extraction");
            }

        var newEntry = new LineContent
        {
            ContentId = Guid.NewGuid(),
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = trackInformation.StartsOn ?? DateTime.Now,
            FeedOn = trackInformation.StartsOn ?? DateTime.Now,
            Line = await SpatialHelpers.GeoJsonWithLineStringFromCoordinateList(trackInformation.Track,
                replaceElevations, progress),
            Title = trackInformation.Name,
            Summary = trackInformation.Name,
            BodyContent = trackInformation.Description,
            LineDistance = lineStatistics.Length,
            MaximumElevation = lineStatistics.MaximumElevation,
            MinimumElevation = lineStatistics.MinimumElevation,
            ClimbElevation = lineStatistics.ElevationClimb,
            DescentElevation = lineStatistics.ElevationDescent,
            RecordingStartedOn = trackInformation.StartsOn,
            RecordingEndedOn = trackInformation.EndsOn,
            Tags = Db.TagListJoin(tagList)
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
            ContentId = Guid.NewGuid(),
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = DateTime.Now,
            FeedOn = DateTime.Now,
            Line = await SpatialHelpers.GeoJsonWithLineStringFromCoordinateList(trackInformation.Track,
                replaceElevations, progress),
            Title = trackInformation.Name,
            Summary = trackInformation.Name,
            BodyContent = trackInformation.Description,
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

        var geoJsonCheck = CommonContentValidation.LineGeoJsonValidation(lineContent.Line);
        if (!geoJsonCheck.Valid)
            return GenerationReturn.Error(updateFormatCheck.Explanation, lineContent.ContentId);

        return GenerationReturn.Success("Line Content Validation Successful");
    }
}