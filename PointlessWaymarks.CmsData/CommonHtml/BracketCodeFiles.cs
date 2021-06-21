using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml
{
    public static class BracketCodeFiles
    {
        public const string BracketCodeToken = "filelink";

        public static string Create(FileContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static async Task<List<FileContent>> DbContentFromBracketCodes(string toProcess,
            IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<FileContent>();

            var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
                .Select(x => x.contentGuid).Distinct().ToList();

            var returnList = new List<FileContent>();

            if (!guidList.Any()) return returnList;

            var context = await Db.Context().ConfigureAwait(false);

            foreach (var loopGuid in guidList)
            {
                var dbContent = await context.FileContents.FirstOrDefaultAsync(x => x.ContentId == loopGuid).ConfigureAwait(false);
                if (dbContent == null) continue;

                progress?.Report($"File Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        public static async Task<string> Process(string toProcess, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for File Link Codes");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = await Db.Context().ConfigureAwait(false);

            foreach (var loopMatch in resultList)
            {
                var dbContent =
                    await context.FileContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
                if (dbContent == null) continue;

                progress?.Report($"Adding file link {dbContent.Title} from Code");
                var settings = UserSettingsSingleton.CurrentSettings();

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? dbContent.Title
                            : loopMatch.displayText.Trim(), settings.FilePageUrl(dbContent), "file-page-link");

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
            }

            return toProcess;
        }
    }
}