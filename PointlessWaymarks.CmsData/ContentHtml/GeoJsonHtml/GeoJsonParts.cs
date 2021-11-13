using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml
{
    public static class GeoJsonParts
    {
        public static string GeoJsonDivAndScript(GeoJsonContent content)
        {
            var divScriptGuidConnector = Guid.NewGuid();

            var tag =
                $"<div id=\"GeoJson-{divScriptGuidConnector}\" class=\"leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag point-content-map\"></div>";

            var script =
                $"<script>lazyInit(document.querySelector(\"#GeoJson-{divScriptGuidConnector}\"), () => singleGeoJsonMapInit(document.querySelector(\"#GeoJson-{divScriptGuidConnector}\"), \"{content.ContentId}\"))</script>";

            return tag + script;
        }

        public static string GeoJsonDivAndScriptForDirectLocalAccess(GeoJsonContent content)
        {
            var divScriptGuidConnector = Guid.NewGuid();

            var tag =
                $"<div id=\"GeoJson-{divScriptGuidConnector}\" class=\"leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag point-content-map\"></div>";

            var script =
                $"<script>lazyInit(document.querySelector(\"#GeoJson-{divScriptGuidConnector}\"), () => singleGeoJsonMapInitFromGeoJson(document.querySelector(\"#GeoJson-{divScriptGuidConnector}\"), {GeoJsonData.GenerateGeoJson(content.GeoJson ?? string.Empty, string.Empty).Result}))</script>";

            return tag + script;
        }

        public static string GeoJsonDivAndScriptWithCaption(GeoJsonContent content)
        {
            var titleCaption =
                $"<a class=\"map-figure-title-caption\" href=\"{UserSettingsSingleton.CurrentSettings().GeoJsonPageUrl(content)}\">{content.Title}</a>";

            return $"<figure class=\"map-figure\">{GeoJsonDivAndScript(content)}{titleCaption}</figure>";
        }

        public static string GeoJsonDivAndScriptWithCaptionForDirectLocalAccess(GeoJsonContent content)
        {
            var titleCaption =
                $"<a class=\"map-figure-title-caption\" href=\"{UserSettingsSingleton.CurrentSettings().GeoJsonPageUrl(content)}\">{content.Title}</a>";

            return
                $"<figure class=\"map-figure\">{GeoJsonDivAndScriptForDirectLocalAccess(content)}{titleCaption}</figure>";
        }
    }
}