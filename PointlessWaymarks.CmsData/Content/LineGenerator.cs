using DocumentFormat.OpenXml.Vml;
using GeoTimeZone;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.FeatureIntersectionTags.Models;
using PointlessWaymarks.SpatialTools;
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

    public static async Task<LineContent> NewFromGpxTrack(GpxTools.GpxTrackInformation trackInformation,
        bool replaceElevations, bool skipFeatureIntersectTagging, IProgress<string> progress)
    {
        var lineStatistics = DistanceTools.LineStatsInImperialFromCoordinateList(trackInformation.Track);

        var tagList = new List<string>();

        if (trackInformation.Track.Any())
        {
            var stateCounty =
                await StateCountyService.GetStateCounty(trackInformation.Track.First().Y,
                    trackInformation.Track.First().X);
            tagList = new List<string> { stateCounty.state, stateCounty.county };
        }

        if (trackInformation.Track.Any() &&
            !string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile) &&!skipFeatureIntersectTagging)
            try
            {
                var lineFeature = new Feature(new LineString(trackInformation.Track.ToArray()),
                        new AttributesTable());

                tagList.AddRange(lineFeature.IntersectionTags(
                    UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile,
                    CancellationToken.None, progress));
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
            CreatedOn = trackInformation.StartsOnLocal ?? DateTime.Now,
            FeedOn = trackInformation.StartsOnLocal ?? DateTime.Now,
            Line = await LineTools.GeoJsonWithLineStringFromCoordinateList(trackInformation.Track,
                replaceElevations, progress),
            Title = trackInformation.Name,
            Summary = trackInformation.Name,
            BodyContent = trackInformation.Description,
            LineDistance = lineStatistics.Length,
            MaximumElevation = lineStatistics.MaximumElevation,
            MinimumElevation = lineStatistics.MinimumElevation,
            ClimbElevation = lineStatistics.ElevationClimb,
            DescentElevation = lineStatistics.ElevationDescent,
            RecordingStartedOn = trackInformation.StartsOnLocal,
            RecordingStartedOnUtc = trackInformation.StartsOnUtc,
            RecordingEndedOn = trackInformation.EndsOnLocal,
            RecordingEndedOnUtc = trackInformation.EndsOnUtc,
            Tags = Db.TagListJoin(tagList)
        };

        if (!string.IsNullOrWhiteSpace(trackInformation.Name))
            newEntry.Slug = SlugTools.CreateSlug(true, trackInformation.Name);
        if (trackInformation.StartsOnLocal != null) newEntry.Folder = trackInformation.StartsOnLocal.Value.Year.ToString();

        return newEntry;
    }

    public static async Task<LineContent> NewFromGpxTrack(GpxTools.GpxRouteInformation trackInformation,
        bool replaceElevations, IProgress<string> progress)
    {
        var lineStatistics = DistanceTools.LineStatsInImperialFromCoordinateList(trackInformation.Track);

        var newEntry = new LineContent
        {
            ContentId = Guid.NewGuid(),
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = DateTime.Now,
            FeedOn = DateTime.Now,
            Line = await LineTools.GeoJsonWithLineStringFromCoordinateList(trackInformation.Track,
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
            newEntry.Slug = SlugTools.CreateSlug(true, trackInformation.Name);

        return newEntry;
    }

    public static async Task<(GenerationReturn generationReturn, LineContent? lineContent)> SaveAndGenerateHtml(
        LineContent toSave, DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);
        toSave.Tags = Db.TagListCleanup(toSave.Tags);

        var lineFeature = LineContent.FeatureFromGeoJsonLine(toSave.Line);
        
        var possibleTitle = lineFeature.Attributes.GetOptionalValue("title");
        if (possibleTitle == null) lineFeature.Attributes.Add("title", toSave.Title);
        else lineFeature.Attributes["title"] = toSave.Title;

        var possibleTitleLink = lineFeature.Attributes.GetOptionalValue("title-link");
        if (possibleTitleLink == null)
            lineFeature.Attributes
                .Add("title-link", UserSettingsSingleton.CurrentSettings().LinePageUrl(toSave));
        else
            lineFeature.Attributes["title-link"] = UserSettingsSingleton.CurrentSettings().LinePageUrl(toSave);

        var possibleDescription = lineFeature.Attributes.GetOptionalValue("description");
        if (possibleDescription == null) lineFeature.Attributes.Add("description", LineParts.LineStatsString(toSave));
        else lineFeature.Attributes["description"] = LineParts.LineStatsString(toSave);

        if(toSave.RecordingStartedOn.HasValue)
        {
            var asLineString = lineFeature.Geometry as LineString;
            var startTimezoneIanaIdentifier =
                TimeZoneLookup.GetTimeZone(asLineString.StartPoint.Y, asLineString.StartPoint.X);
            var startTimeZone = TimeZoneInfo.FindSystemTimeZoneById(startTimezoneIanaIdentifier.Result);
            var startUtcOffset = startTimeZone.GetUtcOffset(toSave.RecordingStartedOn.Value);
            toSave.RecordingStartedOnUtc = toSave.RecordingStartedOn.Value.Subtract(startUtcOffset);
        }
        else
        {
            toSave.RecordingStartedOnUtc = null;
        }

        if (toSave.RecordingEndedOn.HasValue)
        {
            var asLineString = lineFeature.Geometry as LineString;
            var endTimezoneIanaIdentifier =
                TimeZoneLookup.GetTimeZone(asLineString.EndPoint.Y, asLineString.EndPoint.X);
            var endTimeZone = TimeZoneInfo.FindSystemTimeZoneById(endTimezoneIanaIdentifier.Result);
            var endUtcOffset = endTimeZone.GetUtcOffset(toSave.RecordingEndedOn.Value);
            toSave.RecordingEndedOnUtc = toSave.RecordingEndedOn.Value.Subtract(endUtcOffset);
        }
        else
        {
            toSave.RecordingEndedOnUtc = null;
        }

        toSave.Line = await GeoJsonTools.SerializeFeatureToGeoJson(lineFeature);
        
        await Db.SaveLineContent(toSave).ConfigureAwait(false);
        await GenerateHtml(toSave, generationVersion, progress).ConfigureAwait(false);
        await Export.WriteLocalDbJson(toSave, progress).ConfigureAwait(false);

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