using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlTags;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class BracketCodeFileDownload
    {
        public const string BracketCodeToken = "filelink";

        public static List<FileContent> DbContentFromBracketCodes(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<FileContent>();

            var guidList = BracketCodeCommon.BracketCodeMatches(toProcess, BracketCodeToken).Select(x => x.contentGuid).Distinct().ToList();

            var resultList = new List<FileContent>();

            var context = Db.Context().Result;

            foreach (var loopGuid in guidList)
            {
                var dbContent = context.FileContents.FirstOrDefault(x => x.ContentId == loopGuid);
                if (dbContent == null) continue;

                progress?.Report($"File Code - Adding DbContent For {dbContent.Title}");

                resultList.Add(dbContent);
            }

            return resultList;
        }

        public static string FileDownloadLinkBracketCode(FileContent content)
        {
            return $@"{{{{filedownloadlink {content.ContentId}; {content.Title}}}}}";
        }

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

            var resultList = BracketCodeCommon.BracketCodeMatches(toProcess, BracketCodeToken);

            var context = Db.Context().Result;

            foreach (var loopMatch in resultList)
            {
                var dbContent = context.FileContents.FirstOrDefault(x => x.ContentId == loopMatch.contentGuid);
                if (dbContent == null) continue;

                progress?.Report($"Adding file download link {dbContent.Title} from Code");

                var settings = UserSettingsSingleton.CurrentSettings();

                var linkTag = new LinkTag(
                    string.IsNullOrWhiteSpace(loopMatch.displayText) ? dbContent.Title : loopMatch.displayText.Trim(),
                    dbContent.PublicDownloadLink
                        ? settings.FileDownloadUrl(dbContent)
                        : settings.FilePageUrl(dbContent), "file-download-link");

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
            }

            return toProcess;
        }

        public static string FileLinkBracketCode(FileContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

    }
}
