﻿using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentHtml.ImageHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class BracketCodeImages
{
    public const string BracketCodeToken = "image";

    public static string Create(ImageContent content)
    {
        return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<ImageContent>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return new List<ImageContent>();

        progress?.Report("Searching for Image Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<ImageContent>();

        if (!resultList.Any()) return returnList;

        foreach (var loopGuid in resultList)
        {
            var context = await Db.Context().ConfigureAwait(false);

            var dbContent = await context.ImageContents.FirstOrDefaultAsync(x => x.ContentId == loopGuid).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Image Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    /// <summary>
    ///     Processes {{image guid;human_identifier}} with a specified function - best use may be for easily building
    ///     library code.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="pageConversion"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    private static async Task<string?> Process(string? toProcess, Func<SingleImagePage, string> pageConversion,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Image Codes");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbImage =
                await context.ImageContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
            if (dbImage == null) continue;

            progress?.Report($"Image Code for {dbImage.Title} processed");
            var singleImageInfo = new SingleImagePage(dbImage);

            toProcess = toProcess.Replace(loopMatch.bracketCodeText, pageConversion(singleImageInfo));
        }

        return toProcess;
    }

    /// <summary>
    ///     This method processes a image code for use with the CMS Gui Previews (or for another local working
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
    ///     This method processes a image code for use in email.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<string> ProcessForEmail(string? toProcess, IProgress<string>? progress = null)
    {
        return await (Process(toProcess, page => page.PictureInformation.EmailPictureTableTag().ToString(),
            progress).ConfigureAwait(false)) ?? string.Empty;
    }

    /// <summary>
    ///     Processes {{image guid;human_identifier}} into figure html with a link to the image page.
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