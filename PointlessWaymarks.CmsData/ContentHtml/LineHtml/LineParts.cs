using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.LineHtml;

public static class LineParts
{
    public static HtmlTag DownloadLinkTag(LineContent content)
    {
        if (!content.PublicDownloadLink) return HtmlTag.Empty();

        var downloadLinkContainer = new DivTag().AddClass("file-download-container");

        var settings = UserSettingsSingleton.CurrentSettings();
        var gpxDownloadLink =
            new LinkTag("Download GPX", settings.LineGpxDownloadUrl(content)).AddClass("file-download-link");
        var jsonDownloadLink =
            new LinkTag("Download GeoJson", settings.LineJsonDownloadUrl(content)).AddClass("file-download-link");
        downloadLinkContainer.Children.Add(gpxDownloadLink);
        downloadLinkContainer.Children.Add(jsonDownloadLink);

        return downloadLinkContainer;
    }

    public static string LineDivAndScript(LineContent content)
    {
        var divScriptGuidConnector = Guid.NewGuid();

        var tag =
            $"<div id=\"Line-{divScriptGuidConnector}\" class=\"leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag point-content-map\"></div>";

        var script =
            $"<script>lazyInit(document.querySelector(\"#Line-{divScriptGuidConnector}\"), () => singleLineMapInit(document.querySelector(\"#Line-{divScriptGuidConnector}\"), \"{content.ContentId}\"))</script>";

        return tag + script;
    }

    public static string LineDivAndScriptForDirectLocalAccess(LineContent content)
    {
        var divScriptGuidConnector = Guid.NewGuid();

        var tag =
            $"<div id=\"Line-{divScriptGuidConnector}\" class=\"leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag point-content-map\"></div>";

        var smallPictureUrl = content.MainPicture == null
            ? string.Empty
            : new PictureSiteInformation(content.MainPicture.Value).Pictures?.SmallPicture?.SiteUrl ?? string.Empty;

        var script =
            $"<script>lazyInit(document.querySelector(\"#Line-{divScriptGuidConnector}\"), () => singleLineMapInitFromLineData(document.querySelector(\"#Line-{divScriptGuidConnector}\"), {LineData.GenerateLineJson(content.Line ?? string.Empty, content.Title ?? string.Empty, string.Empty, smallPictureUrl).Result}))</script>";

        return tag + script;
    }

    public static string LineDivAndScriptWithCaption(LineContent content)
    {
        var titleCaption =
            $"<a class=\"map-figure-title-caption\" href=\"{UserSettingsSingleton.CurrentSettings().LinePageUrl(content)}\">{content.Title}</a>";

        return $"<figure class=\"map-figure\">{LineDivAndScript(content)}{titleCaption}</figure>";
    }

    public static string LineDivAndScriptWithCaptionForDirectLocalAccess(LineContent content)
    {
        var titleCaption =
            $"<a class=\"map-figure-title-caption\" href=\"{UserSettingsSingleton.CurrentSettings().LinePageUrl(content)}\">{content.Title}</a>";

        return $"<figure class=\"map-figure\">{LineDivAndScriptForDirectLocalAccess(content)}{titleCaption}</figure>";
    }


    /// <summary>
    ///     Returns the total minutes and a well formatted human readable string of a Lines duration in
    ///     Hours and Minutes or null if either the start or end time are null.
    /// </summary>
    /// <param name="dbEntry"></param>
    /// <returns></returns>
    public static (int? totalMinutes, string? presentationString) LineDurationInHoursAndMinutes(LineContent dbEntry)
    {
        if (dbEntry is { RecordingStartedOnUtc: { }, RecordingEndedOnUtc: { } })
        {
            var minuteDuration =
                (int)dbEntry.RecordingEndedOnUtc.Value.Subtract(dbEntry.RecordingStartedOnUtc.Value).TotalMinutes;

            if (minuteDuration > 1)
            {
                var hours = minuteDuration / 60;
                var minutes = minuteDuration - hours * 60;

                if (hours == 0)
                    return (minuteDuration, $"{minutes} Minutes");
                if (minutes == 0)
                    return (minuteDuration, $"{hours} Hour{(hours > 1 ? "s" : "")}");
                return (minuteDuration,
                    $"{hours} Hour{(hours > 1 ? "s" : "")} {minutes} Minute{(minutes > 1 ? "s" : "")}");
            }
        }

        return (null, null);
    }

    public static string LineElevationChartDivAndScript(LineContent content)
    {
        var divScriptGuidConnector = Guid.NewGuid();

        var tag = $"""
                  <div id="LineElevationContainer-{divScriptGuidConnector}" class="line-elevation-chart-container">
                   <canvas id="LineElevationChart-{divScriptGuidConnector}" class="line-elevation-chart"></canvas>
                  </div>
                  """;
        var script =
            $"<script>lazyInit(document.querySelector('#LineElevationChart-{divScriptGuidConnector}'), () => singleLineElevationChartInit(document.querySelector('#LineElevationChart-{divScriptGuidConnector}'), '{content.ContentId}'))</script>";

        return tag + script;
    }

