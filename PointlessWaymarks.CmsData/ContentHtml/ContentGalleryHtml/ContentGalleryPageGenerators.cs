using System.Globalization;
using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using SimMetricsCore;

namespace PointlessWaymarks.CmsData.ContentHtml.ContentGalleryHtml;

public static class ContentGalleryPageGenerators
{
    public static HtmlTag ContentCardFromContentCommon(IContentCommon content)
    {
        var linkTo = UserSettingsSingleton.CurrentSettings().ContentUrl(content.ContentId).Result;

        var contentType = Db.ContentTypeDisplayString((dynamic)content);

        var listItemContainerDiv = new DivTag().AddClasses("info-box");
        listItemContainerDiv.Data("title", content.Title);
        listItemContainerDiv.Data("created", content.CreatedOn.ToString("s"));
        listItemContainerDiv.Data("updated", (content.LastUpdatedOn ?? content.CreatedOn).ToString("s"));
        listItemContainerDiv.Data("tags",
            string.Join(",", Db.TagListParseToSlugs(content, false)));
        listItemContainerDiv.Data("summary", content.Summary);
        listItemContainerDiv.Data("site-main-feed", content.ShowInMainSiteFeed);
        listItemContainerDiv.Data("target-url", linkTo);
        listItemContainerDiv.Data("content-type", contentType);

        var imageTag = HtmlTag.Empty();

        if (content.MainPicture != null)
        {
            var image = new PictureSiteInformation(content.MainPicture.Value);

            imageTag = Tags.PictureImgCardWithLink(image.Pictures, linkTo);
        }

        if (!imageTag.IsEmpty())
        {
            listItemContainerDiv.AddClass("cg-card-with-image");

            var compactContentMainPictureContentDiv =
                new DivTag().AddClass("cg-card-image-div");

            compactContentMainPictureContentDiv.Children.Add(imageTag);

            listItemContainerDiv.Children.Add(compactContentMainPictureContentDiv);
        }
        else
        {
            listItemContainerDiv.AddClass("cg-card-text-only");
        }

        var compactContentMainTextContentDiv = new DivTag().AddClass("cg-card-text-div");

        var compactContentMainTextTitleTextDiv =
            new DivTag().AddClass("cg-card-text-title-div");
        var compactContentMainTextTitleLink =
            new LinkTag(content.Title, linkTo).AddClass("cg-card-text-title-link");
        compactContentMainTextTitleTextDiv.Children.Add(compactContentMainTextTitleLink);
        compactContentMainTextContentDiv.Children.Add(compactContentMainTextTitleTextDiv);

        //Especially in automated imports the summary and title could end up the same - if they are blank the 
        //summary in the context of compact content.
        var summaryLines = new List<string>();

        if (!string.IsNullOrWhiteSpace(content.Summary))
        {
            var summaryIsInTitle = content.Title.ContainsFuzzy(content.Summary, 0.8, SimMetricType.JaroWinkler);
            if (!summaryIsInTitle) summaryLines.Add(content.Summary);
        }

        if (content is LineContent line) summaryLines.Add(LineParts.LineStatsString(line));

        if (!string.IsNullOrWhiteSpace(content.Tags)) summaryLines.Add($"Tags: {content.Tags}");

        summaryLines.ForEach(x =>
            compactContentMainTextContentDiv.Children.Add(new DivTag().Text(x).AddClass("cg-card-text")));

        listItemContainerDiv.Children.Add(compactContentMainTextContentDiv);

        var footerDiv = new DivTag().AddClass("cg-card-footer-div");

        footerDiv.Children.Add(new DivTag().AddClass("cg-card-footer-type").Text(contentType));
        if (content.LastUpdatedOn != null) footerDiv.Children.Add(new DivTag().AddClass("cg-card-footer-date").Text($"Updated {content.LastUpdatedOn:M/d/yyyy}"));
        footerDiv.Children.Add(new DivTag().AddClass("cg-card-footer-date").Text($"Created {content.CreatedOn:M/d/yyyy}"));

        listItemContainerDiv.Children.Add(footerDiv);

        return listItemContainerDiv;
    }

