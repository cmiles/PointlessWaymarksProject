using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.SearchListHtml
{
    public partial class SearchListPage
    {
        public SearchListPage(string rssUrl, Func<List<object>> contentFunction, string listTitle,
            DateTime? generationVersion)
        {
            RssUrl = rssUrl;
            ContentFunction = contentFunction;
            ListTitle = listTitle;
            GenerationVersion = generationVersion;
            LangAttribute = UserSettingsSingleton.CurrentSettings().SiteLangAttribute;
        }

        public bool AddNoIndexTag { get; set; }
        public Func<List<object>> ContentFunction { get; }
        public DateTime? GenerationVersion { get; }

        public string LangAttribute { get; set; }
        public string ListTitle { get; }
        public string RssUrl { get; }

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
                else if (loopContent is LinkContent loopLinkContent)
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

                    photoListPhotoEntryDiv.Children.Add(ContentCompact.FromLinkContent(loopLinkContent));

                    allContentContainer.Children.Add(photoListPhotoEntryDiv);
                }

            return allContentContainer;
        }

        public HtmlTag FilterCheckboxesTag()
        {
            var allContent = ContentFunction();

            var filterTags = allContent.Select(TypeToFilterTag).Distinct().ToList();

            if (filterTags.Count < 2) return HtmlTag.Empty();

            var filterContainer = new DivTag().AddClass("content-list-filter-container");

            var textInfo = new CultureInfo("en-US", false).TextInfo;

            foreach (var loopTag in filterTags)
            {
                var itemContainer = new DivTag().AddClass("content-list-filter-item");
                var checkbox = new CheckboxTag(false).Id($"{loopTag}-content-list-filter-checkbox")
                    .AddClasses("content-list-filter-checkbox", "enable-after-loading", "wait-cursor").Value(loopTag)
                    .Attr("onclick", "searchContent()").BooleanAttr("disabled");
                var label = new HtmlTag("label")
                    .AddClasses("content-list-filter-checkbox-label", "enable-after-loading", "wait-cursor")
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
                NoteContent => "post",
                PostContent => "post",
                ImageContent => "image",
                PhotoContent => "image",
                FileContent => "file",
                LinkContent => "link",
                _ => "other"
            };
        }

        public async Task WriteLocalHtml()
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

            await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString);
        }
    }
}