using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class BracketCodeLineStats
{
    public const string BracketCodeToken = "linestats";

    public static string Create(LineContent content)
    {
        return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<LineContent>> DbContentFromBracketCodes(string toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return new List<LineContent>();

        progress?.Report("Searching for Line Stats Codes...");

        var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<LineContent>();

        if (!guidList.Any()) return returnList;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in guidList)
        {
            var dbContent = await context.LineContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Line Stats Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    public static async Task<string> Process(string toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Line Stats Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbContent =
                await context.LineContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Adding Line Stats {dbContent.Title} from Code");

            toProcess = toProcess.ReplaceEach(loopMatch.bracketCodeText,
                () => LineParts.LineStatisticsGeneralDisplayDiv(dbContent).ToString());
        }

        return toProcess;
    }

}