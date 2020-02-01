using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.NoteHtml;
using PointlessWaymarksCmsData.Pictures;
using PointlessWaymarksCmsData.PostHtml;

namespace PointlessWaymarksCmsData.IndexHtml
{
    public partial class IndexPage
    {
        public IndexPage()
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            SiteKeywords = settings.SiteKeywords;
            SiteSummary = settings.SiteSummary;
            SiteAuthors = settings.SiteAuthors;
            PageUrl = settings.IndexPageUrl();

            var db = Db.Context().Result;

            var posts = db.PostContents.OrderByDescending(x => x.CreatedOn).Cast<dynamic>().Take(20).ToList();
            var notes = db.NoteContents.OrderByDescending(x => x.CreatedOn).Cast<dynamic>().Take(20).ToList();
            IndexContent = posts.Concat(notes).OrderByDescending(x => x.CreatedOn).Take(8).ToList();

            var mainImageGuid = IndexContent
                .FirstOrDefault(x => x.GetType() == typeof(PostContent) && x.MainPicture != null)?.MainPicture;

            if (mainImageGuid != null) MainImage = new PictureSiteInformation(mainImageGuid);
        }

        public List<dynamic> IndexContent { get; set; }


        public PictureSiteInformation MainImage { get; set; }

        public string PageUrl { get; set; }

        public string SiteAuthors { get; set; }
        public string SiteKeywords { get; set; }

        public string SiteName { get; set; }
        public string SiteSummary { get; set; }

        public string SiteUrl { get; set; }

        public HtmlTag IndexPosts()
        {
            if (!IndexContent.Any()) return HtmlTag.Empty();

            var indexBodyContainer = new DivTag().AddClass("index-posts-container");

            foreach (var loopPosts in IndexContent)
            {
                if (loopPosts.GetType() == typeof(PostContent))
                {
                    var post = new SinglePostDiv(loopPosts);
                    var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                    indexPostContentDiv.Encoded(false).Text(post.TransformText());
                    indexBodyContainer.Children.Add(indexPostContentDiv);
                    indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
                }

                if (loopPosts.GetType() == typeof(NoteContent))
                {
                    var post = new SingleNoteDiv(loopPosts);
                    var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                    indexPostContentDiv.Encoded(false).Text(post.TransformText());
                    indexBodyContainer.Children.Add(indexPostContentDiv);
                    indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
                }
            }

            return indexBodyContainer;
        }

        public HtmlTag Title()
        {
            var titleContainer = new DivTag().AddClass("index-title-container");
            var titleHeader = new HtmlTag("H1").AddClass("index-title-content").Text(SiteName);
            var titleSiteSummary = new HtmlTag("H5").AddClass("index-title-summary-content").Text(SiteSummary);

            titleContainer.Children.Add(titleHeader);
            titleContainer.Children.Add(titleSiteSummary);

            return titleContainer;
        }

        public void WriteRss()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var feed = new SyndicationFeed(SiteName, SiteSummary, new Url(SiteUrl),
                $"https:{UserSettingsSingleton.CurrentSettings().RssIndexFeedUrl()}", DateTime.Now);
            feed.Copyright = new TextSyndicationContent($"{DateTime.Now.Year} {SiteAuthors}");

            var items = new List<SyndicationItem>();

            foreach (var loopPosts in IndexContent)
            {
                if (loopPosts.GetType() == typeof(PostContent))
                {
                    var post = new SinglePostDiv(loopPosts);
                    items.Add(new SyndicationItem(post.DbEntry.Title, post.DbEntry.Summary,
                        new Uri($"https:{post.PageUrl}"), post.DbEntry.Slug, post.DbEntry.CreatedOn));
                }

                if (loopPosts.GetType() == typeof(NoteContent))
                {
                    var post = new SingleNoteDiv(loopPosts);
                    items.Add(new SyndicationItem(NoteParts.TitleString(post.DbEntry), post.DbEntry.Summary,
                        new Uri($"https:{post.PageUrl}"), post.DbEntry.Slug, post.DbEntry.CreatedOn));
                }
            }

            feed.Items = items;

            var xmlSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                NewLineHandling = NewLineHandling.Entitize,
                NewLineOnAttributes = true,
                Indent = true
            };

            using var stream = new MemoryStream();

            var localIndexFile = UserSettingsSingleton.CurrentSettings().LocalSiteRssIndexFeedListFile();

            if (localIndexFile.Exists)
            {
                localIndexFile.Delete();
                localIndexFile.Refresh();
            }

            using (var xmlWriter = XmlWriter.Create(localIndexFile.FullName, xmlSettings))
            {
                var rssFormatter = new Rss20FeedFormatter(feed, false);
                rssFormatter.WriteTo(xmlWriter);
                xmlWriter.Flush();
            }
        }

        public void WriteLocalHtml()
        {
            WriteRss();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo =
                new FileInfo($@"{UserSettingsSingleton.CurrentSettings().LocalSiteRootDirectory}\index.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}