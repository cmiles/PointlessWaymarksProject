using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml
{
    public static class BracketCodePoints
    {
        public const string BracketCodeToken = "point";

        public static string Create(PointContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static async Task<List<PointContent>> DbContentFromBracketCodes(string toProcess,
            IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<PointContent>();

            progress?.Report("Searching for Point Codes...");

            var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
                .Select(x => x.contentGuid).Distinct().ToList();

            var returnList = new List<PointContent>();

            if (!guidList.Any()) return returnList;

            var context = await Db.Context().ConfigureAwait(false);

            foreach (var loopMatch in guidList)
            {
                var dbContent = await context.PointContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch).ConfigureAwait(false);
                if (dbContent == null) continue;

                progress?.Report($"Point Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        public static async Task<string> Process(string toProcess, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Point Codes...");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = await Db.Context().ConfigureAwait(false);

            foreach (var loopMatch in resultList)
            {
                var dbContent =
                    await context.PointContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
                if (dbContent?.Slug == null) continue;

                progress?.Report($"Adding point {dbContent.Title} from Code");

                toProcess = toProcess.ReplaceEach(loopMatch.bracketCodeText,
                    () => PointParts.PointDivAndScript(dbContent.Slug));
            }

            return toProcess;
        }


        public static async Task<string> ProcessForDirectLocalAccess(string toProcess,
            IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Point Codes...");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = await Db.Context().ConfigureAwait(false);

            foreach (var loopMatch in resultList)
            {
                var dbContent =
                    await context.PointContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
                if (dbContent?.Slug == null) continue;

                progress?.Report($"Adding point {dbContent.Title} from Code");

                toProcess = toProcess.ReplaceEach(loopMatch.bracketCodeText,
                    () => PointParts.PointDivAndScriptForDirectLocalAccess(dbContent.Slug));
            }

            return toProcess;
        }

        public static async Task<string> ProcessForEmail(string toProcess, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Point Codes...");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = await Db.Context().ConfigureAwait(false);

            foreach (var loopMatch in resultList)
            {
                var dbContent =
                    await context.PointContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
                if (dbContent == null) continue;

                progress?.Report($"For Email Subbing Point Map for Link {dbContent.Title} from Code");

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? dbContent.Title
                            : loopMatch.displayText.Trim(),
                        UserSettingsSingleton.CurrentSettings().PointPageUrl(dbContent), "point-page-link");

                var centeredEmailTag = Tags.EmailCenterTableTag(linkTag);

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, centeredEmailTag.ToString());
            }

            return toProcess;
        }
    }
}