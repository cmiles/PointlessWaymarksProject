using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public class HeaderContentForSpatialContent : IHeaderContentBasedAdditions
{
    public string HeaderAdditions(IContentCommon content)
    {
        return IsNeeded(content) ? HeaderAdditions() : string.Empty;
    }

    public string HeaderAdditions(params string?[] stringContent)
    {
        return IsNeeded(stringContent) ? HeaderAdditions() : string.Empty;
    }

    public string HeaderAdditions()
    {
        return $"""
                <link rel="stylesheet" href="{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}leaflet.css" />
                <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
                <script src="{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}leaflet.js"></script>
                <link rel="stylesheet" href="{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}leaflet-gesture-handling.min.css" type="text/css">
                <script src="{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}leaflet-gesture-handling.min.js"></script>
                <link rel="stylesheet" href="{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}L.Control.Locate.min.css" type="text/css">
                <script src="{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}L.Control.Locate.min.js"></script>
                <script src="{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}pointless-waymarks-spatial-common.js"></script>
                """;
    }


    public bool IsNeeded(params string?[] stringContent)
    {
        if (stringContent.All(string.IsNullOrWhiteSpace)) return false;

        foreach (var loopString in stringContent)
        {
            if (string.IsNullOrWhiteSpace(loopString)) continue;
            if (BracketCodeCommon.ContainsSpatialScriptDependentBracketCodes(loopString)) return true;
        }

        return false;
    }

    public bool IsNeeded(IContentCommon content)
    {
        return BracketCodeCommon.ContainsSpatialScriptDependentBracketCodes(content) || (content.GetType() == typeof(PointContentDto) || content.GetType() == typeof(GeoJsonContent) ||
            content.GetType() == typeof(LineContent));
    }
}