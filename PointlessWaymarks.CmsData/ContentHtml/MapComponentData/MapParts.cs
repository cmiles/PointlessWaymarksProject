using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.MapComponentData
{
    public static class MapParts
    {
        public static string MapDivAndScript(MapComponent map)
        {
            var divScriptGuidConnector = Guid.NewGuid();

            var tag =
                $"<div id=\"MapComponent-{divScriptGuidConnector}\" class=\"leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag point-content-map\"></div>";

            var script =
                $"<script>lazyInit(document.querySelector(\"#MapComponent-{divScriptGuidConnector}\"), () => mapComponentInit(document.querySelector(\"#MapComponent-{divScriptGuidConnector}\"), \"{map.ContentId}\"));</script>";

            return tag + script;
        }
    }
}