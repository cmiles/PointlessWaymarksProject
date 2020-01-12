using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.PhotoHtml;

namespace PointlessWaymarksCmsData.PostHtml
{
    public partial class SinglePostPage
    {
        public SinglePostPage(PostContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsUtilities.ReadSettings().Result;
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PostPageUrl(DbEntry);

            var db = Db.Context().Result;

            if (DbEntry.MainImage != null)
            {
                var dbImage = db.PhotoContents.SingleOrDefault(x => x.ContentId == DbEntry.MainImage.Value);

                if (dbImage != null) MainImage = new SinglePhotoPage(dbImage);
            }

            PreviousPosts = db.PostContents.Where(x => x.CreatedOn < DbEntry.CreatedOn)
                .OrderByDescending(x => x.CreatedOn).Take(3).ToList();

            LaterPosts = db.PostContents.Where(x => x.CreatedOn > DbEntry.CreatedOn).OrderBy(x => x.CreatedOn).Take(3)
                .ToList();
        }

        public PostContent DbEntry { get; }

        public List<PostContent> LaterPosts { get; }

        public SinglePhotoPage MainImage { get; }

        public string PageUrl { get; }

        public List<PostContent> PreviousPosts { get; }

        public string SiteName { get; }

        public string SiteUrl { get; }

        public HtmlTag RelatedPostsDiv()
        {
            if (!LaterPosts.Any() && !PreviousPosts.Any()) return HtmlTag.Empty();

            var settings = UserSettingsUtilities.ReadSettings().Result;

            var hasPreviousPosts = PreviousPosts.Any();
            var hasLaterPosts = PreviousPosts.Any();
            var hasBothEarlierAndLaterPosts = hasPreviousPosts && hasLaterPosts;

            var relatedPostsContainer = new DivTag().AddClass("post-related-posts-container");
            relatedPostsContainer.Children.Add(new DivTag()
                .Text($"Posts {(hasPreviousPosts ? "Before" : "")}" +
                      $"{(hasBothEarlierAndLaterPosts ? "/" : "")}{(hasLaterPosts ? "After" : "")}:")
                .AddClass("post-related-posts-label-tag"));

            foreach (var loopPosts in PreviousPosts)
            {
                var linkDiv = new DivTag().AddClass("post-related-posts-link-container");
                linkDiv.Children.Add(
                    new LinkTag($"{loopPosts.CreatedOn:M/d/yyyy} {loopPosts.Title}", settings.PostPageUrl(loopPosts))
                        .AddClass("post-related-posts-link"));
                relatedPostsContainer.Children.Add(linkDiv);
            }

            if (hasBothEarlierAndLaterPosts)
                relatedPostsContainer.Children.Add(new DivTag().Text("/").AddClass("post-related-posts-label-tag"));

            foreach (var loopPosts in LaterPosts)
            {
                var linkDiv = new DivTag().AddClass("post-related-posts-link-container");
                linkDiv.Children.Add(
                    new LinkTag($"{loopPosts.CreatedOn:M/d/yyyy} {loopPosts.Title}", settings.PostPageUrl(loopPosts))
                        .AddClass("post-related-posts-link"));
                relatedPostsContainer.Children.Add(linkDiv);
            }

            return relatedPostsContainer;
        }

        public HtmlTag TitleDiv()
        {
            var titleContainer = new HtmlTag("div").AddClass("post-title-container");
            titleContainer.Children.Add(new HtmlTag("h1").AddClass("post-title-content").Text(DbEntry.Title));
            return titleContainer;
        }

        public HtmlTag UpdateDiv()
        {
            if (string.IsNullOrWhiteSpace(DbEntry.UpdateNotes)) return HtmlTag.Empty();

            var updateNotesDiv = new DivTag().AddClass("update-notes-container");

            updateNotesDiv.Children.Add(HorizontalRule.StandardRule());

            var updateNotesContentContainer = new DivTag().AddClass("update-notes-content");

            var updateNotesHtml = ContentProcessor.ContentHtml(DbEntry.UpdateNotesFormat, DbEntry.UpdateNotes);

            if (updateNotesHtml.success) updateNotesContentContainer.Encoded(false).Text(updateNotesHtml.output);

            updateNotesDiv.Children.Add(updateNotesContentContainer);

            return updateNotesDiv;
        }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;

            var htmlString = TransformText();

            var htmlFileInfo =
                new FileInfo(
                    $"{Path.Combine(settings.LocalSitePostContentDirectory(DbEntry).FullName, DbEntry.Slug)}.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}