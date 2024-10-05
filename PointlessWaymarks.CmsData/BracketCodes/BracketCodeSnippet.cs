using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.BracketCodes;

public static class BracketCodeSnippet
{
    public const string BracketCodeToken = "snippet";

    public static string Create(Snippet content)
    {
        return $"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<Snippet>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return [];

        progress?.Report("Searching for Snippet Codes...");

        var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<Snippet>();

        if (!guidList.Any()) return returnList;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in guidList)
        {
            var dbContent = context.Snippets.FirstOrDefault(x => x.ContentId == loopMatch);
            if (dbContent == null) continue;

            progress?.Report($"Snippet Code - Adding Db Snippet For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    public static async Task<string?> Process(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Snippet Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbContent =
                await context.Snippets.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report("Adding Snippet from Code");

            toProcess = toProcess.Replace(loopMatch.bracketCodeText, dbContent.BodyContent);
        }

        return toProcess;
    }

    public static async Task<(Guid snippetContentId, List<Guid> relatedSnippetContentId)> SnippetContentIdRelatedSnippetContentIds(Guid contentId, IProgress<string>? progress = null)
    {
        var context = await Db.Context().ConfigureAwait(false);

        var initialSnippet = await context.Snippets.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);

        var returnInformation = (contentId, new List<Guid>());

        if (initialSnippet == null) return returnInformation;

        var currentBody = initialSnippet.BodyContent;

        var embeddedContentIds = BracketCodeCommon.ContentBracketCodeMatches(currentBody, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var cycleDetected = false;

        while (embeddedContentIds.Any() && !cycleDetected)
        {
            if (embeddedContentIds.Any(x => returnInformation.Item2.Contains(x)) || embeddedContentIds.Any(x => x.Equals(returnInformation.contentId)))
            {
                cycleDetected = true;
                continue;
            }

            returnInformation.Item2.AddRange(embeddedContentIds);

            currentBody = await Process(currentBody, progress);

            embeddedContentIds = BracketCodeCommon.ContentBracketCodeMatches(currentBody, BracketCodeToken)
                .Select(x => x.contentGuid).Distinct().ToList();
        }

        return returnInformation;
    }
}