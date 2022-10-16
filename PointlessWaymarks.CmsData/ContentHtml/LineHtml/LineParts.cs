using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.LineHtml;

public static class LineParts
{
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

        var script =
            $"<script>lazyInit(document.querySelector(\"#Line-{divScriptGuidConnector}\"), () => singleLineMapInitFromLineData(document.querySelector(\"#Line-{divScriptGuidConnector}\"), {LineData.GenerateLineJson(content.Line ?? string.Empty, content.Title ?? string.Empty, string.Empty).Result}))</script>";

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
        if (dbEntry.RecordingStartedOn.HasValue && dbEntry.RecordingEndedOn.HasValue)
        {
            var minuteDuration =
                (int)dbEntry.RecordingEndedOn.Value.Subtract(dbEntry.RecordingStartedOn.Value).TotalMinutes;

            if (minuteDuration > 1)
            {
                var hours = minuteDuration / 60;
                var minutes = minuteDuration - hours * 60;

                if (hours == 0)
                    return (minuteDuration, $"{minutes} Minutes");
                return (minuteDuration,
                    $"{hours} Hour{(hours > 1 ? "s" : "")} {minutes} Minute{(minutes > 1 ? "s" : "")}");
            }
        }

        return (null, null);
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

        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.ClimbElevation:F0}' Climbing", "line-detail",
            "climbing", dbEntry.ClimbElevation.ToString("F0")));

        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.DescentElevation:F0}' Descent", "line-detail",
            "descent", dbEntry.DescentElevation.ToString("F0")));

        var duration = LineDurationInHoursAndMinutes(dbEntry);

        if (duration.totalMinutes is not null)
            outerContainer.Children.Add(Tags.InfoTextDivTag(duration.presentationString,
                "line-detail", "duration", $"{duration.totalMinutes}"));

        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.MinimumElevation:F0}' Min Elevation", "line-detail",
            "minimum-elevation", dbEntry.MaximumElevation.ToString("F0")));
        outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.MaximumElevation:F0}' Max Elevation", "line-detail",
            "maximum-elevation", dbEntry.MaximumElevation.ToString("F0")));


        if (dbEntry.RecordingStartedOn.HasValue)
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.RecordingStartedOn:M/d/yy h:mm tt} Start",
                "line-detail", "start-datetime", $"{dbEntry.RecordingStartedOn.Value:U}"));

        if (dbEntry.RecordingEndedOn.HasValue)
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.RecordingStartedOn:M/d/yy h:mm tt} End",
                "line-detail", "end-datetime", $"{dbEntry.RecordingEndedOn.Value:U}"));

        //Return empty if there are no details
        return outerContainer.Children.Count(x => !x.IsEmpty()) > 1 ? outerContainer : HtmlTag.Empty();
    }
}