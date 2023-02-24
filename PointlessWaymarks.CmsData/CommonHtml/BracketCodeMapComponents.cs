using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentHtml.MapComponentData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class BracketCodeMapComponents
{
    public const string BracketCodeToken = "mapcomponent";

    public static string Create(MapComponent content)
    {
        return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<MapComponent>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return new List<MapComponent>();

        var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<MapComponent>();

        if (!guidList.Any()) return returnList;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopGuid in guidList)
        {
            var dbContent = context.MapComponents.FirstOrDefault(x => x.ContentId == loopGuid);
            if (dbContent == null) continue;

            progress?.Report($"MapComponent Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    public static async Task<string?> Process(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for MapComponent Link Codes");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbContent =
                await context.MapComponents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Adding mapComponent {dbContent.Title} from Code");

            toProcess = toProcess.ReplaceEach(loopMatch.bracketCodeText, () => MapParts.MapDivAndScript(dbContent));
        }

        return toProcess;
    }

    public static string? ProcessForEmail(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for MapComponent Link Codes");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        foreach (var loopMatch in resultList)
        {
            progress?.Report($"Removing mapComponent Code {loopMatch} link for email");

            toProcess = toProcess.Replace(loopMatch.bracketCodeText, string.Empty);
        }

        return toProcess;
    }
}