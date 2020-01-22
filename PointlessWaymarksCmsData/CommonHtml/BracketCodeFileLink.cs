using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlTags;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public class BracketCodeFileLink
    {
        /// <summary>
        ///     Processes {{image guid;human_identifier}} with a specified function - best use may be for easily building
        ///     library code.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="pageConversion"></param>
        /// <returns></returns>
        public static string FileLinkCodeProcess(string toProcess)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            var resultList = new List<(string wholeMatch, string siteGuidMatch)>();
            var regexObj = new Regex(@"{{filelink (?<siteGuid>[\dA-Za-z-]*);[^}]*}}", RegexOptions.Singleline);
            var matchResult = regexObj.Match(toProcess);
            while (matchResult.Success)
            {
                resultList.Add((matchResult.Value, matchResult.Groups["siteGuid"].Value));
                matchResult = matchResult.NextMatch();

                var context = Db.Context().Result;

                foreach (var loopString in resultList)
                {
                    var contentGuid = Guid.Parse(loopString.siteGuidMatch);
                    var dbContent = context.FileContents.FirstOrDefault(x => x.ContentId == contentGuid);
                    if (dbContent == null) continue;

                    var settings = UserSettingsSingleton.CurrentSettings();

                    var linkTag = new LinkTag(dbContent.Title, settings.FilePageUrl(dbContent), "file-page-link");

                    toProcess = toProcess.Replace(loopString.wholeMatch, linkTag.ToString());
                }
            }

            return toProcess;
        }
    }
}