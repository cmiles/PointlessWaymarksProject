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
    public static class BracketCodePointLinks
    {
        public const string BracketCodeToken = "pointlink";

        public static string Create(PointContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static async Task<List<PointContent>> DbContentFromBracketCodes(string toProcess,
            IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<PointContent>();

            progress?.Report("Searching for Point Link Codes...");

            var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
                .Select(x => x.contentGuid).Distinct().ToList();

            var returnList = new List<PointContent>();

            if (!guidList.Any()) return returnList;

            var context = await Db.Context();

            foreach (var loopMatch in guidList)
            {
                var dbContent = await context.PointContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch);
                if (dbContent == null) continue;

                progress?.Report($"Point Link Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        public static async Task<string> Process(string toProcess, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Point Link Codes...");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = await Db.Context();

            foreach (var loopMatch in resultList)
            {
                var dbContent =
                    await context.PointContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid);
                if (dbContent == null) continue;

                progress?.Report($"Adding Point Link {dbContent.Title} from Code");

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? dbContent.Title
                            : loopMatch.displayText.Trim(),
                        UserSettingsSingleton.CurrentSettings().PointPageUrl(dbContent), "point-page-link");

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
            }

            return toProcess;
        }
    }
}