    public static HtmlTag ContentCardFromLinkContent(LinkContent content)
    {
        var listItemContainerDiv = new DivTag().AddClasses("info-box");

        var titleList = new List<string>();
        if (!string.IsNullOrWhiteSpace(content.Title)) titleList.Add(content.Title);
        if (!string.IsNullOrWhiteSpace(content.Site)) titleList.Add(content.Site);
        if (!string.IsNullOrWhiteSpace(content.Author)) titleList.Add(content.Author);

        listItemContainerDiv.Data("title", string.Join(" - ", titleList));
        listItemContainerDiv.Data("created", content.CreatedOn.ToString("s"));
        listItemContainerDiv.Data("updated", (content.LastUpdatedOn ?? content.CreatedOn).ToString("s"));
        listItemContainerDiv.Data("tags", string.Join(",", Db.TagListParseToSlugs(content.Tags, false)));
        listItemContainerDiv.Data("summary", $"{content.Description} {content.Comments} {content.Url}");
        listItemContainerDiv.Data("site-main-feed", false);
        listItemContainerDiv.Data("content-type", "Link");

        listItemContainerDiv.AddClass("cg-card-text-only");
        var compactContentMainTextContentDiv = new DivTag().AddClass("cg-card-text-div");

        var compactContentMainTextTitleTextDiv =
            new DivTag().AddClass("cg-card-text-title-div");
        var compactContentMainTextTitleLink =
            new LinkTag(string.IsNullOrWhiteSpace(content.Title) ? content.Url : content.Title, content.Url)
                .AddClass("cg-card-text-title-link");
        compactContentMainTextTitleTextDiv.Children.Add(compactContentMainTextTitleLink);
        compactContentMainTextContentDiv.Children.Add(compactContentMainTextTitleTextDiv);

        var summaryLines = new List<string>();

        if (!string.IsNullOrWhiteSpace(content.Description)) summaryLines.Add(content.Description);
        if (!string.IsNullOrWhiteSpace(content.Comments)) summaryLines.Add(content.Comments);

        var addedInformation = new List<string>();
        if (!string.IsNullOrWhiteSpace(content.Author)) addedInformation.Add(content.Author);
        if (content.LinkDate != null) addedInformation.Add(content.LinkDate.Value.ToString("M/d/yyyy"));

        if (addedInformation.Any()) summaryLines.Add(string.Join(" - ", addedInformation));

        if (!string.IsNullOrWhiteSpace(content.Tags)) summaryLines.Add($"Tags: {content.Tags}");

        summaryLines.ForEach(x =>
            compactContentMainTextContentDiv.Children.Add(new DivTag().Text(x).AddClass("cg-card-text")));

        listItemContainerDiv.Children.Add(compactContentMainTextContentDiv);

        var footerDiv = new DivTag().AddClass("cg-card-footer-div");

        footerDiv.Children.Add(new DivTag().AddClass("cg-card-footer-type").Text("Link"));
        if(content.LastUpdatedOn != null) footerDiv.Children.Add(new DivTag().AddClass("cg-card-footer-date").Text($"Updated {content.LastUpdatedOn:M/d/yyyy}"));
        footerDiv.Children.Add(new DivTag().AddClass("cg-card-footer-date").Text($"Created {content.CreatedOn:M/d/yyyy}"));

        listItemContainerDiv.Children.Add(footerDiv);

        return listItemContainerDiv;
    }

