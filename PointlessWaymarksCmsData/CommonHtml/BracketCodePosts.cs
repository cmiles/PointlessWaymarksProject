using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlTags;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class BracketCodePosts
    {
        public static List<PostContent> DbContentFromBracketCodes(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<PostContent>();

            progress?.Report("Searching for Post Codes...");

            var guidList = new List<Guid>();

            var withTextMatch =
                new Regex(@"{{postlink (?<siteGuid>[\dA-Za-z-]*);\s*text (?<displayText>[^};]*);[^}]*}}",
                    RegexOptions.Singleline);
            var withTextMatchResult = withTextMatch.Match(toProcess);
            while (withTextMatchResult.Success)
            {
                guidList.Add(Guid.Parse(withTextMatchResult.Groups["siteGuid"].Value));
                withTextMatchResult = withTextMatchResult.NextMatch();
            }

            var regexObj = new Regex(@"{{postlink (?<siteGuid>[\dA-Za-z-]*);[^}]*}}", RegexOptions.Singleline);
            var matchResult = regexObj.Match(toProcess);
            while (matchResult.Success)
            {
                guidList.Add(Guid.Parse(matchResult.Groups["siteGuid"].Value));
                matchResult = matchResult.NextMatch();
            }

            guidList = guidList.Distinct().ToList();

            var context = Db.Context().Result;

            var returnList = new List<PostContent>();

            foreach (var loopMatch in guidList)
            {
                var dbContent = context.PostContents.FirstOrDefault(x => x.ContentId == loopMatch);
                if (dbContent == null) continue;

                progress?.Report($"Photo Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        public static string NoteLinkBracketCode(PostContent content)
        {
            return $@"{{{{postlink {content.ContentId}; {content.Title}}}}}";
        }

        /// <summary>
        ///     Processes {{postlink guid;human_identifier}} or {{postlink guid;text toDisplay;(optional human_identifier}} to
        ///     a link
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static string PostLinkCodeProcess(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Post Codes...");

            var resultList = new List<(string wholeMatch, string siteGuidMatch, string displayText)>();

            var withTextMatch =
                new Regex(@"{{postlink (?<siteGuid>[\dA-Za-z-]*);\s*text (?<displayText>[^};]*);[^}]*}}",
                    RegexOptions.Singleline);
            var withTextMatchResult = withTextMatch.Match(toProcess);
            while (withTextMatchResult.Success)
            {
                resultList.Add((withTextMatchResult.Value, withTextMatchResult.Groups["siteGuid"].Value,
                    withTextMatchResult.Groups["displayText"].Value));
                withTextMatchResult = withTextMatchResult.NextMatch();
            }

            var regexObj = new Regex(@"{{postlink (?<siteGuid>[\dA-Za-z-]*);[^}]*}}", RegexOptions.Singleline);
            var matchResult = regexObj.Match(toProcess);
            while (matchResult.Success)
            {
                resultList.Add((matchResult.Value, matchResult.Groups["siteGuid"].Value, string.Empty));
                matchResult = matchResult.NextMatch();
            }

            var context = Db.Context().Result;

            foreach (var loopMatch in resultList)
            {
                var contentGuid = Guid.Parse(loopMatch.siteGuidMatch);
                var dbContent = context.PostContents.FirstOrDefault(x => x.ContentId == contentGuid);
                if (dbContent == null) continue;

                progress?.Report($"Adding post link {dbContent.Title} from Code");

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? dbContent.Title
                            : loopMatch.displayText.Trim(),
                        UserSettingsSingleton.CurrentSettings().PostPageUrl(dbContent), "post-page-link");

                toProcess = toProcess.Replace(loopMatch.wholeMatch, linkTag.ToString());
            }

            return toProcess;
        }
    }
}