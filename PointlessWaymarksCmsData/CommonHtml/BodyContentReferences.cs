using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.PhotoGalleryHtml;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class BodyContentReferences
    {
        public static async Task<List<IContentCommon>> RelatedContent(this PointlessWaymarksContext toQuery,
            Guid toCheckFor)
        {
            var posts = await toQuery.PostContents.Where(x => x.BodyContent.Contains(toCheckFor.ToString()))
                .Cast<IContentCommon>().ToListAsync();
            var notes = await toQuery.NoteContents.Where(x => x.BodyContent.Contains(toCheckFor.ToString()))
                .Cast<IContentCommon>().ToListAsync();
            var files = await toQuery.FileContents.Where(x => x.BodyContent.Contains(toCheckFor.ToString()))
                .Cast<IContentCommon>().ToListAsync();

            return posts.Concat(notes).Concat(files).OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn).ToList();
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

        public static async Task<HtmlTag> RelatedContentTag(Guid toCheckFor, string bodyContentToCheckIn,
            IProgress<string> progress = null)
        {
            var contentCommonList = new List<IContentCommon>();

            var db = await Db.Context();

            contentCommonList.AddRange(await RelatedContent(db, toCheckFor));
            contentCommonList.AddRange(BracketCodeFiles.DbContentFromBracketCodes(bodyContentToCheckIn, progress));
            contentCommonList.AddRange(BracketCodeImages.DbContentFromBracketCodes(bodyContentToCheckIn, progress));
            contentCommonList.AddRange(BracketCodeImageLinks.DbContentFromBracketCodes(bodyContentToCheckIn, progress));
            contentCommonList.AddRange(BracketCodeNotes.DbContentFromBracketCodes(bodyContentToCheckIn, progress));
            contentCommonList.AddRange(BracketCodePosts.DbContentFromBracketCodes(bodyContentToCheckIn, progress));

            var transformedList = new List<(DateTime sortDateTime, HtmlTag tagContent)>();

            if (contentCommonList.Any())
            {
                contentCommonList = contentCommonList.GroupBy(x => x.ContentId).Select(x => x.First()).ToList();

                foreach (var loopContent in contentCommonList)
                {
                    var toAdd = RelatedContentDiv(loopContent);
                    if (toAdd != null && !toAdd.IsEmpty())
                        transformedList.Add((loopContent.LastUpdatedOn ?? loopContent.CreatedOn, toAdd));
                }
            }

            var photoContent = BracketCodePhotos.DbContentFromBracketCodes(bodyContentToCheckIn, progress);
            photoContent.AddRange(BracketCodePhotoLink.DbContentFromBracketCodes(bodyContentToCheckIn, progress));

            //If the object itself is a photo add it to the list
            photoContent.AddIfNotNull(await db.PhotoContents.SingleOrDefaultAsync(x => x.ContentId == toCheckFor));

            if (photoContent.Any())
            {
                var dates = photoContent.Select(x => x.PhotoCreatedOn.Date).Distinct().ToList();

                foreach (var loopDates in dates)
                {
                    var toAdd = await DailyPhotoPageGenerators.DailyPhotoGallery(loopDates);
                    if (toAdd != null)
                        transformedList.Add((loopDates, DailyPhotosPageParts.DailyPhotosPageRelatedContentDiv(toAdd)));
                }
            }

            var relatedTags = transformedList.OrderByDescending(x => x.sortDateTime).Select(x => x.tagContent).ToList();

            if (!relatedTags.Any()) return HtmlTag.Empty();

            var relatedPostsList = new DivTag().AddClass("related-posts-list-container");

            relatedPostsList.Children.Add(new DivTag().Text("Related:").AddClass("related-post-label-tag"));

            foreach (var loopPost in relatedTags) relatedPostsList.Children.Add(loopPost);

            return relatedPostsList;
        }

        public static async Task<HtmlTag> RelatedContentTag(Guid toCheckFor)
        {
            var db = await Db.Context();
            var related = await db.RelatedContent(toCheckFor);

            if (related == null || !related.Any()) return HtmlTag.Empty();

            var relatedPostsList = new DivTag().AddClass("related-posts-list-container");

            relatedPostsList.Children.Add(new DivTag().Text("Related:").AddClass("related-post-label-tag"));

            foreach (var loopPost in related) relatedPostsList.Children.Add(RelatedContentDiv(loopPost));

            return relatedPostsList;
        }

        public static async Task<HtmlTag> RelatedContentTag(List<Guid> toCheckFor)
        {
            toCheckFor ??= new List<Guid>();

            var db = await Db.Context();

            var allRelated = new List<IContentCommon>();

            foreach (var loopGuid in toCheckFor) allRelated.AddRange(await db.RelatedContent(loopGuid));

            if (!allRelated.Any()) return HtmlTag.Empty();

            allRelated = allRelated.Distinct().OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn).ToList();

            var relatedPostsList = new DivTag().AddClass("related-posts-list-container");

            relatedPostsList.Children.Add(new DivTag().Text("Related:").AddClass("related-post-label-tag"));

            foreach (var loopPost in allRelated) relatedPostsList.Children.Add(RelatedContentDiv(loopPost));

            return relatedPostsList;
        }
    }
}