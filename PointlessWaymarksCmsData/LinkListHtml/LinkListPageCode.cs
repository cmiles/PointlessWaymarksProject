using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Json;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Rss;

namespace PointlessWaymarksCmsData.LinkListHtml
{
    public partial class LinkListPage
    {
        public LinkListPage()
        {
            RssUrl = UserSettingsSingleton.CurrentSettings().LinkRssUrl();
            ListTitle = "Links";
        }

        public string ListTitle { get; set; }

        public string RssUrl { get; set; }

        public static HtmlTag LinkListEntry(LinkStream content)
        {
            if (content == null) return HtmlTag.Empty();

            var compactContentContainerDiv = new DivTag().AddClass("content-compact-container");

            var compactContentMainTextContentDiv = new DivTag().AddClass("link-compact-text-content-container");

            var compactContentMainTextTitleTextDiv =
                new DivTag().AddClass("content-compact-text-content-title-container");
            var compactContentMainTextTitleLink =
                new LinkTag(string.IsNullOrWhiteSpace(content.Title) ? content.Url : content.Title, content.Url)
                    .AddClass("content-compact-text-content-title-link");

            compactContentMainTextTitleTextDiv.Children.Add(compactContentMainTextTitleLink);

            var compactContentSummaryTextDiv = new DivTag().AddClass("link-compact-text-content-summary");

            var itemsPartOne = new List<string>();
            if (!string.IsNullOrWhiteSpace(content.Author)) itemsPartOne.Add(content.Author);
            if (content.LinkDate != null) itemsPartOne.Add(content.LinkDate.Value.ToString("M/d/yyyy"));
            if (content.LinkDate == null) itemsPartOne.Add($"Saved {content.CreatedOn:M/d/yyyy}");

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

            compactContentMainTextContentDiv.Children.Add(compactContentMainTextTitleTextDiv);
            compactContentMainTextContentDiv.Children.Add(compactContentSummaryTextDiv);

            compactContentContainerDiv.Children.Add(compactContentMainTextContentDiv);

            return compactContentContainerDiv;
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

        private static void WriteContentListRss()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var db = Db.Context().Result;

            var content = db.LinkStreams.Where(x => x.ShowInLinkRss).OrderByDescending(x => x.CreatedOn).ToList();

            var feed = new SyndicationFeed(settings.SiteName, $"{settings.SiteSummary} - Links",
                new Uri($"https://{settings.SiteUrl}"), $"https:{settings.LinkRssUrl()}", DateTime.Now)
            {
                Copyright = new TextSyndicationContent($"{DateTime.Now.Year} {settings.SiteAuthors}")
            };

            var items = new List<string>();

            foreach (var loopContent in content)
            {
                var linkParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(loopContent.Site)) linkParts.Add(loopContent.Site);
                if (!string.IsNullOrWhiteSpace(loopContent.Author)) linkParts.Add(loopContent.Author);
                if (loopContent.LinkDate != null) linkParts.Add(loopContent.LinkDate.Value.ToString("M/d/yyyy"));
                if (!string.IsNullOrWhiteSpace(loopContent.Description)) linkParts.Add(loopContent.Description);
                if (!string.IsNullOrWhiteSpace(loopContent.Comments)) linkParts.Add(loopContent.Comments);

                items.Add(RssStringBuilder.RssItemString(loopContent.Title, loopContent.Url,
                    string.Join(" - ", linkParts), loopContent.CreatedOn, loopContent.ContentId.ToString()));
            }

            var localIndexFile = settings.LocalSiteLinkRssFile();

            if (localIndexFile.Exists)
            {
                localIndexFile.Delete();
                localIndexFile.Refresh();
            }

            File.WriteAllText(localIndexFile.FullName,
                RssStringBuilder.RssFileString($"{UserSettingsSingleton.CurrentSettings().SiteName} - Link List",
                    string.Join(Environment.NewLine, items)), Encoding.UTF8);
        }

        private void WriteLocalDbJson()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var db = Db.Context().Result;
            var allContent = db.LinkStreams.OrderByDescending(x => x.CreatedOn).ToList();

            var jsonDbEntry = JsonSerializer.Serialize(allContent);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSiteLinkDirectory().FullName, "LinkList.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricLinkStreams.ToList();

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSiteLinkDirectory().FullName,
                "HistoricLinkList.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        public void WriteLocalHtmlRssAndJson()
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
            WriteLocalDbJson();
        }
    }
}