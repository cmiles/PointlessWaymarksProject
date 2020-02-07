using HtmlTags;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class ContentCompact
    {
        public static HtmlTag FromContent(IContentCommon content)
        {
            if (content == null) return HtmlTag.Empty();

            var compactContentContainerDiv = new DivTag().AddClass("content-compact-container");

            var linkTo = UserSettingsSingleton.CurrentSettings().ContentUrl(content.ContentId).Result;

            if (content?.MainPicture != null)
            {
                var compactContentMainPictureContentDiv =
                    new DivTag().AddClass("content-compact-image-content-container");

                var image = new PictureSiteInformation(content.MainPicture.Value);

                compactContentMainPictureContentDiv.Children.Add(Tags.PictureImgThumbWithLink(image.Pictures, linkTo));

                compactContentContainerDiv.Children.Add(compactContentMainPictureContentDiv);
            }

            var compactContentMainTextContentDiv = new DivTag().AddClass("content-compact-text-content-container");

            var compactContentMainTextTitleTextDiv =
                new DivTag().AddClass("content-compact-text-content-title-container");
            var compactContentMainTextTitleLink =
                new LinkTag(content.Title, linkTo).AddClass("content-compact-text-content-title-link");
            compactContentMainTextTitleTextDiv.Children.Add(compactContentMainTextTitleLink);

            var compactContentSummaryTextDiv = new DivTag().AddClass("content-compact-text-content-summary")
                .Text(content.Summary);

            var compactContentMainTextCreatedOrUpdatedTextDiv = new DivTag()
                .AddClass("content-compact-text-content-date")
                .Text(Tags.LatestCreatedOnOrUpdatedOn(content)?.ToString("M/d/yyyy") ?? string.Empty);

            compactContentMainTextContentDiv.Children.Add(compactContentMainTextTitleTextDiv);
            compactContentMainTextContentDiv.Children.Add(compactContentSummaryTextDiv);
            compactContentMainTextContentDiv.Children.Add(compactContentMainTextCreatedOrUpdatedTextDiv);

            compactContentContainerDiv.Children.Add(compactContentMainTextContentDiv);

            return compactContentContainerDiv;
        }
    }
}