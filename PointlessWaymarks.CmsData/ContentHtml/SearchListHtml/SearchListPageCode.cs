using System.Globalization;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.SearchListHtml;

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
        DirAttribute = UserSettingsSingleton.CurrentSettings().SiteDirectionAttribute;
    }

    public bool AddNoIndexTag { get; set; }
    public Func<List<object>> ContentFunction { get; }

    public string DirAttribute { get; set; }
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
                allContentContainer.Children.Add(ContentList.FromContentCommon(loopContentCommon));
            }
            else if (loopContent is LinkContent loopLinkContent)
            {
                allContentContainer.Children.Add(ContentList.FromLinkContent(loopLinkContent));
            }

        return allContentContainer;
    }

    public HtmlTag FilterCheckboxesTag()
    {
        var allContent = ContentFunction();

        var filterTags = allContent.Select(ContentList.ContentTypeToContentListItemFilterTag).Distinct().ToList();

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

        await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString).ConfigureAwait(false);
    }
}