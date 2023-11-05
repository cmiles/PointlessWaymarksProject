using HtmlTags;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class BracketCodeSpecialPages
{
    public static string Process(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Special Page Codes");

        var specialPageLookup = new List<(string bracketCode, string defaultDisplayString, string url)>
        {
            ("index", "Main", $"{UserSettingsSingleton.CurrentSettings().IndexPageUrl()}"),
            // ReSharper disable StringLiteralTypo
            ("indexrss", "Main Page RSS Feed",
                $"{UserSettingsSingleton.CurrentSettings().RssIndexFeedUrl()}"),
            ("filerss", "Files RSS Feed", $"{UserSettingsSingleton.CurrentSettings().FileRssUrl()}"),
            ("imagerss", "Images RSS Feed", $"{UserSettingsSingleton.CurrentSettings().ImageRssUrl()}"),
            ("linkrss", "Links RSS Feed", $"{UserSettingsSingleton.CurrentSettings().LinkRssUrl()}"),
            ("noterss", "Notes RSS Feed", $"{UserSettingsSingleton.CurrentSettings().NoteRssUrl()}"),
            ("photorss", "Photo Gallery RSS Feed",
                $"{UserSettingsSingleton.CurrentSettings().PhotoRssUrl()}"),
            ("photogallerypage", "Photos",
                $"{UserSettingsSingleton.CurrentSettings().CameraRollGalleryUrl()}"),
            ("searchpage", "Search", $"{UserSettingsSingleton.CurrentSettings().AllContentListUrl()}"),
            ("tagspage", "Tags", $"{UserSettingsSingleton.CurrentSettings().AllTagsListUrl()}"),
            ("linklistpage", "Links", $"{UserSettingsSingleton.CurrentSettings().LinkListUrl()}")
            // ReSharper restore StringLiteralTypo
        };

        foreach (var loopLookups in specialPageLookup)
        {
            var matches = BracketCodeCommon.SpecialPageBracketCodeMatches(toProcess, loopLookups.bracketCode);

            foreach (var loopMatch in matches)
            {
                progress?.Report($"Adding Special Page {loopLookups.bracketCode} from Code");

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? loopLookups.defaultDisplayString
                            : loopMatch.displayText.Trim(), loopLookups.url, "special-page-link");

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
            }
        }

        return toProcess;
    }
}