using System.Globalization;
using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.SearchListHtml;

public partial class SearchListPage(
    string rssUrl,
    Func<List<object>> contentFunction,
    string listTitle,
    DateTime? generationVersion)
{
    public bool AddNoIndexTag { get; set; }
    public Func<List<object>> ContentFunction { get; } = contentFunction;
    public string DirAttribute { get; set; } = UserSettingsSingleton.CurrentSettings().SiteDirectionAttribute;
    public DateTime? GenerationVersion { get; } = generationVersion;
    public string LangAttribute { get; set; } = UserSettingsSingleton.CurrentSettings().SiteLangAttribute;
    public string ListTitle { get; } = listTitle;
    public string RssUrl { get; } = rssUrl;

    public HtmlTag ContentListTag()
    {
        var allContent = ContentFunction();

        var allContentContainer = new DivTag().AddClass("content-list-container");

        foreach (var loopContent in allContent)
            if (loopContent is IContentCommon loopContentCommon)
                allContentContainer.Children.Add(ContentList.FromContentCommon(loopContentCommon));
            else if (loopContent is LinkContent loopLinkContent)
                allContentContainer.Children.Add(ContentList.FromLinkContent(loopLinkContent));

        return allContentContainer;
    }

    public HtmlTag FilterCheckboxesTag()
    {
        var allContent = ContentFunction();

        var filterTypeTags = allContent.Select(ContentList.ContentTypeToContentListItemFilterTag).Distinct().ToList();

        var filterContainer = new DivTag().AddClass("content-list-filter-container wait-cursor enable-after-loading");

        var textInfo = new CultureInfo("en-US", false).TextInfo;

        var contentHasMainSiteFeedEntries = allContent.Any(x =>
            x is IContentCommon { ShowInMainSiteFeed: true });

        if (contentHasMainSiteFeedEntries)
        {
            var checkBoxContainer = new DivTag().AddClass("content-list-filter-item");
            var checkbox = new CheckboxTag(false).Id("main-feed-filter-checkbox")
                .AddClasses("site-main-feed-filter-checkbox").Value("site-main-feed")
                .Attr("onclick", "searchContent()");
            var label = new HtmlTag("label")
                .AddClasses("content-list-filter-checkbox-label")
                .Text("Main Page");
            checkBoxContainer.Children.Add(checkbox);
            checkBoxContainer.Children.Add(label);
            filterContainer.Children.Add(checkBoxContainer);
        }

        if (filterTypeTags.Count > 1)
        {
            foreach (var loopTag in filterTypeTags)
            {
                var checkBoxContainer = new DivTag().AddClass("content-list-filter-item");
                var checkbox = new CheckboxTag(false).Id($"{loopTag}-content-list-filter-checkbox")
                    .AddClasses("content-list-filter-checkbox").Value(loopTag)
                    .Attr("onclick", "processSearchContent()");
                var label = new HtmlTag("label")
                    .AddClasses("content-list-filter-checkbox-label")
                    .Text(textInfo.ToTitleCase(loopTag));
                checkBoxContainer.Children.Add(checkbox);
                checkBoxContainer.Children.Add(label);
                filterContainer.Children.Add(checkBoxContainer);
            }

            filterContainer.Children.Add(new DivTag().Text("|"));
        }

        var sortList = new List<(string label, string sortMethod)>
        {
            ("Title ↑", "sortTitleAscending"),
            ("Title ↓", "sortTitleDescending"),
            ("Created ↑", "sortCreatedAscending"),
            ("Created ↓", "sortCreatedDescending"),
            ("Updated ↑", "sortUpdatedAscending"),
            ("Updated ↓", "sortUpdatedDescending")
        };

        var first = true;

        foreach (var radioLoop in sortList)
        {
            var radioContainer = new DivTag().AddClass("content-list-filter-item");
            var radio = new HtmlTag("input")
                .Attr("type", "radio")
                .Attr("name", "content-sort-order")
                .Attr("onclick", $"{radioLoop.sortMethod}()")
                .AddClass("content-list-filter-item")
                .AddClasses("content-list-filter-radio");
            if (first)
            {
                first = false;
                radio.Attr("checked");
            }

            var label = new HtmlTag("label")
                .AddClasses("content-list-filter-radio-label")
                .Text(textInfo.ToTitleCase(radioLoop.label));
            radioContainer.Children.Add(radio);
            radioContainer.Children.Add(label);
            filterContainer.Children.Add(radioContainer);
        }

        return filterContainer;
    }
}