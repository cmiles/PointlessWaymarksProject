using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.BracketCodes;

public static class BracketCodePointDetails
{
    public const string BracketCodeToken = "pointdetails";

    public static string Create(PointContent content)
    {
        return $"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<PointContent>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return [];

        progress?.Report("Searching for Point Detail Codes...");

        var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<PointContent>();

        if (!guidList.Any()) return returnList;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in guidList)
        {
            var dbContent = await context.PointContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch)
                .ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Point Detail Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    public static async Task<string?> Process(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Point Detail Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var pointInfo = await Db.PointContentDto(loopMatch.contentGuid);
            if (pointInfo is null) continue;

            progress?.Report($"Adding point {pointInfo.Title} from Code");

            var replacementString = (await PointParts.StandAlonePointDetailsDiv(pointInfo)).ToString();

            toProcess = toProcess.ReplaceEach(loopMatch.bracketCodeText, () => replacementString);
        }

        return toProcess;
    }
}