    public static async Task<ContentGalleryPage> LatestContentGallery(DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        var db = await Db.Context().ConfigureAwait(false);

        progress?.Report("Starting Content Gallery Generation");

        async Task<List<DateOnly>> DateList()
        {
            //!!Content Type List!!
            var fileContent = await db.FileContents.Where(x => !x.IsDraft && x.ShowInSearch).Select(x => x.LastUpdatedOn ?? x.CreatedOn.Date)
                .ToListAsync();
            var geoJsonContent = await db.GeoJsonContents.Where(x => !x.IsDraft && x.ShowInSearch).Select(x => x.LastUpdatedOn ?? x.CreatedOn.Date)
                .ToListAsync();
            var imageContent = await db.ImageContents.Where(x => !x.IsDraft && x.ShowInSearch)
                .Select(x => x.LastUpdatedOn ?? x.CreatedOn.Date).ToListAsync();
            var lineContent = await db.LineContents.Where(x => !x.IsDraft && x.ShowInSearch).Select(x => x.LastUpdatedOn ?? x.CreatedOn.Date)
                .ToListAsync();
            var noteContent = await db.NoteContents.Where(x => !x.IsDraft && x.ShowInSearch).Select(x => x.LastUpdatedOn ?? x.CreatedOn.Date)
                .ToListAsync();
            var photoContent = await db.PhotoContents.Where(x => !x.IsDraft && x.ShowInSearch).Select(x => x.LastUpdatedOn ?? x.CreatedOn.Date)
                .ToListAsync();
            var pointContent = await db.PointContents.Where(x => !x.IsDraft && x.ShowInSearch).Select(x => x.LastUpdatedOn ?? x.CreatedOn.Date)
                .ToListAsync();
            var postContent = await db.PostContents.Where(x => !x.IsDraft && x.ShowInSearch).Select(x => x.LastUpdatedOn ?? x.CreatedOn.Date)
                .ToListAsync();
            var trailContent = await db.TrailContents.Where(x => !x.IsDraft && x.ShowInSearch).Select(x => x.LastUpdatedOn ?? x.CreatedOn.Date)
                .ToListAsync();
            var videoContent = await db.VideoContents.Where(x => !x.IsDraft && x.ShowInSearch).Select(x => x.LastUpdatedOn ?? x.CreatedOn.Date)
                .ToListAsync();

            return fileContent.Concat(geoJsonContent).Concat(imageContent).Concat(lineContent).Concat(noteContent)
                .Concat(photoContent).Concat(pointContent).Concat(postContent).Concat(trailContent).Concat(videoContent)
                .Select(DateOnly.FromDateTime).Distinct().OrderByDescending(x => x).ToList();
        }

        var allDates = await DateList();

        progress?.Report($"Found {allDates.Count} Dates with Photos for Content Roll");

        if (allDates.Count == 0)
            return new ContentGalleryPage
            {
                CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
                PageUrl = UserSettingsSingleton.CurrentSettings().LatestContentGalleryUrl(),
                ItemContentTag = new NoTag(),
                SiteName = UserSettingsSingleton.CurrentSettings().SiteName,
                LastDateGroupDateTime = DateTime.MinValue,
                MainImage = null,
                GenerationVersion = generationVersion,
                LangAttribute = UserSettingsSingleton.CurrentSettings().SiteLangAttribute,
                DirAttribute = UserSettingsSingleton.CurrentSettings().SiteDirectionAttribute
            };

        var loopGoal = allDates.Count;

        var contentContainer = new DivTag().AddClass("content-gallery-list");

        PictureSiteInformation? mainImage = null;
        var isFirstPictureItem = true;
        var currentYear = -1;
        var currentMonth = -1;

        var yearList = allDates.Select(x => x.Year).Distinct().OrderByDescending(x => x).ToList();

        var createdByList = new List<string>();

        for (var i = 0; i < allDates.Count; i++)
        {
            var loopDate = allDates[i];

            var newYear = false;
            var newMonth = false;

            if (loopDate.Year != currentYear)
            {
                newYear = true;
                currentYear = loopDate.Year;

                var yearNavigationDiv = new DivTag().AddClass("content-gallery-year-list-container");

                var yearLabelContainer = new DivTag().AddClass("content-gallery-year-list-item");
                var yearLabel = new DivTag().Text("Years:").AddClass("content-gallery-year-list-label");
                yearLabelContainer.Children.Add(yearLabel);
                yearNavigationDiv.Children.Add(yearLabelContainer);

                foreach (var loopYear in yearList)
                {
                    var yearContainer = new DivTag().AddClass("content-gallery-year-list-item");

                    if (loopYear == currentYear)
                    {
                        var activeYearContent = new DivTag().Text(loopYear.ToString())
                            .AddClass("content-gallery-year-list-content")
                            .AddClass("content-gallery-nav-current-selection");
                        yearContainer.Children.Add(activeYearContent);
                    }
                    else
                    {
                        var activeYearContent =
                            new LinkTag(loopYear.ToString(), $"#{loopYear}").AddClass(
                                "content-gallery-year-list-content");
                        yearContainer.Children.Add(activeYearContent);
                    }

                    yearNavigationDiv.Children.Add(yearContainer);
                }

                contentContainer.Children.Add(yearNavigationDiv);
            }

            if (loopDate.Month != currentMonth || newYear)
            {
                newMonth = true;
                currentMonth = loopDate.Month;

                var currentYearMonths = allDates.Where(x => x.Year == currentYear).Select(x => x.Month).Distinct()
                    .OrderBy(x => x).ToList();

                var monthNavigationDiv = new DivTag().AddClass("content-gallery-month-list-container");

                var monthLabelContainer = new DivTag().AddClass("content-gallery-month-list-item");
                var monthLabel = new DivTag(currentYear.ToString()).Text("Months:")
                    .AddClass("content-gallery-month-list-label");
                monthLabelContainer.Children.Add(monthLabel);
                monthNavigationDiv.Children.Add(monthLabelContainer);

                for (var j = 1; j < 13; j++)
                {
                    var monthContainer = new DivTag().AddClass("content-gallery-month-list-item");

                    var monthText = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(j);

                    if (j == currentMonth)
                    {
                        monthContainer.Id($"{currentYear}-{currentMonth}");
                        var activeMonthContent = new DivTag().Text(monthText)
                            .AddClass("content-gallery-month-list-content")
                            .AddClass("content-gallery-nav-current-selection");
                        monthContainer.Children.Add(activeMonthContent);
                    }
                    else if (!currentYearMonths.Contains(j))
                    {
                        var activeMonthContent = new DivTag().Text(monthText)
                            .AddClass("content-gallery-month-list-content")
                            .AddClass("content-gallery-nav-unused-selection");
                        monthContainer.Children.Add(activeMonthContent);
                    }
                    else
                    {
                        var activeMonthContent =
                            new LinkTag(monthText, $"#{currentYear}-{j}").AddClass(
                                "content-gallery-month-list-content");
                        monthContainer.Children.Add(activeMonthContent);
                    }

                    monthNavigationDiv.Children.Add(monthContainer);
                }

                contentContainer.Children.Add(monthNavigationDiv);
            }

            if (i % 10 == 0) progress?.Report($"Content Gallery Section - {loopDate:D} - {i} of {loopGoal}");

            var dateContent = await Db.ContentLastUpdatedCreatedOnDayNoDrafts(loopDate.ToDateTime(TimeOnly.MinValue));
            dateContent.Reverse();

            var infoItem = new DivTag().AddClass("content-gallery-info-item-container");

            var dateLink = new HtmlTag("p").Text($"{loopDate:yyyy MMMM d, dddd}")
                .AddClass("content-gallery-info-date-link");
            var dateDiv = new DivTag().AddClass("content-gallery-info-date");

            if (newMonth) dateDiv.Id($"{currentYear}-{currentMonth}");

            dateDiv.Children.Add(dateLink);
            infoItem.Children.Add(dateDiv);

            contentContainer.Children.Add(infoItem);

            foreach (var loopContent in dateContent)
            {
                if (loopContent is IContentCommon loopContentCommon)
                {
                    contentContainer.Children.Add(ContentCardFromContentCommon(loopContentCommon));
                    if (!string.IsNullOrWhiteSpace(loopContentCommon.CreatedBy) &&
                        !createdByList.Contains(loopContentCommon.CreatedBy,
                            StringComparer.InvariantCultureIgnoreCase))
                        createdByList.Add(loopContentCommon.CreatedBy);
                }
                else if (loopContent is LinkContent loopLinkContent)
                {
                    contentContainer.Children.Add(ContentCardFromLinkContent(loopLinkContent));
                    if (!string.IsNullOrWhiteSpace(loopLinkContent.CreatedBy) &&
                        !createdByList.Contains(loopLinkContent.CreatedBy,
                            StringComparer.InvariantCultureIgnoreCase))
                        createdByList.Add(loopLinkContent.CreatedBy);
                }

                if (isFirstPictureItem && loopContent is IContentCommon { MainPicture: not null } commonContent)
                {
                    isFirstPictureItem = false;
                    mainImage = new PictureSiteInformation(commonContent.MainPicture.Value);
                }
            }
        }

        var toReturn = new ContentGalleryPage
        {
            CreatedBy = string.Join(",", createdByList),
            PageUrl = UserSettingsSingleton.CurrentSettings().LatestContentGalleryUrl(),
            ItemContentTag = contentContainer,
            SiteName = UserSettingsSingleton.CurrentSettings().SiteName,
            LastDateGroupDateTime = allDates.First().ToDateTime(TimeOnly.MinValue),
            MainImage = mainImage,
            GenerationVersion = generationVersion,
            LangAttribute = UserSettingsSingleton.CurrentSettings().SiteLangAttribute,
            DirAttribute = UserSettingsSingleton.CurrentSettings().SiteDirectionAttribute
        };

        return toReturn;
    }
}