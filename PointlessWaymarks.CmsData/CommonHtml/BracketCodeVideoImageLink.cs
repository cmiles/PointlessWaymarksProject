using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class BracketCodeVideoImage
{
    public const string BracketCodeToken = "videoimagelink";

    public static string Create(VideoContent content)
    {
        return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<VideoContent>> DbContentFromBracketCodes(string toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return new List<VideoContent>();

        progress?.Report("Searching for Video Content Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<VideoContent>();

        if (!resultList.Any()) return returnList;

        foreach (var loopGuid in resultList)
        {
            var context = await Db.Context().ConfigureAwait(false);

            var dbContent = await context.VideoContents.FirstOrDefaultAsync(x => x.ContentId == loopGuid)
                .ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Video Image Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    /// <summary>
    ///     Processes {{video guid;human_identifier}} with a specified function - best use may be for easily building
    ///     library code.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="pageConversion"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    private static async Task<string> Process(string toProcess,
        Func<(PictureSiteInformation pictureInfo, string linkUrl), string> pageConversion,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Video Image Link Codes");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbVideo = await context.VideoContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid)
                .ConfigureAwait(false);

            if (dbVideo == null) continue;

            if (dbVideo.MainPicture == null)
            {
                progress?.Report(
                    $"Video Image Link without Main Image - converting to videolink - Video: {dbVideo.Title}");

                var newBracketCodeText = loopMatch.bracketCodeText.Replace("videoimagelink", "videolink",
                    StringComparison.OrdinalIgnoreCase);

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, newBracketCodeText);

                await BracketCodeVideos.Process(toProcess).ConfigureAwait(false);

                continue;
            }

            var dbPicture = new PictureSiteInformation(dbVideo.MainPicture.Value);

            if (dbPicture.Pictures == null)
            {
                progress?.Report(
                    $"Video Image Link with Null PictureSiteInformation - converting to videolink - Video: {dbVideo.Title}");

                var newBracketCodeText = loopMatch.bracketCodeText.Replace("videoimagelink", "videolink",
                    StringComparison.OrdinalIgnoreCase);

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, newBracketCodeText);

                await BracketCodeVideos.Process(toProcess).ConfigureAwait(false);

                continue;
            }

            toProcess = toProcess.Replace(loopMatch.bracketCodeText,
                pageConversion((dbPicture, UserSettingsSingleton.CurrentSettings().VideoPageUrl(dbVideo))));

            progress?.Report($"Video Image Link {dbVideo.Title} processed");
        }

        return toProcess;
    }

    /// <summary>
    ///     This method processes a videoimagelink code for use with the CMS Gui Previews (or for another local working
    ///     program).
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<string> ProcessForDirectLocalAccess(string toProcess,
        IProgress<string>? progress = null)
    {
        return await Process(toProcess, pictureInfo => pictureInfo.pictureInfo.LocalPictureFigureTag().ToString(),
            progress).ConfigureAwait(false);
    }

    /// <summary>
    ///     This method processes a videoimagelink code for use in email.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<string> ProcessForEmail(string toProcess, IProgress<string>? progress = null)
    {
        return await Process(toProcess, pictureInfo => pictureInfo.pictureInfo.EmailPictureTableTag().ToString(),
            progress).ConfigureAwait(false);
    }

    /// <summary>
    ///     Processes {{image guid;human_identifier}} into figure html with a link to the video page.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<string> ProcessToFigureWithLink(string toProcess, IProgress<string>? progress = null)
    {
        return await Process(toProcess,
            pictureInfo => pictureInfo.pictureInfo.PictureFigureWithCaptionAndLinkTag("100vw", pictureInfo.linkUrl)
                .ToString(), progress).ConfigureAwait(false);
    }
}