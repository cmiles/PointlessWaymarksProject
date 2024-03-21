using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.BracketCodes;

public static class BracketCodeLineElevationCharts
{
    public const string BracketCodeToken = "lineelevationchart";

    public static string Create(LineContent content)
    {
        return $"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<LineContent>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return [];

        progress?.Report("Searching for Line Elevation Chart Codes...");

        var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<LineContent>();

        if (!guidList.Any()) return returnList;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in guidList)
        {
            var dbContent = await context.LineContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Line Elevation Chart Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    public static async Task<string?> Process(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Line Elevation Chart Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbContent =
                await context.LineContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Adding Line Elevation Chart {dbContent.Title} from Code");

            toProcess = toProcess.ReplaceEach(loopMatch.bracketCodeText,
                () => LineParts.LineElevationChartDivAndScript(dbContent));
        }

        return toProcess;
    }

    public static string ProcessForEmail(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Line Elevation Chart Link Codes");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        foreach (var loopMatch in resultList)
        {
            progress?.Report($"Removing lineelevationchart Code {loopMatch} link for email");

            toProcess = toProcess.Replace(loopMatch.bracketCodeText, string.Empty);
        }

        return toProcess;
    }

}