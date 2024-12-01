using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.BracketCodes;

public static class BracketCodeTrailTextStats
{
    public const string BracketCodeToken = "trailtextstats";

    public static string Create(TrailContent content)
    {
        return
            $"{{{{{BracketCodeToken} {content.ContentId};text [distance] miles, [climb]' ascent, [descent]' descent;{content.Title}}}}}";
    }

    public static async Task<List<TrailContent>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return [];

        progress?.Report("Searching for Trail Text Stats Codes...");

        var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<TrailContent>();

        if (!guidList.Any()) return returnList;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in guidList)
        {
            var dbContent = await context.TrailContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch)
                .ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Trail Text Stats Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    public static async Task<string?> Process(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Trail Text Stats Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var trailContent =
                await context.TrailContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid)
                    .ConfigureAwait(false);
            if (trailContent == null) continue;

            var dbContent = await context.LineContents.FirstOrDefaultAsync(x => x.ContentId == trailContent.LineContentId)
                .ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Adding Trail Text Stats {dbContent.Title} from Code");

            string statsString;

            if (string.IsNullOrWhiteSpace(loopMatch.displayText))
            {
                statsString =
                    $"{dbContent.LineDistance} miles, {dbContent.ClimbElevation}' ascent, {dbContent.MaximumElevation}' max elevation";
            }
            else
            {
                statsString = loopMatch.displayText.Trim();
                statsString = statsString.Replace("[distance]", $"{dbContent.LineDistance:N1}");
                statsString = statsString.Replace("[climb]", $"{dbContent.ClimbElevation:N0}");
                statsString = statsString.Replace("[descent]", $"{dbContent.DescentElevation:N0}");
                statsString = statsString.Replace("[maxelevation]", $"{dbContent.MaximumElevation:N0}");
                statsString = statsString.Replace("[minelevation]", $"{dbContent.MinimumElevation:N0}");
            }

            toProcess = toProcess.Replace(loopMatch.bracketCodeText, statsString);
        }

        return toProcess;
    }
}