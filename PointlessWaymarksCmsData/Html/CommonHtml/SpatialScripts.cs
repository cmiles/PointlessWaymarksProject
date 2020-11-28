using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Html.CommonHtml
{
    public static class SpatialScripts
    {
        public static string IncludeIfNeeded(string content)
        {
            return BracketCodeCommon.ContainsSpatialBracketCodes(content) ? ScriptsAndLinks() : string.Empty;
        }

        public static string IncludeIfNeeded(IContentCommon content)
        {
            return BracketCodeCommon.ContainsSpatialBracketCodes(content) ? ScriptsAndLinks() : string.Empty;
        }

        public static string IncludeIfNeeded(bool isNeeded)
        {
            return isNeeded ? ScriptsAndLinks() : string.Empty;
        }

        public static string ScriptsAndLinks()
        {
            return
                $"<link rel=\"stylesheet\" href=\"{ UserSettingsSingleton.CurrentSettings().SiteResourcesUrl() }leaflet.css\" />\r\n" +
                $"<script src=\"{ UserSettingsSingleton.CurrentSettings().SiteResourcesUrl() }leaflet.js\"></script>\r\n" +
                $"<link rel=\"stylesheet\" href=\"{ UserSettingsSingleton.CurrentSettings().SiteResourcesUrl() }leaflet-gesture-handling.min.css\" type=\"text/css\">\r\n" +
                $"<script src=\"{ UserSettingsSingleton.CurrentSettings().SiteResourcesUrl() }leaflet-gesture-handling.min.js\"></script>\r\n" +
                $"<script src=\"{ UserSettingsSingleton.CurrentSettings().SiteResourcesUrl() }pointless-waymarks-spatial-common.js\"></script>";
        }
    }
}