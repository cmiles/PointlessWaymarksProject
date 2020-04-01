using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlTags;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class BracketCodeFileLink
    {
        /// <summary>
        ///     Processes {{filedownloadlink guid;human_identifier}} or {{filedownloadlink guid;text toDisplay;(optional
        ///     human_identifier}} to
        ///     a file download link. If the file content is not set to offer public downloads of the file the link is converted to
        ///     a page link.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static string FileDownloadLinkCodeProcess(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            var resultList = new List<(string wholeMatch, string siteGuidMatch, string displayText)>();

            var withTextMatch =
                new Regex(@"{{filedownloadlink (?<siteGuid>[\dA-Za-z-]*);\s*text (?<displayText>[^};]*);[^}]*}}",
                    RegexOptions.Singleline);
            var withTextMatchResult = withTextMatch.Match(toProcess);
            while (withTextMatchResult.Success)
            {
                resultList.Add((withTextMatchResult.Value, withTextMatchResult.Groups["siteGuid"].Value,
                    withTextMatchResult.Groups["displayText"].Value));
                withTextMatchResult = withTextMatchResult.NextMatch();
            }

            var regexObj = new Regex(@"{{filedownloadlink (?<siteGuid>[\dA-Za-z-]*);[^}]*}}", RegexOptions.Singleline);
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
                var dbContent = context.FileContents.FirstOrDefault(x => x.ContentId == contentGuid);
                if (dbContent == null) continue;

                progress?.Report($"Adding file download link {dbContent.Title} from Code");

                var settings = UserSettingsSingleton.CurrentSettings();

                var linkTag = new LinkTag(
                    string.IsNullOrWhiteSpace(loopMatch.displayText) ? dbContent.Title : loopMatch.displayText.Trim(),
                    dbContent.PublicDownloadLink
                        ? settings.FileDownloadUrl(dbContent)
                        : settings.FilePageUrl(dbContent), "file-download-link");

                toProcess = toProcess.Replace(loopMatch.wholeMatch, linkTag.ToString());
            }

            return toProcess;
        }

        /// <summary>
        ///     Processes {{filelink guid;human_identifier}} or {{filelink guid;text toDisplay;(optional human_identifier}} to
        ///     a file page link
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static string FileLinkCodeProcess(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for File Link Codes");

            var resultList = new List<(string wholeMatch, string siteGuidMatch, string displayText)>();

            var withTextMatch =
                new Regex(@"{{filelink (?<siteGuid>[\dA-Za-z-]*);\s*text (?<displayText>[^};]*);[^}]*}}",
                    RegexOptions.Singleline);
            var withTextMatchResult = withTextMatch.Match(toProcess);
            while (withTextMatchResult.Success)
            {
                resultList.Add((withTextMatchResult.Value, withTextMatchResult.Groups["siteGuid"].Value,
                    withTextMatchResult.Groups["displayText"].Value));
                withTextMatchResult = withTextMatchResult.NextMatch();
            }

            var regexObj = new Regex(@"{{filelink (?<siteGuid>[\dA-Za-z-]*);[^}]*}}", RegexOptions.Singleline);
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
                var dbContent = context.FileContents.FirstOrDefault(x => x.ContentId == contentGuid);
                if (dbContent == null) continue;

                progress?.Report($"Adding file link {dbContent.Title} from Code");
                var settings = UserSettingsSingleton.CurrentSettings();

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? dbContent.Title
                            : loopMatch.displayText.Trim(), settings.FilePageUrl(dbContent), "file-page-link");

                toProcess = toProcess.Replace(loopMatch.wholeMatch, linkTag.ToString());
            }

            return toProcess;
        }
    }
}