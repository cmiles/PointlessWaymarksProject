using System;
using System.Collections.Generic;
using HtmlTags;

namespace PointlessWaymarks.CmsData.Html.CommonHtml
{
    public static class BracketCodeSpecialPages
    {
        public static string Process(string toProcess, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Special Page Codes");

            var specialPageLookup = new List<(string bracketCode, string defaultDisplayString, string url)>
            {
                ("index", "Main", $"https:{UserSettingsSingleton.CurrentSettings().IndexPageUrl()}"),
                ("indexrss", "Main Page RSS Feed",
                    $"https:{UserSettingsSingleton.CurrentSettings().RssIndexFeedUrl()}"),
                ("filerss", "Files RSS Feed", $"https:{UserSettingsSingleton.CurrentSettings().FileRssUrl()}"),
                ("imagerss", "Images RSS Feed", $"https:{UserSettingsSingleton.CurrentSettings().ImageRssUrl()}"),
                ("linkrss", "Links RSS Feed", $"https:{UserSettingsSingleton.CurrentSettings().LinkRssUrl()}"),
                ("noterss", "Notes RSS Feed", $"https:{UserSettingsSingleton.CurrentSettings().NoteRssUrl()}"),
                ("photorss", "Photo Gallery RSS Feed",
                    $"https:{UserSettingsSingleton.CurrentSettings().PhotoRssUrl()}"),
                ("photogallerypage", "Photos",
                    $"https:{UserSettingsSingleton.CurrentSettings().CameraRollPhotoGalleryUrl()}"),
                ("searchpage", "Search", $"https:{UserSettingsSingleton.CurrentSettings().AllContentListUrl()}"),
                ("tagspage", "Tags", $"https:{UserSettingsSingleton.CurrentSettings().AllTagsListUrl()}"),
                ("linklistpage", "Links", $"https:{UserSettingsSingleton.CurrentSettings().LinkListUrl()}")
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
}