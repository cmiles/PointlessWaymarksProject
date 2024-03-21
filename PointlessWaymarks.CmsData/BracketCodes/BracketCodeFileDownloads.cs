using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.BracketCodes;

public static class BracketCodeFileDownloads
{
    public const string BracketCodeToken = "filedownloadlink";

    public static string Create(FileContent content)
    {
        return $"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<FileContent>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return [];

        var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var resultList = new List<FileContent>();

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopGuid in guidList)
        {
            var dbContent = await context.FileContents.FirstOrDefaultAsync(x => x.ContentId == loopGuid)
                .ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"File Code - Adding DbContent For {dbContent.Title}");

            resultList.Add(dbContent);
        }

        return resultList;
    }

    public static async Task<string?> Process(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbContent =
                await context.FileContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid)
                    .ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Adding file download link {dbContent.Title} from Code");

            var settings = UserSettingsSingleton.CurrentSettings();

            var linkTag = new LinkTag(
                string.IsNullOrWhiteSpace(loopMatch.displayText) ? dbContent.Title : loopMatch.displayText.Trim(),
                dbContent.PublicDownloadLink
                    ? settings.FileDownloadUrl(dbContent)
                    : settings.FilePageUrl(dbContent), "file-download-link");

            toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
        }

        return toProcess;
    }
}