    /// <summary>
    ///     This is intended as a way to display Line Stats in other content - some details are omitted vs. the details
    ///     shown on a Line Page so that the stats are as likely as possible to be relevant and useful.
    /// </summary>
    /// <param name="dbEntry"></param>
    /// <returns></returns>
    public static HtmlTag LineStatisticsGeneralDisplayDiv(LineContent dbEntry)
    {
        var outerContainer = new DivTag().AddClasses("photo-details-container", "info-list-container");

        outerContainer.Children.Add(new DivTag().AddClasses("photo-detail-label-tag", "info-list-label")
            .Text("Details:"));

        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.LineDistance:F2} Miles", "line-detail", "distance",
            dbEntry.LineDistance.ToString("F2")));

        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.MinimumElevation:F0}' Min Elevation", "line-detail",
            "minimum-elevation", dbEntry.MaximumElevation.ToString("F0")));
        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.MaximumElevation:F0}' Max Elevation", "line-detail",
            "maximum-elevation", dbEntry.MaximumElevation.ToString("F0")));

        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.ClimbElevation:F0}' Climbing", "line-detail",
            "climbing", dbEntry.ClimbElevation.ToString("F0")));
        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.DescentElevation:F0}' Descent", "line-detail",
            "descent", dbEntry.DescentElevation.ToString("F0")));

        if (dbEntry.PublicDownloadLink)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            outerContainer.Children.Add(Tags.InfoLinkDivTag(settings.LineGpxDownloadUrl(dbEntry), "GPX",
                "line-detail", "line-data", dbEntry.DescentElevation.ToString("F0")));
            outerContainer.Children.Add(Tags.InfoLinkDivTag(settings.LineJsonDownloadUrl(dbEntry), "GeoJson",
                "line-detail", "line-data", dbEntry.DescentElevation.ToString("F0")));
        }

        //Return empty if there are no details
        return outerContainer.Children.Count(x => !x.IsEmpty()) > 1 ? outerContainer : HtmlTag.Empty();
    }

    /// <summary>
    ///     This div is intended to be used when stats are presented along with the Line Content and have details and
    ///     presentation that are
    ///     appropriate when it is clear 'the line' is the reference. See the LineStatisticsGeneralDisplayDiv method for
    ///     another choice.
    /// </summary>
    /// <param name="dbEntry"></param>
    /// <returns></returns>
    public static HtmlTag LineStatisticsWithContentDiv(LineContent dbEntry)
    {
        var outerContainer = new DivTag().AddClasses("photo-details-container", "info-list-container");

        outerContainer.Children.Add(new DivTag().AddClasses("photo-detail-label-tag", "info-list-label")
            .Text("Details:"));

        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.LineDistance:F2} Miles", "line-detail", "distance",
            dbEntry.LineDistance.ToString("F2")));

        var duration = LineDurationInHoursAndMinutes(dbEntry);

        if (duration.totalMinutes is not null)
        {
            outerContainer.Children.Add(Tags.InfoTextDivTag(duration.presentationString,
                "line-detail", "duration", $"{duration.totalMinutes}"));

            var mph = dbEntry.LineDistance / (duration.totalMinutes.Value / 60D);

            if (mph >= .1)
                outerContainer.Children.Add(Tags.InfoTextDivTag($"{mph:F1} Mph", "line-detail",
                    "pace-in-mph", mph.ToString("F1")));
        }

        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.ClimbElevation:F0}' Climbing", "line-detail",
            "climbing", dbEntry.ClimbElevation.ToString("F0")));

        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.DescentElevation:F0}' Descent", "line-detail",
            "descent", dbEntry.DescentElevation.ToString("F0")));

        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.MinimumElevation:F0}' Min Elevation", "line-detail",
            "minimum-elevation", dbEntry.MaximumElevation.ToString("F0")));
        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.MaximumElevation:F0}' Max Elevation", "line-detail",
            "maximum-elevation", dbEntry.MaximumElevation.ToString("F0")));

        if (dbEntry.RecordingStartedOn.HasValue)
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.RecordingStartedOn:M/d/yy h:mm tt} Start",
                "line-detail", "start-datetime", $"{dbEntry.RecordingStartedOn.Value:U}"));

        if (dbEntry.RecordingEndedOn.HasValue)
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.RecordingEndedOn:M/d/yy h:mm tt} End",
                "line-detail", "end-datetime", $"{dbEntry.RecordingEndedOn.Value:U}"));

        if (dbEntry.PublicDownloadLink)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            outerContainer.Children.Add(Tags.InfoLinkDivTag(settings.LineGpxDownloadUrl(dbEntry), "GPX",
                "line-detail", "line-data", dbEntry.DescentElevation.ToString("F0")));
            outerContainer.Children.Add(Tags.InfoLinkDivTag(settings.LineJsonDownloadUrl(dbEntry), "GeoJson",
                "line-detail", "line-data", dbEntry.DescentElevation.ToString("F0")));
        }

        //Return empty if there are no details
        return outerContainer.Children.Count(x => !x.IsEmpty()) > 1 ? outerContainer : HtmlTag.Empty();
    }

    public static string LineStatsString(LineContent line)
    {
        var lineStats =
            $"{line.LineDistance:N1} Miles, {line.ClimbElevation:N0}' Climbing, {line.DescentElevation:N0}' Descent";

        var lineDuration = LineDurationInHoursAndMinutes(line);

        if (lineDuration.totalMinutes is not null) lineStats = $"{lineStats}, {lineDuration.presentationString}";

        lineStats =
            $"{lineStats}, {line.MinimumElevation:N0}' Min Elevation, {line.MaximumElevation:N0} Max Elevation";

        return lineStats;
    }
}