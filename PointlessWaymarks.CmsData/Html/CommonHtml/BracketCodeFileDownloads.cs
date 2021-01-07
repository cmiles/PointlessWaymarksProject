using System;
using System.Collections.Generic;
using System.Linq;
using HtmlTags;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Html.CommonHtml
{
    public static class BracketCodeFileDownloads
    {
        public const string BracketCodeToken = "filedownloadlink";

        public static string Create(FileContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static List<FileContent> DbContentFromBracketCodes(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<FileContent>();

            var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
                .Select(x => x.contentGuid).Distinct().ToList();

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

        public static string FileLinkBracketCode(FileContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static string Process(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

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
    }
}