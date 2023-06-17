using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class SpatialScripts
{
    public static string IncludeIfNeeded(IContentCommon content)
    {
        return IncludeIfNeeded(BracketCodeCommon.ContainsSpatialScriptDependentBracketCodes(content));
    }

    public static string IncludeIfNeeded(bool isNeeded)
    {
        return isNeeded ? ScriptsAndLinks() : string.Empty;
    }

    public static bool IsNeeded(string? contentString)
    {
        if (string.IsNullOrWhiteSpace(contentString)) return false;
        return BracketCodeCommon.ContainsSpatialScriptDependentBracketCodes(contentString);
    }

    public static string ScriptsAndLinks()
    {
        return
            $"<link rel=\"stylesheet\" href=\"{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}leaflet.css\" />\r\n" +
            $"<script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>\r\n" +
            $"<script src=\"{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}leaflet.js\"></script>\r\n" +
            $"<link rel=\"stylesheet\" href=\"{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}leaflet-gesture-handling.min.css\" type=\"text/css\">\r\n" +
            $"<script src=\"{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}leaflet-gesture-handling.min.js\"></script>\r\n" +
            $"<link rel=\"stylesheet\" href=\"{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}L.Control.Locate.min.css\" type=\"text/css\">\r\n" +
            $"<script src=\"{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}L.Control.Locate.min.js\"></script>\r\n" +
            $"<script src=\"{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}pointless-waymarks-spatial-common.js\"></script>";
    }
}