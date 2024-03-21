using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.BracketCodes;

public static class BracketCodeFileImageLink
{
    public const string BracketCodeToken = "fileimagelink";

    public static string Create(FileContent content)
    {
        return $"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<FileContent>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return [];

        progress?.Report("Searching for File Content Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<FileContent>();

        if (!resultList.Any()) return returnList;

        foreach (var loopGuid in resultList)
        {
            var context = await Db.Context().ConfigureAwait(false);

            var dbContent = await context.FileContents.FirstOrDefaultAsync(x => x.ContentId == loopGuid)
                .ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"File Image Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    /// <summary>
    ///     Processes {{file guid;human_identifier}} with a specified function - best use may be for easily building
    ///     library code.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="pageConversion"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    private static async Task<string?> Process(string? toProcess,
        Func<(PictureSiteInformation pictureInfo, string? linkUrl), string> pageConversion,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for File Image Link Codes");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbFile = await context.FileContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid)
                .ConfigureAwait(false);

            if (dbFile == null) continue;
            if (dbFile.MainPicture == null)
            {
                progress?.Report(
                    $"File Image Link without Main Image - converting to filelink - File: {dbFile.Title}");

                var newBracketCodeText =
                    loopMatch.bracketCodeText.Replace("fileimagelink", "filelink", StringComparison.OrdinalIgnoreCase);

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, newBracketCodeText);

                await BracketCodeFiles.Process(toProcess).ConfigureAwait(false);

                continue;
            }

            var dbPicture = new PictureSiteInformation(dbFile.MainPicture.Value);

            if (dbPicture.Pictures == null)
            {
                progress?.Report(
                    $"File Image Link with Null PictureSiteInformation - converting to filelink - File: {dbFile.Title}");

                var newBracketCodeText =
                    loopMatch.bracketCodeText.Replace("fileimagelink", "filelink", StringComparison.OrdinalIgnoreCase);

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, newBracketCodeText);

                await BracketCodeFiles.Process(toProcess).ConfigureAwait(false);

                continue;
            }

            var conversion = pageConversion((dbPicture, UserSettingsSingleton.CurrentSettings().FilePageUrl(dbFile)));

            if (string.IsNullOrWhiteSpace(conversion))
            {
                progress?.Report(
                    $"File Image Link converted to Null/Empty - converting to filelink - File: {dbFile.Title}");

                var newBracketCodeText =
                    loopMatch.bracketCodeText.Replace("fileimagelink", "filelink", StringComparison.OrdinalIgnoreCase);

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, newBracketCodeText);

                await BracketCodeFiles.Process(toProcess).ConfigureAwait(false);

                continue;
            }

            toProcess = toProcess.Replace(loopMatch.bracketCodeText, conversion);

            progress?.Report($"File Image Link {dbFile.Title} processed");
        }

        return toProcess;
    }

    /// <summary>
    ///     This method processes a fileimagelink code for use in email.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<string> ProcessForEmail(string? toProcess, IProgress<string>? progress = null)
    {
        return await Process(toProcess,
            pictureInfo => pictureInfo.pictureInfo.EmailPictureTableTag().ToString() ?? string.Empty,
            progress).ConfigureAwait(false) ?? string.Empty;
    }

    /// <summary>
    ///     Processes {{image guid;human_identifier}} into figure html with a link to the file page.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<string?> ProcessToFigureWithLink(string? toProcess, IProgress<string>? progress = null)
    {
        return await Process(toProcess,
            pictureInfo => pictureInfo.pictureInfo
                .PictureFigureWithCaptionAndLinkTag("100vw", pictureInfo.linkUrl ?? string.Empty)
                .ToString() ?? string.Empty, progress).ConfigureAwait(false);
    }
}