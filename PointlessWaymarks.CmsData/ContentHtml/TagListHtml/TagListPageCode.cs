using System.ComponentModel;
using HtmlTags;
using PointlessWaymarks.CmsData.Content;

namespace PointlessWaymarks.CmsData.ContentHtml.TagListHtml;

public partial class TagListPage
{
    public Func<List<(string tagName, List<object> tagCotentEntries)>>? ContentFunction { get; set; }
    public string? DirAttribute { get; set; }
    public DateTime? GenerationVersion { get; set; }
    public string? LangAttribute { get; set; }

    public HtmlTag TagList()
    {
        if (ContentFunction == null)
            throw new InvalidEnumArgumentException("Can not build a TagList with a null Content Function");

        var toProcess = ContentFunction().OrderBy(x => x.tagName);

        var tagListContainer = new DivTag().AddClass("tag-list");

        foreach (var loopTagGroup in toProcess)
        {
            var contentContainer = new DivTag().AddClasses("tag-list-item", "info-box");
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

        var htmlString = TransformText();

        var htmlFileInfo = settings.LocalSiteAllTagsListFileInfo();

        if (htmlFileInfo.Exists)
        {
            htmlFileInfo.Delete();
            htmlFileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString).ConfigureAwait(false);
    }
}