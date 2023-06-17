using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class CodeHighlightingScripts
{
    public static string IncludeIfNeeded(IContentCommon content)
    {
        return IncludeIfNeeded(IsNeeded(content.BodyContent));
    }

    public static string IncludeIfNeeded(bool isNeeded)
    {
        return isNeeded ? ScriptsAndLinks() : string.Empty;
    }

    public static bool IsNeeded(string? stringContent)
    {
        if (string.IsNullOrWhiteSpace(stringContent)) return false;
        return stringContent.Contains("```");
    }

    public static string ScriptsAndLinks()
    {
        return
            """
            <link rel="stylesheet" href="//cdn.jsdelivr.net/gh/highlightjs/cdn-release@11.7.0/build/styles/default.min.css">
            <script src="//cdn.jsdelivr.net/gh/highlightjs/cdn-release@11.7.0/build/highlight.min.js"></script>
            <script>hljs.highlightAll();</script>
            """;
    }
}