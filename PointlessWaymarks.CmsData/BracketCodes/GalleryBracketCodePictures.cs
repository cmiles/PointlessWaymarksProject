using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.BracketCodes;

public static class GalleryBracketCodePictures
{
    public const string BracketCodeToken = "picturegallery";

    public static string Create(string innerBracketCodes)
    {
        return $"""

                [[{BracketCodeToken}
                {innerBracketCodes}
                ]]
                """;
    }

    public static async Task<List<dynamic>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return [];

        progress?.Report("Searching for Photo Gallery Codes...");

        var resultList = BracketCodeCommon.ContentGalleryBracketCodeMatches(toProcess, BracketCodeToken)
            .SelectMany(x => x.contentGuid).ToList();

        var returnList = new List<dynamic>();

        if (!resultList.Any()) return returnList;

        var db = await Db.Context().ConfigureAwait(false);

        var dbContent = await db.ContentFromContentIds(resultList, false);

        foreach (var loopContent in dbContent)
        {
            if (!DynamicTypeTools.PropertyExists(loopContent, "MainPicture")) continue;

            if (loopContent.MainPicture is not Guid) continue;

            returnList.Add(loopContent);
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
    private static async Task<string?> Process(string? toProcess, Func<List<Guid>, Task<string>> pageConversion,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Photo Gallery Codes");

        var resultList = BracketCodeCommon.ContentGalleryBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        foreach (var loopMatch in resultList)
        {
            toProcess = toProcess.Replace(loopMatch.bracketCodeText, await pageConversion(loopMatch.contentGuid));

            progress?.Report($"Photo Gallery Code - {loopMatch.contentGuid.Count} processed");
        }

        return toProcess;
    }

    /// <summary>
    ///     Processes {{photo guid;human_identifier}} into figure html with a link to the photo page.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<string> ProcessToGallery(string? toProcess, IProgress<string>? progress = null)
    {
        async Task<string> Processor(List<Guid> guidListToProcess)
        {
            var galleryId = $"IG-{Guid.NewGuid()}";
            var galleryDiv = new DivTag().Id(galleryId);

            var db = await Db.Context();

            var content = await db.ContentFromContentIds(guidListToProcess, false);
            var contentWithMainPicture = content.Where(x => DynamicTypeTools.PropertyExists(x, "MainPicture")).ToList();

            var rowHeight = 180;

            foreach (var loopContent in contentWithMainPicture)
            {
                if (loopContent.MainPicture is not Guid mainPictureId) continue;

                if (loopContent.ContentId is not Guid contentId) continue;

                var pageUrl = await UserSettingsSingleton.CurrentSettings().ContentUrl(contentId);
                var pictureAsset = PictureAssetProcessing.ProcessPictureDirectory(mainPictureId);
                if (pictureAsset == null) continue;

                var linkTag = new LinkTag(string.Empty, pageUrl);

                var closestHeight = pictureAsset.SrcsetImages.Any(x => x.Height >= rowHeight)
                    ? pictureAsset.SrcsetImages.Where(x => x.Height >= rowHeight).MinBy(x => rowHeight - x.Height)
                    : pictureAsset.SrcsetImages.MaxBy(x => rowHeight - x.Height);

                if (closestHeight == null) continue;

                var title = DynamicTypeTools.PropertyExists(loopContent, "Title") ? loopContent.Title : string.Empty;

                linkTag.Children.Add(new HtmlTag("img").Attr("src", closestHeight.SiteUrl).Attr("alt", title));

                galleryDiv.Children.Add(linkTag);
            }

            var withScriptTag = $$"""
                                  {{galleryDiv}}
                                  <script>
                                  $("#{{galleryId}}").justifiedGallery({
                                      rowHeight : {{rowHeight}},
                                      lastRow : 'center',
                                      margins : 3,
                                      captions:	true
                                    });
                                  </script>
                                  """;

            return withScriptTag;
        }

        return await Process(toProcess, Processor, progress).ConfigureAwait(false) ?? string.Empty;
    }
}