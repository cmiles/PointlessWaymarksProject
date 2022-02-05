using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentHtml.PhotoGalleryHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class BracketCodeDailyPhotoPage
{
    public const string BracketCodeToken = "dailyphotoslink";

    public static string Create(PhotoContent content)
    {
        return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.PhotoCreatedOn:d} ({content.Title})}}}}";
    }

    public static async Task<List<PhotoContent>> DbContentFromBracketCodes(string toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return new List<PhotoContent>();

        progress?.Report("Searching for Daily Photo Codes...");

        var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<PhotoContent>();

        if (!guidList.Any()) return returnList;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in guidList)
        {
            var dbContent = context.PhotoContents.FirstOrDefault(x => x.ContentId == loopMatch);
            if (dbContent == null) continue;

            progress?.Report($"Daily Photos Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    public static async Task<string> Process(string toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Daily Photos Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbContent =
                await context.PhotoContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Adding daily photo link {dbContent.Title} from Code");

            var linkTag =
                new LinkTag(
                    string.IsNullOrWhiteSpace(loopMatch.displayText)
                        ? DailyPhotosPageParts.DailyPhotosPageHeader(dbContent.PhotoCreatedOn)
                        : loopMatch.displayText.Trim(),
                    UserSettingsSingleton.CurrentSettings().DailyPhotoGalleryUrl(dbContent.PhotoCreatedOn), "daily-photo-link");

            toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
        }

        return toProcess;
    }
}