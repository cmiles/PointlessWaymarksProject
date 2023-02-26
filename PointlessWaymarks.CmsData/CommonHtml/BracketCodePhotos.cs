using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class BracketCodePhotos
{
    public const string BracketCodeToken = "photo";

    public static string Create(PhotoContent content)
    {
        return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<PhotoContent>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return new List<PhotoContent>();

        progress?.Report("Searching for Photo Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<PhotoContent>();

        if (!resultList.Any()) return returnList;

        foreach (var loopGuid in resultList)
        {
            var context = await Db.Context().ConfigureAwait(false);

            var dbContent = context.PhotoContents.FirstOrDefault(x => x.ContentId == loopGuid);
            if (dbContent == null) continue;

            progress?.Report($"Photo Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }


    /// <summary>
    ///     Processes {{photo guid;human_identifier}} with a specified function - best use may be for easily building
    ///     library code.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="pageConversion"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    private static async Task<string?> Process(string? toProcess, Func<SinglePhotoPage, string> pageConversion,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Photo Codes");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbPhoto =
                await context.PhotoContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
            if (dbPhoto == null) continue;

            progress?.Report($"Photo Code for {dbPhoto.Title} processed");
            var singlePhotoInfo = new SinglePhotoPage(dbPhoto);

            toProcess = toProcess.Replace(loopMatch.bracketCodeText, pageConversion(singlePhotoInfo));
        }

        return toProcess;
    }

    /// <summary>
    ///     This method processes a photo code for use with the CMS Gui Previews (or for another local working
    ///     program).
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<string?> ProcessForDirectLocalAccess(string? toProcess,
        IProgress<string>? progress = null)
    {
        return await Process(toProcess, page => page.PictureInformation.LocalPictureFigureTag().ToString(),
            progress).ConfigureAwait(false);
    }

    /// <summary>
    ///     This method processes a photo code for use in Email
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<string> ProcessForEmail(string? toProcess, IProgress<string>? progress = null)
    {
        return (await Process(toProcess, page => page.PictureInformation.EmailPictureTableTag().ToString(),
            progress).ConfigureAwait(false)) ?? string.Empty;
    }

    /// <summary>
    ///     Processes {{photo guid;human_identifier}} into figure html with a link to the photo page.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<string?> ProcessToFigureWithLink(string? toProcess, IProgress<string>? progress = null)
    {
        return await Process(toProcess,
            page => page.PictureInformation.PictureFigureWithCaptionAndLinkToPicturePageTag("100vw").ToString(),
            progress).ConfigureAwait(false);
    }
}