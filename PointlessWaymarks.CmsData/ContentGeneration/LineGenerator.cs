using System.Diagnostics;
using GeoTimeZone;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.SpatialTools;
using Serilog;

namespace PointlessWaymarks.CmsData.ContentGeneration;

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
        bool replaceElevations, bool skipFeatureIntersectTagging, bool linkAssociatedPhotosInBody,
        IProgress<string> progress)
    {
        var lineStatistics = DistanceTools.LineStatsInImperialFromCoordinateList(trackInformation.Track);

        var tagList = new List<string>();

        if (trackInformation.Track.Any())
        {
            var stateCounty =
                await StateCountyService.GetStateCounty(trackInformation.Track.First().Y,
                    trackInformation.Track.First().X);
            tagList = [stateCounty.state, stateCounty.county];
        }

        if (trackInformation.Track.Any() && UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagOnImport &&
            !string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile) &&
            !skipFeatureIntersectTagging)
            try
            {
                // ReSharper disable once CoVariantArrayConversion
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

        var newEntry = NewContentModels.InitializeLineContent(null);

        newEntry.CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;
        newEntry.CreatedOn = trackInformation.StartsOnLocal ?? DateTime.Now;
        newEntry.FeedOn = trackInformation.StartsOnLocal ?? DateTime.Now;
        newEntry.ContentVersion = Db.ContentVersionDateTime();
        newEntry.Line = await LineTools.GeoJsonWithLineStringFromCoordinateList(trackInformation.Track,
            replaceElevations, progress);
        newEntry.Title = trackInformation.Name;
        newEntry.Summary = trackInformation.Name;
        newEntry.BodyContent = trackInformation.Description;
        newEntry.LineDistance = lineStatistics.Length;
        newEntry.MaximumElevation = lineStatistics.MaximumElevation;
        newEntry.MinimumElevation = lineStatistics.MinimumElevation;
        newEntry.ClimbElevation = lineStatistics.ElevationClimb;
        newEntry.DescentElevation = lineStatistics.ElevationDescent;
        newEntry.RecordingStartedOn = trackInformation.StartsOnLocal;
        newEntry.RecordingStartedOnUtc = trackInformation.StartsOnUtc;
        newEntry.RecordingEndedOn = trackInformation.EndsOnLocal;
        newEntry.RecordingEndedOnUtc = trackInformation.EndsOnUtc;
        newEntry.IncludeInActivityLog = newEntry is { RecordingStartedOn: not null, RecordingEndedOn: not null };
        newEntry.Tags = Db.TagListJoin(tagList);

        if (newEntry.Title.Contains("Hike", StringComparison.CurrentCultureIgnoreCase)
            || newEntry.Title.Contains("Hiking", StringComparison.CurrentCultureIgnoreCase)
            || newEntry.Title.Contains("Run", StringComparison.CurrentCultureIgnoreCase)
            || newEntry.Title.Contains("Walk", StringComparison.CurrentCultureIgnoreCase))
            newEntry.ActivityType = "On Foot";
        if (newEntry.Title.Contains("Bike", StringComparison.OrdinalIgnoreCase) ||
            newEntry.Title.Contains("Biking", StringComparison.OrdinalIgnoreCase))
            newEntry.ActivityType = "Biking";

        if (!string.IsNullOrWhiteSpace(trackInformation.Name))
            newEntry.Slug = SlugTools.CreateSlug(true, trackInformation.Name);
        if (trackInformation.StartsOnLocal != null)
            newEntry.Folder = trackInformation.StartsOnLocal.Value.Year.ToString();

        if (newEntry is { RecordingStartedOnUtc: not null, RecordingEndedOnUtc: not null })
        {
            var db = await Db.Context();
            var relatedPhotos = db.PhotoContents.Where(x =>
                x.PhotoCreatedOnUtc != null && x.PhotoCreatedOnUtc >= newEntry.RecordingStartedOnUtc &&
                x.PhotoCreatedOnUtc <= newEntry.RecordingEndedOnUtc).ToList();

            if (relatedPhotos.Count is > 0 and < 4)
            {
                var photoBodyAddition = string.Join($"{Environment.NewLine}{Environment.NewLine}",
                    relatedPhotos.Select(x => $"{BracketCodePhotos.Create(x)}"));

                newEntry.BodyContent =
                    $"{(string.IsNullOrWhiteSpace(newEntry.BodyContent) ? photoBodyAddition : $"{Environment.NewLine}{Environment.NewLine}{photoBodyAddition}")}";
            }

            if (relatedPhotos.Count is >= 4)
            {
                var mainPhoto = BracketCodePhotos.Create(relatedPhotos.First());
                var galleryPhotos = GalleryBracketCodePictures.Create(string.Join(
                    $"{Environment.NewLine}{Environment.NewLine}",
                    relatedPhotos.Skip(1).Select(x => $"{BracketCodePhotos.Create(x)}")));
                var photoBodyAddition = $"{mainPhoto}{Environment.NewLine}{Environment.NewLine}{galleryPhotos}";
                newEntry.BodyContent =
                    $"{(string.IsNullOrWhiteSpace(newEntry.BodyContent) ? photoBodyAddition : $"{Environment.NewLine}{Environment.NewLine}{photoBodyAddition}")}";
            }
        }

        return newEntry;
    }

    public static async Task<LineContent> NewFromGpxTrack(GpxTools.GpxRouteInformation trackInformation, bool replaceElevations, bool skipFeatureIntersectTagging, IProgress<string> progress)
    {
        var lineStatistics = DistanceTools.LineStatsInImperialFromCoordinateList(trackInformation.Track);

        var tagList = new List<string>();

        if (trackInformation.Track.Any())
        {
            var stateCounty =
                await StateCountyService.GetStateCounty(trackInformation.Track.First().Y,
                    trackInformation.Track.First().X);
            tagList = [stateCounty.state, stateCounty.county];
        }

        if (trackInformation.Track.Any() && UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagOnImport &&
            !string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile) &&
            !skipFeatureIntersectTagging)
            try
            {
                // ReSharper disable once CoVariantArrayConversion
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

        var newEntry = NewContentModels.InitializeLineContent(null);

        newEntry.CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;
        newEntry.ContentVersion = Db.ContentVersionDateTime();
        newEntry.Line = await LineTools.GeoJsonWithLineStringFromCoordinateList(trackInformation.Track,
            replaceElevations, progress);
        newEntry.Title = trackInformation.Name;
        newEntry.Summary = trackInformation.Name;
        newEntry.BodyContent = trackInformation.Description;
        newEntry.LineDistance = lineStatistics.Length;
        newEntry.MaximumElevation = lineStatistics.MaximumElevation;
        newEntry.MinimumElevation = lineStatistics.MinimumElevation;
        newEntry.ClimbElevation = lineStatistics.ElevationClimb;
        newEntry.DescentElevation = lineStatistics.ElevationDescent;
        newEntry.IncludeInActivityLog = newEntry is { RecordingStartedOn: not null, RecordingEndedOn: not null };
        newEntry.Tags = Db.TagListJoin(tagList);

        if (newEntry.Title.Contains("Hike", StringComparison.CurrentCultureIgnoreCase)
            || newEntry.Title.Contains("Hiking", StringComparison.CurrentCultureIgnoreCase)
            || newEntry.Title.Contains("Run", StringComparison.CurrentCultureIgnoreCase)
            || newEntry.Title.Contains("Walk", StringComparison.CurrentCultureIgnoreCase))
            newEntry.ActivityType = "On Foot";
        if (newEntry.Title.Contains("Bike", StringComparison.OrdinalIgnoreCase) ||
            newEntry.Title.Contains("Biking", StringComparison.OrdinalIgnoreCase))
            newEntry.ActivityType = "Biking";

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

        Debug.Assert(lineFeature != null, nameof(lineFeature) + " != null");

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
        else lineFeature.Attributes["description"] = $"Totals: {LineParts.LineStatsString(toSave)}";

        var possibleContentId = lineFeature.Attributes.GetOptionalValue("content-id");
        if (possibleContentId == null) lineFeature.Attributes.Add("content-id", toSave.ContentId);
        else lineFeature.Attributes["content-id"] = toSave.ContentId;

        if (toSave.RecordingStartedOn.HasValue)
        {
            var asLineString = lineFeature.Geometry as LineString;
            var startTimezoneIanaIdentifier =
                TimeZoneLookup.GetTimeZone(asLineString!.StartPoint.Y, asLineString.StartPoint.X);
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
                TimeZoneLookup.GetTimeZone(asLineString!.EndPoint.Y, asLineString.EndPoint.X);
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
        await Export.WriteLineContentData(toSave, progress).ConfigureAwait(false);

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