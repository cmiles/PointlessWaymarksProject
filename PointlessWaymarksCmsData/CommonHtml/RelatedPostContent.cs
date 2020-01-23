using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData
{
    public static class RelatedPostContent
    {
        public static HtmlTag RelatedPostDiv(PostContent post)
        {
            if (post == null) return HtmlTag.Empty();

            var relatedPostContainerDiv = new DivTag().AddClass("related-post-container");

            var relatedPostMainPictureContentDiv = new DivTag().AddClass("related-post-image-content-container");

            var image = new PictureSiteInformation(post.MainPicture.Value);
            var postImgLink = new LinkTag(string.Empty, UserSettingsSingleton.CurrentSettings().PostPageUrl(post));
            var imgTag = Tags.PictureImgTagWithSmallestDefaultSrc(image.Pictures);

            imgTag.Style(
                image.Pictures.SmallPicture.Height > image.Pictures.SmallPicture.Width ? "max-width" : "max-height",
                "100px");

            postImgLink.Children.Add(imgTag);
            relatedPostMainPictureContentDiv.Children.Add(postImgLink);

            var relatedPostMainTextContentDiv = new DivTag().AddClass("related-post-text-content-container");

            var relatedPostMainTextTitleTextDiv = new DivTag().AddClass("related-post-text-content-title-container");
            var relatedPostMainTextTitleLink =
                new LinkTag(post.Title, UserSettingsSingleton.CurrentSettings().PostPageUrl(post)).AddClass(
                    "related-post-text-content-title-container");
            relatedPostMainTextTitleTextDiv.Children.Add(relatedPostMainTextTitleLink);

            var relatedPostMainTextCreatedOrUpdatedTextDiv = new DivTag().AddClass("related-post-text-content-date")
                .Text(Tags.LatestCreatedOnOrUpdatedOn(post)?.ToString("M/d/yyyy") ?? string.Empty);

            relatedPostMainTextContentDiv.Children.Add(relatedPostMainTextTitleTextDiv);
            relatedPostMainTextContentDiv.Children.Add(relatedPostMainTextCreatedOrUpdatedTextDiv);

            relatedPostContainerDiv.Children.Add(relatedPostMainPictureContentDiv);
            relatedPostContainerDiv.Children.Add(relatedPostMainTextContentDiv);

            return relatedPostContainerDiv;
        }

        public static async Task<List<PostContent>> RelatedPosts(this PointlessWaymarksContext toQuery, Guid toCheckFor)
        {
            return await toQuery.PostContents.Where(x => x.BodyContent.Contains(toCheckFor.ToString())).ToListAsync();
        }

        public static async Task<HtmlTag> RelatedPostsTag(Guid toCheckFor)
        {
            var db = await Db.Context();
            var posts = await db.RelatedPosts(toCheckFor);

            if (posts == null || !posts.Any()) return HtmlTag.Empty();

            var relatedPostsList = new DivTag().AddClass("related-posts-list-container");

            relatedPostsList.Children.Add(new DivTag().Text("Appears In:").AddClass("related-post-label-tag"));

            foreach (var loopPost in posts) relatedPostsList.Children.Add(RelatedPostDiv(loopPost));

            return relatedPostsList;
        }
    }
}