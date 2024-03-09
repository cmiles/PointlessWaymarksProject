using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class SlugUtility
{
    public static async Task<bool> FileFilenameExistsInDatabase(this PointlessWaymarksContext context,
        string filename, Guid? exceptInThisContent)
    {
        if (string.IsNullOrWhiteSpace(filename)) return false;

        bool imageCheck;

        if (exceptInThisContent == null)
            imageCheck = await context.FileContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                .AnyAsync(x => x.OriginalFileName!.ToLower() == filename.ToLower()).ConfigureAwait(false);
        else
            imageCheck = await context.FileContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                .AnyAsync(x =>
                    x.OriginalFileName!.ToLower() == filename.ToLower() &&
                    x.ContentId != exceptInThisContent.Value).ConfigureAwait(false);

        return imageCheck;
    }

    public static async Task<bool> ImageFilenameExistsInDatabase(this PointlessWaymarksContext context,
        string filename, Guid? exceptInThisContent)
    {
        if (string.IsNullOrWhiteSpace(filename)) return false;

        bool imageCheck;

        if (exceptInThisContent == null)
            imageCheck = await context.ImageContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                .AnyAsync(x => x.OriginalFileName!.ToLower() == filename.ToLower()).ConfigureAwait(false);
        else
            imageCheck = await context.ImageContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                .AnyAsync(x =>
                    x.OriginalFileName!.ToLower() == filename.ToLower() &&
                    x.ContentId != exceptInThisContent.Value).ConfigureAwait(false);

        return imageCheck;
    }

    public static async Task<bool> PhotoFilenameExistsInDatabase(this PointlessWaymarksContext context,
        string filename, Guid? exceptInThisContent)
    {
        if (string.IsNullOrWhiteSpace(filename)) return false;

        bool photoCheck;

        if (exceptInThisContent == null)
            photoCheck = await context.PhotoContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                .AnyAsync(x => x.OriginalFileName!.ToLower() == filename.ToLower()).ConfigureAwait(false);
        else
            photoCheck = await context.PhotoContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                .AnyAsync(x =>
                    x.OriginalFileName!.ToLower() == filename.ToLower() &&
                    x.ContentId != exceptInThisContent.Value).ConfigureAwait(false);

        return photoCheck;
    }

    public static async Task<bool> SlugExistsInDatabase(this PointlessWaymarksContext context, string? slug,
        Guid? excludedContentId)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;

        //!!Content Type List!!
        if (excludedContentId == null)
        {
            var fileCheck = await context.FileContents.AnyAsync(x => x.Slug == slug).ConfigureAwait(false);
            var geoJsonCheck = await context.GeoJsonContents.AnyAsync(x => x.Slug == slug).ConfigureAwait(false);
            var imageCheck = await context.ImageContents.AnyAsync(x => x.Slug == slug).ConfigureAwait(false);
            var lineCheck = await context.LineContents.AnyAsync(x => x.Slug == slug).ConfigureAwait(false);
            var noteCheck = await context.NoteContents.AnyAsync(x => x.Slug == slug).ConfigureAwait(false);
            var photoCheck = await context.PhotoContents.AnyAsync(x => x.Slug == slug).ConfigureAwait(false);
            var pointCheck = await context.PointContents.AnyAsync(x => x.Slug == slug).ConfigureAwait(false);
            var postCheck = await context.PostContents.AnyAsync(x => x.Slug == slug).ConfigureAwait(false);
            var videoCheck = await context.VideoContents.AnyAsync(x => x.Slug == slug).ConfigureAwait(false);

            return photoCheck || postCheck || imageCheck || noteCheck || fileCheck || pointCheck || geoJsonCheck ||
                   lineCheck || videoCheck;
        }

        var fileExcludeCheck =
            await context.FileContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId)
                .ConfigureAwait(false);
        var geoJsonExcludeCheck =
            await context.GeoJsonContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId)
                .ConfigureAwait(false);
        var imageExcludeCheck =
            await context.ImageContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId)
                .ConfigureAwait(false);
        var lineExcludeCheck =
            await context.LineContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId)
                .ConfigureAwait(false);
        var noteExcludeCheck =
            await context.NoteContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId)
                .ConfigureAwait(false);
        var photoExcludeCheck =
            await context.PhotoContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId)
                .ConfigureAwait(false);
        var pointExcludeCheck =
            await context.PointContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId)
                .ConfigureAwait(false);
        var postExcludeCheck =
            await context.PostContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId)
                .ConfigureAwait(false);
        var videoExcludeCheck =
            await context.VideoContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId)
                .ConfigureAwait(false);


        return photoExcludeCheck || postExcludeCheck || imageExcludeCheck || noteExcludeCheck || fileExcludeCheck ||
               pointExcludeCheck || geoJsonExcludeCheck || lineExcludeCheck || videoExcludeCheck;
    }

    public static async Task<bool> VideoFilenameExistsInDatabase(this PointlessWaymarksContext context,
        string filename, Guid? exceptInThisContent)
    {
        if (string.IsNullOrWhiteSpace(filename)) return false;

        bool imageCheck;

        if (exceptInThisContent == null)
            imageCheck = await context.VideoContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                .AnyAsync(x => x.OriginalFileName!.ToLower() == filename.ToLower()).ConfigureAwait(false);
        else
            imageCheck = await context.VideoContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                .AnyAsync(x =>
                    x.OriginalFileName!.ToLower() == filename.ToLower() &&
                    x.ContentId != exceptInThisContent.Value).ConfigureAwait(false);

        return imageCheck;
    }
}