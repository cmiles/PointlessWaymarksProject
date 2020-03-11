using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData
{
    public static class RelatedPostContent
    {
        public static async Task<(List<IContentCommon> previousContent, List<IContentCommon> laterContent)>
            PreviousAndLaterContent(int numberOfPreviousAndLater, DateTime createdOn)
        {
            var previousContent = Db.MainFeedCommonContentBefore(createdOn, numberOfPreviousAndLater).Result;
            var laterContent = Db.MainFeedCommonContentAfter(createdOn, numberOfPreviousAndLater).Result;

            return (previousContent, laterContent);
        }

        public static HtmlTag PreviousAndNextPostsDiv(List<IContentCommon> previousPosts,
            List<IContentCommon> laterPosts)
        {
            if (!laterPosts.Any() && !previousPosts.Any()) return HtmlTag.Empty();

            var hasPreviousPosts = previousPosts.Any();
            var hasLaterPosts = laterPosts.Any();
            var hasBothEarlierAndLaterPosts = hasPreviousPosts && hasLaterPosts;

            var relatedPostsContainer = new DivTag().AddClass("post-related-posts-container");
            relatedPostsContainer.Children.Add(new DivTag()
                .Text($"Posts {(hasPreviousPosts ? "Before" : "")}" +
                      $"{(hasBothEarlierAndLaterPosts ? "/" : "")}{(hasLaterPosts ? "After" : "")}:")
                .AddClass("post-related-posts-label-tag"));

            foreach (var loopPosts in previousPosts)
                relatedPostsContainer.Children.Add(RelatedContentDiv(loopPosts));

            foreach (var loopPosts in laterPosts)
                relatedPostsContainer.Children.Add(RelatedContentDiv(loopPosts));

            return relatedPostsContainer;
        }

        public static HtmlTag RelatedContentDiv(IContentCommon post)
        {
            if (post == null) return HtmlTag.Empty();

            var relatedPostContainerDiv = new DivTag().AddClass("related-post-container");

            if (post.MainPicture != null)
            {
                var relatedPostMainPictureContentDiv = new DivTag().AddClass("related-post-image-content-container");

                var image = new PictureSiteInformation(post.MainPicture.Value);

                relatedPostMainPictureContentDiv.Children.Add(Tags.PictureImgThumbWithLink(image.Pictures,
                    UserSettingsSingleton.CurrentSettings().ContentUrl(post.ContentId).Result));

                relatedPostContainerDiv.Children.Add(relatedPostMainPictureContentDiv);
            }

            var relatedPostMainTextContentDiv = new DivTag().AddClass("related-post-text-content-container");

            var relatedPostMainTextTitleTextDiv = new DivTag().AddClass("related-post-text-content-title-container");

            HtmlTag relatedPostMainTextTitleLink;

            if (post.MainPicture == null)
                relatedPostMainTextTitleLink =
                    new LinkTag($"{post.Title} - {post.Summary}",
                            UserSettingsSingleton.CurrentSettings().ContentUrl(post.ContentId).Result)
                        .AddClass("related-post-text-content-title-link");
            else
                relatedPostMainTextTitleLink =
                    new LinkTag(post.Title, UserSettingsSingleton.CurrentSettings().ContentUrl(post.ContentId).Result)
                        .AddClass("related-post-text-content-title-link");

            relatedPostMainTextTitleTextDiv.Children.Add(relatedPostMainTextTitleLink);

            var relatedPostMainTextCreatedOrUpdatedTextDiv = new DivTag().AddClass("related-post-text-content-date")
                .Text(Tags.LatestCreatedOnOrUpdatedOn(post)?.ToString("M/d/yyyy") ?? string.Empty);

            relatedPostMainTextContentDiv.Children.Add(relatedPostMainTextTitleTextDiv);
            relatedPostMainTextContentDiv.Children.Add(relatedPostMainTextCreatedOrUpdatedTextDiv);

            relatedPostContainerDiv.Children.Add(relatedPostMainTextContentDiv);

            return relatedPostContainerDiv;
        }

        public static async Task<List<IContentCommon>> RelatedPostsAndNotes(this PointlessWaymarksContext toQuery,
            Guid toCheckFor)
        {
            var posts = await toQuery.PostContents.Where(x => x.BodyContent.Contains(toCheckFor.ToString()))
                .Cast<IContentCommon>().ToListAsync();
            var notes =
                await toQuery.NoteContents.Where(x => x.BodyContent.Contains(toCheckFor.ToString())).Cast<IContentCommon>().ToListAsync();

            return posts.Concat(notes).ToList();
        }

        public static async Task<HtmlTag> RelatedPostsTag(Guid toCheckFor)
        {
            var db = await Db.Context();
            var posts = await db.RelatedPostsAndNotes(toCheckFor);

            if (posts == null || !posts.Any()) return HtmlTag.Empty();

            var relatedPostsList = new DivTag().AddClass("related-posts-list-container");

            relatedPostsList.Children.Add(new DivTag().Text("Appears In:").AddClass("related-post-label-tag"));

            foreach (var loopPost in posts) relatedPostsList.Children.Add(RelatedContentDiv(loopPost));

            return relatedPostsList;
        }
    }
}