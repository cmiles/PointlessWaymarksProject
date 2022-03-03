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
            $"<script>lazyInit(document.querySelector(\"#Line-{divScriptGuidConnector}\"), () => singleLineMapInitFromLineData(document.querySelector(\"#Line-{divScriptGuidConnector}\"), {LineData.GenerateLineJson(content.Line ?? string.Empty, string.Empty).Result}))</script>";

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

    public static HtmlTag LineStatisticsDiv(LineContent dbEntry)
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

        if (dbEntry.RecordingStartedOn.HasValue)
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.RecordingStartedOn:M/d/yy h:mm:ss tt} Start",
                "line-detail", "start-datetime", $"{dbEntry.RecordingStartedOn.Value:U}"));

        if (dbEntry.RecordingEndedOn.HasValue)
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.RecordingStartedOn:M/d/yy h:mm:ss tt} End",
                "line-detail", "end-datetime", $"{dbEntry.RecordingEndedOn.Value:U}"));

        //Return empty if there are no details
        return outerContainer.Children.Count(x => !x.IsEmpty()) > 1 ? outerContainer : HtmlTag.Empty();
    }
}