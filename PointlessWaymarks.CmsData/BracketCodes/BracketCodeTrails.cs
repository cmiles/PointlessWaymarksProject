using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.BracketCodes;

public static class BracketCodeTrails
{
    public const string BracketCodeToken = "traillink";

    public static string Create(TrailContent content)
    {
        return $"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<TrailContent>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return [];

        progress?.Report("Searching for Trail Codes...");

        var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<TrailContent>();

        if (!guidList.Any()) return returnList;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in guidList)
        {
            var dbContent = context.TrailContents.FirstOrDefault(x => x.ContentId == loopMatch);
            if (dbContent == null) continue;

            progress?.Report($"Trail Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    public static async Task<string?> Process(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Trail Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbContent =
                await context.TrailContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid)
                    .ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Adding trail link {dbContent.Title} from Code");

            var linkTag =
                new LinkTag(
                    string.IsNullOrWhiteSpace(loopMatch.displayText)
                        ? dbContent.Title
                        : loopMatch.displayText.Trim(),
                    UserSettingsSingleton.CurrentSettings().TrailPageUrl(dbContent), "trail-page-link");

            toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
        }

        return toProcess;
    }
}