using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlTags;
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

                if (dbImage != null)
                {
                    MainImage = new SinglePhotoPage(dbImage);
                }
            }

            var previousPosts = db.PostContents.Where(x => x.CreatedOn < DbEntry.CreatedOn)
                .OrderByDescending(x => x.CreatedOn).Take(3).ToList();

            var nextPosts = db.PostContents.Where(x => x.CreatedOn > DbEntry.CreatedOn).OrderBy(x => x.CreatedOn)
                .Take(3).ToList();

            RelatedPosts = previousPosts.Concat(nextPosts).OrderBy(x => x.CreatedOn).ToList();
        }

        public List<PostContent> RelatedPosts { get; set; }

        public HtmlTag RelatedPostsDiv()
        {
            if (RelatedPosts == null || !RelatedPosts.Any()) return HtmlTag.Empty();

            var settings = UserSettingsUtilities.ReadSettings().Result;

            var relatedPostsContainer = new DivTag().AddClass("post-related-posts-container");
            relatedPostsContainer.Children.Add(new DivTag().Text("Posts Before/After:")
                .AddClass("post-related-posts-label-tag"));

            foreach (var loopPosts in RelatedPosts)
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

        public HtmlTag CreatedByAndUpdatedOnDiv()
        {
            var titleContainer = new HtmlTag("div").AddClass("title-area-created-and-updated-container");
            titleContainer.Children.Add(new HtmlTag("h3").AddClass("title-area-created-and-updated-content")
                .Text(CreatedByAndUpdatedOnString()));
            return titleContainer;
        }

        public HtmlTag PostBodyDiv()
        {
            var bodyContainer = new HtmlTag("div").AddClass("post-body-container");

            var bodyText = PhotoBracketCode.MarkdownPreprocessedForSitePhotoTags(DbEntry.BodyContent);

            var bodyHtmlProcessing = ContentProcessor.ContentHtml(DbEntry.BodyContentFormat, bodyText);

            if (bodyHtmlProcessing.success)
            {
                bodyContainer.Children.Add(new HtmlTag("div").AddClass("post-body-content").Encoded(false)
                    .Text(bodyHtmlProcessing.output));
            }

            return bodyContainer;
        }

        public HtmlTag UpdateDiv()
        {
            if (string.IsNullOrWhiteSpace(DbEntry.UpdateNotes)) return HtmlTag.Empty();

            var updateNotesDiv = new DivTag().AddClass("update-notes-container");

            updateNotesDiv.Children.Add(CommonHtml.HorizontalRule.StandardRule());

            var updateNotesContentContainer = new DivTag().AddClass("update-notes-content");

            var updateNotesHtml = ContentProcessor.ContentHtml(DbEntry.UpdateNotesFormat, DbEntry.UpdateNotes);

            if (updateNotesHtml.success)
            {
                updateNotesContentContainer.Encoded(false).Text(updateNotesHtml.output);
            }

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


        public String CreatedByAndUpdatedOnString()
        {
            var createdUpdatedString = string.Empty;

            var onlyCreated = false;

            if (DbEntry.LastUpdatedOn != null && DbEntry.CreatedOn.Date == DbEntry.LastUpdatedOn.Value.Date)
            {
                if (string.Compare(DbEntry.CreatedBy, DbEntry.LastUpdatedBy, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    createdUpdatedString += $"Created by {DbEntry.CreatedBy} and {DbEntry.LastUpdatedBy} ";
                    onlyCreated = true;
                }
                else
                {
                    createdUpdatedString += $"Created by {DbEntry.CreatedBy} ";
                }
            }

            createdUpdatedString += $"on {DbEntry.CreatedOn:M/d/yyyy}. ";

            if (onlyCreated) return createdUpdatedString;

            if (string.IsNullOrWhiteSpace(DbEntry.LastUpdatedBy) && DbEntry.LastUpdatedOn == null)
                return createdUpdatedString;

            if (DbEntry.LastUpdatedOn != null && DbEntry.CreatedOn.Date == DbEntry.LastUpdatedOn.Value.Date)
                return createdUpdatedString;

            var updatedString = "Updated";

            if (!string.IsNullOrWhiteSpace(DbEntry.LastUpdatedBy)) updatedString += $" by {DbEntry.LastUpdatedBy}";

            if (DbEntry.LastUpdatedOn != null)
            {
                updatedString += $" on {DbEntry.LastUpdatedOn.Value:M/d/yyyy}";
            }

            updatedString += ".";

            return (createdUpdatedString + updatedString).Trim();
        }

        public SinglePhotoPage MainImage { get; set; }

        public string PageUrl { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }

        public PostContent DbEntry { get; set; }
    }
}