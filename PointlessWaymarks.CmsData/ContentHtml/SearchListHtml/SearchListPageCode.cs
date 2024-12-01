using System.Globalization;
using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

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

    public async Task<HtmlTag> ContentListTag()
    {
        var allContent = ContentFunction().OrderBy(x => (x as ITitle)?.Title).ToList();

        var allContentContainer = new DivTag().AddClass("content-list-container");

        foreach (var loopContent in allContent)
            if (loopContent is IContentCommon loopContentCommon)
                allContentContainer.Children.Add(await ContentList.FromContentCommon(loopContentCommon));
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
                .Attr("for", "main-feed-filter-checkbox")
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

        List<(string label, string sortMethod)> sortList;

        if (allContent.Any() && allContent.All(x => x is LineContent or TrailContent))
            sortList =
            [
                ("Title ↑", "sortTitleAscending"),
                ("Title ↓", "sortTitleDescending"),
                ("Distance ↑", "sortDistanceAscending"),
                ("Distance ↓", "sortDistanceDescending"),
                ("Climb ↑", "sortClimbAscending"),
                ("Climb ↓", "sortClimbDescending"),
                ("Max Elevation ↑", "sortMaxElevationAscending"),
                ("Max Elevation ↓", "sortMaxElevationDescending")
            ];
        else
            sortList =
            [
                ("Title ↑", "sortTitleAscending"),
                ("Title ↓", "sortTitleDescending"),
                ("Created ↑", "sortCreatedAscending"),
                ("Created ↓", "sortCreatedDescending"),
                ("Updated ↑", "sortUpdatedAscending"),
                ("Updated ↓", "sortUpdatedDescending")
            ];

        var first = true;

        foreach (var radioLoop in sortList)
        {
            var radioContainer = new DivTag().AddClass("content-list-filter-item");
            var radio = new HtmlTag("input")
                .Attr("type", "radio")
                .Attr("name", "content-sort-order")
                .Attr("id", radioLoop.sortMethod)
                .Attr("title", radioLoop.sortMethod.CamelCaseToSpacedString())
                .Attr("onclick", $"{radioLoop.sortMethod}()")
                .AddClass("content-list-filter-item")
                .AddClasses("content-list-filter-radio");
            if (first)
            {
                first = false;
                radio.Attr("checked");
            }

            var label = new HtmlTag("label")
                .Attr("for", radioLoop.sortMethod)
                .AddClasses("content-list-filter-radio-label")
                .Text(textInfo.ToTitleCase(radioLoop.label));
            radioContainer.Children.Add(radio);
            radioContainer.Children.Add(label);
            filterContainer.Children.Add(radioContainer);
        }

        if (allContent.Any() && allContent.All(x => x is TrailContent))
        {
            var filterList = new List<(string displayName, string dataType, bool value)>
                { ("Fees", "trail-fees", true), ("No Fees", "trail-fees", false), ("Dog Friendly", "trail-dogs", true), ("No Dogs", "trail-dogs", false), ("Bikes Allowed", "trail-bikes", true), ("No Bikes", "trail-bikes", false) };

            foreach (var loopFilters in filterList)
            {
                var checkboxId = $"{SlugTools.CreateSlug(true, loopFilters.displayName)}-trail-list-filter-checkbox";
                var trailFilterItemContainer = new DivTag().AddClass("trail-list-filter-item");
                var checkbox = new CheckboxTag(true)
                    .Id(checkboxId)
                    .AddClasses($"trail-list-{loopFilters.value.ToString().ToLower()}-filter-checkbox").Value(loopFilters.dataType)
                    .Attr("onclick", "processSearchContent()");
                var label = new HtmlTag("label")
                    .Attr("for", checkboxId)
                    .AddClasses("content-list-filter-checkbox-label")
                    .Text(textInfo.ToTitleCase(loopFilters.displayName));
                trailFilterItemContainer.Children.Add(checkbox);
                trailFilterItemContainer.Children.Add(label);
                filterContainer.Children.Add(trailFilterItemContainer);
            }

            var locations = allContent.Cast<TrailContent>().Where(x => !string.IsNullOrWhiteSpace(x.LocationArea))
                .Select(x => x.LocationArea).Distinct().OrderBy(x => x).ToList();

            if (locations.Any())
            {
                var selectTag = new SelectTag();

                foreach (var loopLocation in locations)
                    selectTag.Option(loopLocation, SlugTools.CreateSlug(true, loopLocation));

                selectTag.DefaultOption("All").Id("trail-location-filter-dropdown")
                    .AddClasses("trail-location-filter-dropdown").Attr("onchange", "processSearchContent()");

                filterContainer.Children.Add(selectTag);
            }
        }

        return filterContainer;
    }
}