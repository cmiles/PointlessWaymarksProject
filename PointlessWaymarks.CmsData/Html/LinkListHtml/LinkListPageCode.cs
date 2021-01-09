using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Html.CommonHtml;
using PointlessWaymarks.CmsData.Rss;

namespace PointlessWaymarks.CmsData.Html.LinkListHtml
{
    public partial class LinkListPage
    {
        public LinkListPage()
        {
            RssUrl = UserSettingsSingleton.CurrentSettings().LinkRssUrl();
            ListTitle = "Links";
        }

        public DateTime? GenerationVersion { get; set; }
        public string ListTitle { get; set; }
        public string RssUrl { get; set; }

        public HtmlTag LinkTableTag()
        {
            var db = Db.Context().Result;

            var allContent = db.LinkContents.OrderByDescending(x => x.CreatedOn).ToList();

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

                photoListPhotoEntryDiv.Children.Add(ContentCompact.FromLinkContent(loopContent));

                allContentContainer.Children.Add(photoListPhotoEntryDiv);
            }

            return allContentContainer;
        }

        private static void WriteContentListRss()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var db = Db.Context().Result;

            var content = db.LinkContents.Where(x => x.ShowInLinkRss).OrderByDescending(x => x.CreatedOn).ToList();

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

                items.Add(RssBuilder.RssItemString(loopContent.Title, loopContent.Url, string.Join(" - ", linkParts),
                    loopContent.CreatedOn, loopContent.ContentId.ToString()));
            }

            var localIndexFile = settings.LocalSiteLinkRssFile();

            if (localIndexFile.Exists)
            {
                localIndexFile.Delete();
                localIndexFile.Refresh();
            }

            FileManagement.WriteAllTextToFileAndLog(localIndexFile.FullName,
                RssBuilder.RssFileString($"{UserSettingsSingleton.CurrentSettings().SiteName} - Link List",
                    string.Join(Environment.NewLine, items)), Encoding.UTF8);
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

            FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString);

            WriteContentListRss();
        }
    }
}