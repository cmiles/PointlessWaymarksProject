using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public void WriteLocalHtml()
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
        }
    }
}