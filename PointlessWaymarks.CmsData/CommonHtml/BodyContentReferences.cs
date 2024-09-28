using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.ContentHtml.PhotoGalleryHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class BodyContentReferences
{
    public static HtmlTag CompactContentDiv(IContentCommon? content)
    {
        if (content == null) return HtmlTag.Empty();
        var relatedPostContainerDiv = new DivTag().AddClasses("compact-content-container", "info-box");
        if (content.MainPicture != null)
        {
            var relatedPostMainPictureContentDiv = new DivTag().AddClass("compact-content-image-content-container");
            var image = new PictureSiteInformation(content.MainPicture.Value);
            relatedPostMainPictureContentDiv.Children.Add(Tags.PictureImgThumbWithLink(image.Pictures,
                UserSettingsSingleton.CurrentSettings().ContentUrl(content.ContentId).Result));
            relatedPostContainerDiv.Children.Add(relatedPostMainPictureContentDiv);
        }

        string combinedTitleAndSummary;
        if (content.Summary!.Equals(content.Title, StringComparison.OrdinalIgnoreCase) || content.Summary[..^1]
                .Equals(content.Title, StringComparison.OrdinalIgnoreCase))
        {
            combinedTitleAndSummary = $"{content.Title}";
        }
        else
        {
            combinedTitleAndSummary = $"{content.Title} - {content.Summary}";
        }

        var relatedPostMainTextContentDiv = new DivTag().AddClass("compact-content-text-content-container");
        var relatedPostMainTextTitleTextDiv = new DivTag().AddClass("compact-content-text-content-title-container");
        HtmlTag relatedPostMainTextTitleLink;
        if (content.MainPicture == null)
            relatedPostMainTextTitleLink =
                new LinkTag(combinedTitleAndSummary,
                        UserSettingsSingleton.CurrentSettings().ContentUrl(content.ContentId).Result)
                    .AddClass("compact-content-text-content-title-link");
        else
            relatedPostMainTextTitleLink =
                new LinkTag(content.Title, UserSettingsSingleton.CurrentSettings().ContentUrl(content.ContentId).Result)
                    .AddClass("compact-content-text-content-title-link");
        relatedPostMainTextTitleTextDiv.Children.Add(relatedPostMainTextTitleLink);
        var relatedPostMainTextCreatedOrUpdatedTextDiv = new DivTag().AddClass("compact-content-text-content-date")
            .Text(Tags.LatestCreatedOnOrUpdatedOn(content)?.ToString("M/d/yyyy") ?? string.Empty);
        relatedPostMainTextContentDiv.Children.Add(relatedPostMainTextTitleTextDiv);
        relatedPostMainTextContentDiv.Children.Add(relatedPostMainTextCreatedOrUpdatedTextDiv);
        relatedPostContainerDiv.Children.Add(relatedPostMainTextContentDiv);
        return relatedPostContainerDiv;
    }

    /// <summary>
    ///     Finds Post, Note and File reference to the input ContentId Guid.
    /// </summary>
    /// <param name="toQuery"></param>
    /// <param name="toCheckFor"></param>
    /// <param name="generationVersion"></param>
    /// <returns></returns>
    public static async Task<List<IContentCommon>> RelatedContentReferencesFromOtherContent(
        this PointlessWaymarksContext toQuery, Guid toCheckFor, DateTime? generationVersion)
    {
        if (generationVersion == null)
        {
            var posts = await toQuery.PostContents
                .Where(x => x.BodyContent != null && x.BodyContent.Contains(toCheckFor.ToString()))
                .Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var notes = await toQuery.NoteContents
                .Where(x => x.BodyContent != null && x.BodyContent.Contains(toCheckFor.ToString()))
                .Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var files = await toQuery.FileContents
                .Where(x => x.BodyContent != null && x.BodyContent.Contains(toCheckFor.ToString()))
                .Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);

            return posts.Concat(notes).Concat(files).OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToList();
        }

        var db = await Db.Context().ConfigureAwait(false);

        var referencesFromOtherContent = await db.GenerationRelatedContents
            .Where(x => x.GenerationVersion == generationVersion && x.ContentTwo == toCheckFor)
            .Select(x => x.ContentOne).Distinct().ToListAsync().ConfigureAwait(false);

        var otherReferences = await db.ContentFromContentIds(referencesFromOtherContent).ConfigureAwait(false);

        var typeFilteredReferences = new List<IContentCommon>();

        foreach (var loopContent in otherReferences)
            switch (loopContent)
            {
                case FileContent content:
                {
                    typeFilteredReferences.Add(content);
                    break;
                }
                case PostContent content:
                {
                    typeFilteredReferences.Add(content);
                    break;
                }
                case NoteContent content:
                {
                    typeFilteredReferences.Add(content);
                    break;
                }
            }

        return typeFilteredReferences.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn).ToList();
    }

    public static async Task<HtmlTag> CompactContentTag(IContentCommon content, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        if(!UserSettingsSingleton.CurrentSettings().ShowRelatedContent) return HtmlTag.Empty();

        var toSearch = string.Empty;

        toSearch += content.BodyContent + content.Summary;

        if (content is IUpdateNotes updateContent) toSearch += updateContent.UpdateNotes;

        return await CompactContentTag(content.ContentId, toSearch, generationVersion, progress).ConfigureAwait(false);
    }

    private static async Task<HtmlTag> CompactContentTag(Guid toCheckFor, string? bodyContentToCheckIn,
        DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var contentCommonList = new List<IContentCommon>();

        var db = await Db.Context().ConfigureAwait(false);

        //References to this content
        contentCommonList.AddRange(
            await RelatedContentReferencesFromOtherContent(db, toCheckFor, generationVersion).ConfigureAwait(false));

        //5/4/2021 - Based on looking at Pointless Waymarks it doesn't seem useful to link back to all the content that is used - for example an Image that is displayed doesn't merit a related content link to the image (the display of the image seems self documenting), otoh just using a link to an image seems like it may be worth a related link.
        contentCommonList.AddRange(
            await BracketCodeFiles.DbContentFromBracketCodes(bodyContentToCheckIn, progress).ConfigureAwait(false));
        //contentCommonList.AddRange(BracketCodeFileImageLink.DbContentFromBracketCodes(bodyContentToCheckIn, progress));
        contentCommonList.AddRange(
            await BracketCodeFileDownloads.DbContentFromBracketCodes(bodyContentToCheckIn, progress)
                .ConfigureAwait(false));
        //contentCommonList.AddRange(BracketCodeGeoJson.DbContentFromBracketCodes(bodyContentToCheckIn, progress));
        contentCommonList.AddRange(
            await BracketCodeGeoJsonLinks.DbContentFromBracketCodes(bodyContentToCheckIn, progress)
                .ConfigureAwait(false));
        //contentCommonList.AddRange(BracketCodeImages.DbContentFromBracketCodes(bodyContentToCheckIn, progress));
        contentCommonList.AddRange(
            await BracketCodeImageLinks.DbContentFromBracketCodes(bodyContentToCheckIn, progress)
                .ConfigureAwait(false));
        //contentCommonList.AddRange(BracketCodeLines.DbContentFromBracketCodes(bodyContentToCheckIn, progress));
        contentCommonList.AddRange(
            await BracketCodeLineLinks.DbContentFromBracketCodes(bodyContentToCheckIn, progress).ConfigureAwait(false));
        contentCommonList.AddRange(
            await BracketCodeNotes.DbContentFromBracketCodes(bodyContentToCheckIn, progress).ConfigureAwait(false));
        //contentCommonList.AddRange(BracketCodePoints.DbContentFromBracketCodes(bodyContentToCheckIn, progress));
        contentCommonList.AddRange(
            await BracketCodePointLinks.DbContentFromBracketCodes(bodyContentToCheckIn, progress)
                .ConfigureAwait(false));
        contentCommonList.AddRange(
            await BracketCodePointExternalDirectionLinks.DbContentFromBracketCodes(bodyContentToCheckIn, progress)
                .ConfigureAwait(false));
        contentCommonList.AddRange(
            await BracketCodePosts.DbContentFromBracketCodes(bodyContentToCheckIn, progress).ConfigureAwait(false));

        var transformedList = new List<(DateTime sortDateTime, HtmlTag tagContent)>();

        if (contentCommonList.Any())
        {
            contentCommonList = contentCommonList.GroupBy(x => x.ContentId).Select(x => x.First()).ToList();

            foreach (var loopContent in contentCommonList)
            {
                var toAdd = CompactContentDiv(loopContent);
                if (!toAdd.IsEmpty())
                    transformedList.Add((loopContent.LastUpdatedOn ?? loopContent.CreatedOn, toAdd));
            }
        }

        var photoContent = await BracketCodePhotos.DbContentFromBracketCodes(bodyContentToCheckIn, progress)
            .ConfigureAwait(false);
        photoContent.AddRange(
            await BracketCodePhotoLinks.DbContentFromBracketCodes(bodyContentToCheckIn, progress)
                .ConfigureAwait(false));

        //If the object itself is a photo add it to the list
        photoContent.AddIfNotNull(await db.PhotoContents.SingleOrDefaultAsync(x => x.ContentId == toCheckFor)
            .ConfigureAwait(false));

        if (photoContent.Any())
        {
            var dates = photoContent.Select(x => x.PhotoCreatedOn.Date).Distinct().ToList();

            foreach (var loopDates in dates)
            {
                var toAdd = await DailyPhotoPageGenerators.DailyPhotoGallery(loopDates, null).ConfigureAwait(false);
                if (toAdd != null)
                    transformedList.Add((loopDates, DailyPhotosPageParts.DailyPhotosPageRelatedContentDiv(toAdd)));
            }
        }

        var relatedTags = transformedList.OrderByDescending(x => x.sortDateTime).Select(x => x.tagContent).ToList();

        if (!relatedTags.Any()) return HtmlTag.Empty();

        var relatedPostsList =
            new DivTag().AddClasses("related-posts-list-container", "compact-content-list-container");

        relatedPostsList.Children.Add(new DivTag().Text("Related:")
            .AddClasses("compact-content-label-tag", "compact-content-list-label"));

        foreach (var loopPost in relatedTags) relatedPostsList.Children.Add(loopPost);

        return relatedPostsList;
    }

    public static async Task<HtmlTag> CompactContentTag(List<Guid> toCheckFor, DateTime? generationVersion)
    {
        var db = await Db.Context().ConfigureAwait(false);

        var allRelated = new List<IContentCommon>();

        foreach (var loopGuid in toCheckFor)
            allRelated.AddRange(await db.RelatedContentReferencesFromOtherContent(loopGuid, generationVersion)
                .ConfigureAwait(false));

        if (!allRelated.Any()) return HtmlTag.Empty();

        allRelated = allRelated.GroupBy(x => x.ContentId).Select(x => x.First())
            .OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn).ToList();

        var relatedPostsList =
            new DivTag().AddClasses("related-posts-list-container", "compact-content-list-container");

        relatedPostsList.Children.Add(new DivTag().Text("Related:")
            .AddClasses("compact-content-label-tag", "compact-content-list-label"));

        foreach (var loopPost in allRelated) relatedPostsList.Children.Add(CompactContentDiv(loopPost));

        return relatedPostsList;
    }
}