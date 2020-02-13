using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.ContentListHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData
{
    public static class RelatedPostContent
    {
        public static async Task<(List<IContentCommon> previousContent, List<IContentCommon> laterContent)>
            PreviousAndLaterContent(int numberOfPreviousAndLater, DateTime createdOn)
        {
            var db = await Db.Context();

            var previousNotes = (await db.NoteContents.Where(x => x.CreatedOn < createdOn && x.ShowInMainSiteFeed)
                    .OrderByDescending(x => x.CreatedOn).Take(numberOfPreviousAndLater).ToListAsync())
                .Select(x => x.NoteToCommonContent()).Cast<IContentCommon>().ToList();

            var previousPosts = (await db.PostContents
                .Where(x => x.CreatedOn < createdOn && x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(numberOfPreviousAndLater).ToListAsync()).Cast<IContentCommon>().ToList();

            var previousPhotos = (await db.PhotoContents
                .Where(x => x.CreatedOn < createdOn && x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(numberOfPreviousAndLater).ToListAsync()).Cast<IContentCommon>().ToList();

            var previousImages = (await db.ImageContents
                .Where(x => x.CreatedOn < createdOn && x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(numberOfPreviousAndLater).ToListAsync()).Cast<IContentCommon>().ToList();

            var previousFiles = (await db.FileContents
                .Where(x => x.CreatedOn < createdOn && x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(numberOfPreviousAndLater).ToListAsync()).Cast<IContentCommon>().ToList();

            var previous = previousNotes.Concat(previousPosts).Concat(previousPhotos).Concat(previousImages)
                .Concat(previousFiles).OrderByDescending(x => x.CreatedOn).Take(numberOfPreviousAndLater).ToList();

            var laterNotes = (await db.NoteContents.Where(x => x.CreatedOn > createdOn && x.ShowInMainSiteFeed)
                    .OrderBy(x => x.CreatedOn).Take(numberOfPreviousAndLater).ToListAsync())
                .Select(x => x.NoteToCommonContent()).Cast<IContentCommon>().ToList();

            var laterPosts = (await db.PostContents.Where(x => x.CreatedOn > createdOn && x.ShowInMainSiteFeed)
                    .OrderBy(x => x.CreatedOn).Take(numberOfPreviousAndLater).ToListAsync()).Cast<IContentCommon>()
                .ToList();

            var laterPhotos = (await db.PhotoContents.Where(x => x.CreatedOn > createdOn && x.ShowInMainSiteFeed)
                    .OrderBy(x => x.CreatedOn).Take(numberOfPreviousAndLater).ToListAsync()).Cast<IContentCommon>()
                .ToList();

            var laterImages = (await db.ImageContents.Where(x => x.CreatedOn > createdOn && x.ShowInMainSiteFeed)
                    .OrderBy(x => x.CreatedOn).Take(numberOfPreviousAndLater).ToListAsync()).Cast<IContentCommon>()
                .ToList();

            var laterFiles = (await db.FileContents.Where(x => x.CreatedOn > createdOn && x.ShowInMainSiteFeed)
                    .OrderBy(x => x.CreatedOn).Take(numberOfPreviousAndLater).ToListAsync()).Cast<IContentCommon>()
                .ToList();

            var later = laterNotes.Concat(laterPosts).Concat(laterPhotos).Concat(laterImages).Concat(laterFiles)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfPreviousAndLater).ToList();

            return (previous, later);
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
            var relatedPostMainTextTitleLink =
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
                .ToListAsync();
            var notes =
                (await toQuery.NoteContents.Where(x => x.BodyContent.Contains(toCheckFor.ToString())).ToListAsync())
                .Select(x => x.NoteToCommonContent()).Cast<IContentCommon>().ToList();

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