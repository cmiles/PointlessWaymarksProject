using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;

namespace PointlessWaymarksCmsData.TagListHtml
{
    public partial class TagListPage
    {
        public Func<List<(string tagName, List<object> tagCotentEntries)>> ContentFunction { get; set; }

        public HtmlTag TagList()
        {
            var toProcess = ContentFunction().OrderBy(x => x.tagName);

            var tagListContainer = new DivTag().AddClass("tag-list");

            foreach (var loopTagGroup in toProcess)
            {
                var contentContainer = new DivTag().AddClass("tag-list-item");
                contentContainer.Data("tagname", loopTagGroup.tagName);

                var contentLink = new LinkTag($"{loopTagGroup.tagName} ({loopTagGroup.tagCotentEntries.Count})",
                    UserSettingsSingleton.CurrentSettings().TagPageUrl(loopTagGroup.tagName)).AddClass("tag-list-link");

                contentContainer.Children.Add(contentLink);
                tagListContainer.Children.Add(contentContainer);
            }

            return tagListContainer;
        }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo = settings.LocalSiteAllTagsListFileInfo();

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}