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

namespace PointlessWaymarksCmsData.ContentListHtml
{
    public partial class ContentListPage
    {
        public ContentListPage(string rssUrl)
        {
            RssUrl = rssUrl;
        }

        public Func<List<IContentCommon>> ContentFunction { get; set; }
        public string ListTitle { get; set; }

        public string RssUrl { get; set; }

        public HtmlTag ContentTableTag()
        {
            var allContent = ContentFunction();

            var allContentContainer = new DivTag().AddClass("content-list-container");

            foreach (var loopContent in allContent)
            {
                var photoListPhotoEntryDiv = new DivTag().AddClass("content-list-item-container");
                photoListPhotoEntryDiv.Data("title", loopContent.Title);
                photoListPhotoEntryDiv.Data("tags", loopContent.Tags);
                photoListPhotoEntryDiv.Data("summary", loopContent.Summary);
                photoListPhotoEntryDiv.Data("contenttype", TypeToFilterTag(loopContent));

                photoListPhotoEntryDiv.Children.Add(ContentCompact.FromContent(loopContent));

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