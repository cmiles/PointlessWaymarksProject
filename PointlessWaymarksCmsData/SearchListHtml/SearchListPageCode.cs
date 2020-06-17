using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.SearchListHtml
{
    public partial class SearchListPage
    {
        public SearchListPage(string rssUrl)
        {
            RssUrl = rssUrl;
        }

        public Func<List<object>> ContentFunction { get; set; }
        public string ListTitle { get; set; }

        public string RssUrl { get; set; }

        public HtmlTag ContentTableTag()
        {
            var allContent = ContentFunction();

            var allContentContainer = new DivTag().AddClass("content-list-container");

            foreach (var loopContent in allContent)
                if (loopContent is IContentCommon loopContentCommon)
                {
                    var photoListPhotoEntryDiv = new DivTag().AddClass("content-list-item-container");
                    photoListPhotoEntryDiv.Data("title", loopContentCommon.Title);
                    photoListPhotoEntryDiv.Data("tags",
                        string.Join(",", Db.TagListParseToSlugs(loopContentCommon, false)));
                    photoListPhotoEntryDiv.Data("summary", loopContentCommon.Summary);
                    photoListPhotoEntryDiv.Data("contenttype", TypeToFilterTag(loopContentCommon));

                    photoListPhotoEntryDiv.Children.Add(ContentCompact.FromContentCommon(loopContentCommon));

                    allContentContainer.Children.Add(photoListPhotoEntryDiv);
                }
                else if (loopContent is LinkStream loopLinkContent)
                {
                    var photoListPhotoEntryDiv = new DivTag().AddClass("content-list-item-container");

                    var titleList = new List<string>();

                    if (!string.IsNullOrWhiteSpace(loopLinkContent.Title)) titleList.Add(loopLinkContent.Title);
                    if (!string.IsNullOrWhiteSpace(loopLinkContent.Site)) titleList.Add(loopLinkContent.Site);
                    if (!string.IsNullOrWhiteSpace(loopLinkContent.Author)) titleList.Add(loopLinkContent.Author);

                    photoListPhotoEntryDiv.Data("title", string.Join(" - ", titleList));
                    photoListPhotoEntryDiv.Data("tags",
                        string.Join(",", Db.TagListParseToSlugs(loopLinkContent.Tags, false)));
                    photoListPhotoEntryDiv.Data("summary", $"{loopLinkContent.Description} {loopLinkContent.Comments}");
                    photoListPhotoEntryDiv.Data("contenttype", TypeToFilterTag(loopLinkContent));

                    photoListPhotoEntryDiv.Children.Add(ContentCompact.FromLinkStream(loopLinkContent));

                    allContentContainer.Children.Add(photoListPhotoEntryDiv);
                }

            return allContentContainer;
        }

        public HtmlTag FilterCheckboxesTag()
        {
            var allContent = ContentFunction();

            var filterTags = allContent.Select(TypeToFilterTag).Distinct().ToList();

            if (filterTags.Count() < 2) return HtmlTag.Empty();

            var filterContainer = new DivTag().AddClass("content-list-filter-container");

            var textInfo = new CultureInfo("en-US", false).TextInfo;

            foreach (var loopTag in filterTags)
            {
                var itemContainer = new DivTag().AddClass("content-list-filter-item");
                var checkbox = new CheckboxTag(false).Id($"{loopTag}-content-list-filter-checkbox")
                    .AddClass("content-list-filter-checkbox").Value(loopTag).Attr("onclick", "searchContent()");
                var label = new HtmlTag("label").AddClass("content-list-filter-checkbox-label")
                    .Text(textInfo.ToTitleCase(loopTag));
                itemContainer.Children.Add(checkbox);
                itemContainer.Children.Add(label);
                filterContainer.Children.Add(itemContainer);
            }

            return filterContainer;
        }

        private static string TypeToFilterTag(object content)
        {
            return content switch
            {
                NoteContent _ => "post",
                PostContent _ => "post",
                ImageContent _ => "image",
                PhotoContent _ => "image",
                FileContent _ => "file",
                LinkStream _ => "link",
                _ => "other"
            };
        }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo = settings.LocalSitePhotoListFile();

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}