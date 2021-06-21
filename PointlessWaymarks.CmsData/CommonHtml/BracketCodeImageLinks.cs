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
    public static class BracketCodeImageLinks
    {
        public const string BracketCodeToken = "imagelink";

        public static string Create(ImageContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static async Task<List<ImageContent>> DbContentFromBracketCodes(string toProcess,
            IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<ImageContent>();

            var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
                .Select(x => x.contentGuid).Distinct().ToList();

            var returnList = new List<ImageContent>();

            if (!guidList.Any()) return returnList;

            var context = await Db.Context().ConfigureAwait(false);

            foreach (var loopGuid in guidList)
            {
                var dbContent = await context.ImageContents.FirstOrDefaultAsync(x => x.ContentId == loopGuid).ConfigureAwait(false);
                if (dbContent == null) continue;

                progress?.Report($"Image Link Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        public static async Task<string> Process(string toProcess, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Image Link Codes");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = await Db.Context().ConfigureAwait(false);

            foreach (var loopMatch in resultList)
            {
                var dbContent =
                    await context.ImageContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
                if (dbContent == null) continue;

                progress?.Report($"Adding image link {dbContent.Title} from Code");
                var settings = UserSettingsSingleton.CurrentSettings();

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? dbContent.Title
                            : loopMatch.displayText.Trim(), settings.ImagePageUrl(dbContent), "image-page-link");

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
            }

            return toProcess;
        }
    }
}