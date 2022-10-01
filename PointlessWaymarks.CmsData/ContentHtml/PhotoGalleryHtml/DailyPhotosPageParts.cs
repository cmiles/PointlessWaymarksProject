using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.PhotoGalleryHtml;

public static class DailyPhotosPageParts
{
    public static string DailyPhotosPageHeader(DateTime photoCreatedOn) =>
        $"Photographs - {photoCreatedOn:MMMM d, dddd, yyyy}";

    public static HtmlTag DailyPhotosPageRelatedContentDiv(DailyPhotosPage? photoPage)
    {
        if (photoPage == null) return HtmlTag.Empty();

        var relatedPostContainerDiv = new DivTag().AddClasses("compact-content-container", "info-box");

        var relatedPostMainPictureContentDiv = new DivTag().AddClass("compact-content-image-content-container");

        relatedPostMainPictureContentDiv.Children.Add(Tags.PictureImgThumbWithLink(photoPage.MainImage?.Pictures,
            photoPage.PageUrl ?? string.Empty));

        relatedPostContainerDiv.Children.Add(relatedPostMainPictureContentDiv);

        var relatedPostMainTextContentDiv = new DivTag().AddClass("compact-content-text-content-container");

        var relatedPostMainTextTitleTextDiv = new DivTag().AddClass("compact-content-text-content-title-container");

        var relatedPostMainTextTitleLink =
            new LinkTag(photoPage.Title, photoPage.PageUrl).AddClass("compact-content-text-content-title-link");

        relatedPostMainTextTitleTextDiv.Children.Add(relatedPostMainTextTitleLink);

        var relatedPostMainTextCreatedOrUpdatedTextDiv = new DivTag().AddClass("compact-content-text-content-date")
            .Text(Tags.LatestCreatedOnOrUpdatedOn(
                          photoPage.MainImage?.Pictures?.DbEntry as ICreatedAndLastUpdateOnAndBy)
                      ?.ToString("M/d/yyyy") ??
                  string.Empty);

        relatedPostMainTextContentDiv.Children.Add(relatedPostMainTextTitleTextDiv);
        relatedPostMainTextContentDiv.Children.Add(relatedPostMainTextCreatedOrUpdatedTextDiv);

        relatedPostContainerDiv.Children.Add(relatedPostMainTextContentDiv);

        return relatedPostContainerDiv;
    }

    public static HtmlTag PhotoList(List<PictureSiteInformation> photos)
    {
        var containerDiv = new DivTag().AddClass("daily-photo-gallery-list-container");

        foreach (var loopPhotos in photos)
        {
            var photoContainer = new DivTag().AddClass("daily-photo-gallery-photo-container");
            photoContainer.Children.Add(
                loopPhotos.PictureFigureWithCaptionAndLinkToPicturePageTag(
                    "(min-width: 1200px) 33vw, (min-width: 600px) 50vw, 100vw"));

            containerDiv.Children.Add(photoContainer);
        }

        return containerDiv;
    }

    public static HtmlTag PreviousAndNextDailyPhotosDiv(DailyPhotosPage photoPage)
    {
        if (photoPage.PreviousDailyPhotosPage == null && photoPage.NextDailyPhotosPage == null)
            return HtmlTag.Empty();

        var hasPreviousPosts = photoPage.PreviousDailyPhotosPage != null;
        var hasLaterPosts = photoPage.NextDailyPhotosPage != null;
        var hasBothEarlierAndLaterPosts = hasPreviousPosts && hasLaterPosts;

        var relatedPostsContainer = new DivTag().AddClasses("post-related-posts-container", "compact-content-list-container");
        relatedPostsContainer.Children.Add(new DivTag()
            .Text($"Daily Photos {(hasPreviousPosts ? "Before" : "")}" +
                  $"{(hasBothEarlierAndLaterPosts ? "/" : "")}{(hasLaterPosts ? "After" : "")}:")
            .AddClasses("post-related-posts-label-tag", "compact-content-list-label"));

        if (hasPreviousPosts)
            relatedPostsContainer.Children.Add(DailyPhotosPageRelatedContentDiv(photoPage.PreviousDailyPhotosPage));

        if (hasLaterPosts)
            relatedPostsContainer.Children.Add(DailyPhotosPageRelatedContentDiv(photoPage.NextDailyPhotosPage));

        return relatedPostsContainer;
    }
}