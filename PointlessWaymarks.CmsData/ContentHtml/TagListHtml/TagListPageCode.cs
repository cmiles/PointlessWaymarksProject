using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarks.CmsData.Content;

namespace PointlessWaymarks.CmsData.ContentHtml.TagListHtml
{
    public partial class TagListPage
    {
        public Func<List<(string tagName, List<object> tagCotentEntries)>>? ContentFunction { get; set; }
        public DateTime? GenerationVersion { get; set; }

        public string? LangAttribute { get; set; }
        public string? DirAttribute { get; set; }

        public HtmlTag TagList()
        {
            if (ContentFunction == null)
                throw new InvalidEnumArgumentException("Can not build a TagList with a null Content Function");

            var toProcess = ContentFunction().OrderBy(x => x.tagName);

            var tagListContainer = new DivTag().AddClass("tag-list");

            foreach (var loopTagGroup in toProcess)
            {
                var contentContainer = new DivTag().AddClasses("tag-list-item", "box-container");
                contentContainer.Data("tagname", loopTagGroup.tagName.Replace("-", " "));
                contentContainer.Data("tagslug", loopTagGroup.tagName);

                var contentLink =
                    new LinkTag($"{loopTagGroup.tagName.Replace("-", " ")} ({loopTagGroup.tagCotentEntries.Count})",
                            UserSettingsSingleton.CurrentSettings().TagPageUrl(loopTagGroup.tagName))
                        .AddClass("tag-list-link");

                contentContainer.Children.Add(contentLink);
                tagListContainer.Children.Add(contentContainer);
            }

            return tagListContainer;
        }

        public async Task WriteLocalHtml()
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

            await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString);
        }
    }
}