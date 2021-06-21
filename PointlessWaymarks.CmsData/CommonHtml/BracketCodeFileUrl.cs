using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml
{
    public static class BracketCodeFileUrl
    {
        public const string BracketCodeToken = "fileurl";

        public static string Create(FileContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static async Task<string> Process(string toProcess, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            var context = await Db.Context().ConfigureAwait(false);

            foreach (var loopMatch in resultList)
            {
                var dbContent =
                    await context.FileContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
                if (dbContent == null) continue;

                progress?.Report($"Adding file url {dbContent.Title} from Code");

                var settings = UserSettingsSingleton.CurrentSettings();

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, settings.FileDownloadUrl(dbContent));
            }

            return toProcess;
        }
    }
}