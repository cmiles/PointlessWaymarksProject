using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.LinkListHtml
{
    public partial class LinkListPage
    {
        public LinkListPage()
        {
            RssUrl = UserSettingsSingleton.CurrentSettings().LinkListUrl();
            ListTitle = "Links";
        }

        public string ListTitle { get; set; }

        public string RssUrl { get; set; }

        public static HtmlTag LinkListEntry(LinkStream content)
        {
            if (content == null) return HtmlTag.Empty();

            var compactContentContainerDiv = new DivTag().AddClass("content-compact-container");

            var compactContentMainTextContentDiv = new DivTag().AddClass("content-compact-text-content-container");

            var compactContentMainTextTitleTextDiv =
                new DivTag().AddClass("content-compact-text-content-title-container");
            var compactContentMainTextTitleLink =
                new LinkTag(string.IsNullOrWhiteSpace(content.Title) ? content.Url : content.Title, content.Url)
                    .AddClass("content-compact-text-content-title-link");

            compactContentMainTextTitleTextDiv.Children.Add(compactContentMainTextTitleLink);

            var compactContentSummaryTextDiv = new DivTag().AddClass("content-compact-text-content-summary");

            var itemsPartOne = new List<string>();
            if (!string.IsNullOrWhiteSpace(content.Site)) itemsPartOne.Add(content.Site);
            if (!string.IsNullOrWhiteSpace(content.Author)) itemsPartOne.Add(content.Author);
            if (content.LinkDate != null) itemsPartOne.Add(content.LinkDate.Value.ToString("M/d/yyyy"));

            if (itemsPartOne.Any())
            {
                var textPartOneDiv = new DivTag().AddClass("content-compact-text-content-link-summary")
                    .Text(string.Join(" - ", itemsPartOne));
                compactContentSummaryTextDiv.Children.Add(textPartOneDiv);
            }

            if (!string.IsNullOrWhiteSpace(content.Description))
            {
                var textPartThreeDiv = new DivTag().AddClass("content-compact-text-content-link-summary")
                    .Text(content.Description);
                compactContentSummaryTextDiv.Children.Add(textPartThreeDiv);
            }

            if (!string.IsNullOrWhiteSpace(content.Comments))
            {
                var textPartTwoDiv = new DivTag().AddClass("content-compact-text-content-link-summary")
                    .Text(content.Comments);
                compactContentSummaryTextDiv.Children.Add(textPartTwoDiv);
            }

            var compactContentMainTextCreatedOrUpdatedTextDiv = new DivTag()
                .AddClass("content-compact-text-content-date")
                .Text(Tags.LatestCreatedOnOrUpdatedOn(content)?.ToString("M/d/yyyy") ?? string.Empty);

            compactContentMainTextContentDiv.Children.Add(compactContentSummaryTextDiv);
            compactContentMainTextContentDiv.Children.Add(compactContentMainTextCreatedOrUpdatedTextDiv);
            compactContentMainTextContentDiv.Children.Add(compactContentMainTextTitleTextDiv);
            compactContentContainerDiv.Children.Add(compactContentMainTextContentDiv);

            return compactContentContainerDiv;
        }

        private static async void WriteContentListRss()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var db = Db.Context().Result;

            var content = db.LinkStreams.OrderByDescending(x => x.CreatedOn).ToList();

            var feed = new SyndicationFeed(settings.SiteName, $"{settings.SiteSummary} - Links",
                new Uri($"https://{settings.SiteUrl}"), $"https:{settings.LinkRssUrl()}", DateTime.Now);
            feed.Copyright = new TextSyndicationContent($"{DateTime.Now.Year} {settings.SiteAuthors}");

            var items = new List<SyndicationItem>();

            foreach (var loopContent in content)
            {
                var contentUrl = await settings.ContentUrl(loopContent.ContentId);

                var title = string.IsNullOrWhiteSpace(loopContent.Title) ? loopContent.Url : loopContent.Title;

                var linkParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(loopContent.Site)) linkParts.Add(loopContent.Site);
                if (!string.IsNullOrWhiteSpace(loopContent.Author)) linkParts.Add(loopContent.Author);
                if (loopContent.LinkDate != null) linkParts.Add(loopContent.LinkDate.Value.ToString("M/d/yyyy"));
                if (!string.IsNullOrWhiteSpace(loopContent.Description)) linkParts.Add(loopContent.Description);
                if (!string.IsNullOrWhiteSpace(loopContent.Comments)) linkParts.Add(loopContent.Comments);

                items.Add(new SyndicationItem(loopContent.Title, string.Join(" - ", linkParts),
                    new Uri(loopContent.Url)));
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

            var localIndexFile = settings.LocalSiteLinkRssFile();

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

        public HtmlTag LinkTableTag()
        {
            var db = Db.Context().Result;

            var allContent = db.LinkStreams.OrderByDescending(x => x.CreatedOn).ToList();

            var allContentContainer = new DivTag().AddClass("content-list-container");

            foreach (var loopContent in allContent)
            {
                var photoListPhotoEntryDiv = new DivTag().AddClass("content-list-item-container");

                var titleList = new List<string>();

                if (!string.IsNullOrWhiteSpace(loopContent.Title)) titleList.Add(loopContent.Title);
                if (!string.IsNullOrWhiteSpace(loopContent.Site)) titleList.Add(loopContent.Site);
                if (!string.IsNullOrWhiteSpace(loopContent.Author)) titleList.Add(loopContent.Author);

                photoListPhotoEntryDiv.Data("title", string.Join(" - ", titleList));
                photoListPhotoEntryDiv.Data("tags", loopContent.Tags);
                photoListPhotoEntryDiv.Data("description", loopContent.Description);
                photoListPhotoEntryDiv.Data("comment", loopContent.Comments);

                photoListPhotoEntryDiv.Children.Add(LinkListEntry(loopContent));

                allContentContainer.Children.Add(photoListPhotoEntryDiv);
            }

            return allContentContainer;
        }

        public void WriteLocalHtmlAndRss()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo = settings.LocalSiteLinkListFile();

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);

            WriteContentListRss();
        }
    }
}