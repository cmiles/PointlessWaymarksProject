using HtmlTags;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;

public static class GeoJsonParts
{
    public static HtmlTag DownloadLinkTag(GeoJsonContent content)
    {
        if (!content.PublicDownloadLink) return HtmlTag.Empty();

        var downloadLinkContainer = new DivTag().AddClass("file-download-container");

        var settings = UserSettingsSingleton.CurrentSettings();
        var downloadLink =
            new LinkTag("Download GeoJson", settings.GeoJsonJsonDownloadUrl(content)).AddClass("file-download-link");
        downloadLinkContainer.Children.Add(downloadLink);

        return downloadLinkContainer;
    }

    public static string GeoJsonDivAndScript(GeoJsonContent content)
    {
        var divScriptGuidConnector = Guid.NewGuid();

        var tag =
            $"<div id=\"GeoJson-{divScriptGuidConnector}\" class=\"leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag point-content-map\"></div>";

        var script =
            $"<script>lazyInit(document.querySelector(\"#GeoJson-{divScriptGuidConnector}\"), () => singleGeoJsonMapInit(document.querySelector(\"#GeoJson-{divScriptGuidConnector}\"), \"{content.ContentId}\"))</script>";

        return tag + script;
    }

    public static string GeoJsonDivAndScriptWithCaption(GeoJsonContent content)
    {
        var titleCaption =
            $"<a class=\"map-figure-title-caption\" href=\"{UserSettingsSingleton.CurrentSettings().GeoJsonPageUrl(content)}\">{content.Title}</a>";

        return $"<figure class=\"map-figure\">{GeoJsonDivAndScript(content)}{titleCaption}</figure>";
    }
}