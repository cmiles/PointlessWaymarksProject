using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public class HeaderContentForPictureGallery : IHeaderContentBasedAdditions
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
                <link rel="stylesheet" href="{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}justifiedGallery.css" />
                <script src="{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}jquery-3.7.1.min.js"></script>
                <script src="{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}jquery.justifiedGallery.js"></script>
                """;
    }

    public bool IsNeeded(params string?[] stringContent)
    {
        if (stringContent.All(string.IsNullOrWhiteSpace)) return false;

        foreach (var loopString in stringContent)
        {
            if (string.IsNullOrWhiteSpace(loopString)) continue;
            if (BracketCodeCommon.ContainsPictureGalleryDependentBracketCodes(loopString)) return true;
        }

        return false;
    }

    public bool IsNeeded(IContentCommon content)
    {
        return BracketCodeCommon.ContainsPictureGalleryDependentBracketCodes(content);
    }
}