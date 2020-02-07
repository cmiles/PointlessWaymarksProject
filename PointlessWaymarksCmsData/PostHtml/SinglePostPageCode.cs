using System.Collections.Generic;
using System.IO;
using System.Linq;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.PostHtml
{
    public partial class SinglePostPage
    {
        public SinglePostPage(PostContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PostPageUrl(DbEntry);

            var db = Db.Context().Result;

            if (DbEntry.MainPicture != null) MainImage = new PictureSiteInformation(DbEntry.MainPicture.Value);

            PreviousPosts = db.PostContents.Where(x => x.CreatedOn < DbEntry.CreatedOn)
                .OrderByDescending(x => x.CreatedOn).Take(3).ToList();

            LaterPosts = db.PostContents.Where(x => x.CreatedOn > DbEntry.CreatedOn).OrderBy(x => x.CreatedOn).Take(3)
                .ToList();
        }

        public PostContent DbEntry { get; }

        public List<PostContent> LaterPosts { get; }

        public PictureSiteInformation MainImage { get; }

        public string PageUrl { get; }

        public List<PostContent> PreviousPosts { get; }

        public string SiteName { get; }

        public string SiteUrl { get; }

        public HtmlTag PreviousAndNextPostsDiv()
        {
            if (!LaterPosts.Any() && !PreviousPosts.Any()) return HtmlTag.Empty();

            var hasPreviousPosts = PreviousPosts.Any();
            var hasLaterPosts = LaterPosts.Any();
            var hasBothEarlierAndLaterPosts = hasPreviousPosts && hasLaterPosts;

            var relatedPostsContainer = new DivTag().AddClass("post-related-posts-container");
            relatedPostsContainer.Children.Add(new DivTag()
                .Text($"Posts {(hasPreviousPosts ? "Before" : "")}" +
                      $"{(hasBothEarlierAndLaterPosts ? "/" : "")}{(hasLaterPosts ? "After" : "")}:")
                .AddClass("post-related-posts-label-tag"));

            foreach (var loopPosts in PreviousPosts)
                relatedPostsContainer.Children.Add(RelatedPostContent.RelatedPostDiv(loopPosts));

            foreach (var loopPosts in LaterPosts)
                relatedPostsContainer.Children.Add(RelatedPostContent.RelatedPostDiv(loopPosts));

            return relatedPostsContainer;
        }

        public HtmlTag TitleDiv()
        {
            var titleContainer = new HtmlTag("div").AddClass("post-title-container");
            titleContainer.Children.Add(new HtmlTag("h1").AddClass("post-title-content").Text(DbEntry.Title));
            return titleContainer;
        }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

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