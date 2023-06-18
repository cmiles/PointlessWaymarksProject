using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public class HeaderContentForCodeHighlighting : IHeaderContentBasedAdditions
{
    public string HeaderAdditions(IContentCommon content)
    {
        return IsNeeded(content.BodyContent) ? HeaderAdditions() : string.Empty;
    }

    public string HeaderAdditions(params string?[] stringContent)
    {
        return IsNeeded(stringContent) ? HeaderAdditions() : string.Empty;
    }

    public string HeaderAdditions()
    {
        return
            """
            <link rel="stylesheet" href="//cdn.jsdelivr.net/gh/highlightjs/cdn-release@11.7.0/build/styles/default.min.css">
            <script src="//cdn.jsdelivr.net/gh/highlightjs/cdn-release@11.7.0/build/highlight.min.js"></script>
            <script>hljs.highlightAll();</script>
            """;
    }

    public bool IsNeeded(params string?[] stringContent)
    {
        if (stringContent.All(string.IsNullOrWhiteSpace)) return false;

        foreach (var loopString in stringContent)
        {
            if (string.IsNullOrWhiteSpace(loopString)) continue;
            if (loopString.Contains("```")) return true;
        }

        return false;
    }

    public bool IsNeeded(IContentCommon content)
    {
        return IsNeeded(content.BodyContent);
    }
}