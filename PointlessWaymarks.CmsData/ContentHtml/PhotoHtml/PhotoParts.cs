using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;

public static class PhotoParts
{
    public static HtmlTag SizeList(PhotoContent content, PictureSiteInformation? photoInformation)
    {
        if (!content.ShowPhotoSizes || photoInformation?.Pictures == null) return HtmlTag.Empty();

        var outerContainer = new DivTag().AddClasses("photo-sizes-container", "info-list-container");

        outerContainer.Children.Add(new DivTag().AddClasses("photo-sizes-label-tag", "info-list-label")
            .Text("Specific Sizes:"));

        var sizes = photoInformation.Pictures.SrcsetImages.OrderBy(x => x.Height * x.Width).ToList();

        foreach (var loopSizes in sizes)
        {
            var tagLinkContainer = new DivTag().AddClasses("tags-detail-link-container", "info-list-item-container");

            var tagLink =
                new LinkTag($"{loopSizes.Width}x{loopSizes.Height}",
                        loopSizes.SiteUrl)
                    .AddClasses("tag-detail-link", "info-list-item-link");
            tagLinkContainer.Children.Add(tagLink);
            outerContainer.Children.Add(tagLinkContainer);
        }

        return outerContainer;
    }
}