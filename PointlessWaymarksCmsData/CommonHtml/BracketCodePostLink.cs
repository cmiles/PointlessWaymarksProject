using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlTags;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public class BracketCodePostLink
    {
        /// <summary>
        ///     Processes {{postlink guid;human_identifier}} or {{postlink guid;text toDisplay;(optional human_identifier}} to
        ///     a link
        /// </summary>
        /// <param name="toProcess"></param>
        /// <returns></returns>
        public static string PostLinkCodeProcess(string toProcess)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            var resultList = new List<(string wholeMatch, string siteGuidMatch, string displayText)>();

            var withTextMatch = new Regex(@"{{postlink (?<siteGuid>[\dA-Za-z-]*);text (?<displayText>[^};]*);[^}]*}}",
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

                var settings = UserSettingsSingleton.CurrentSettings();

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? dbContent.Title
                            : loopMatch.displayText.Trim(), settings.PostPageUrl(dbContent), "post-page-link");

                toProcess = toProcess.Replace(loopMatch.wholeMatch, linkTag.ToString());
            }

            return toProcess;
        }
    }
}