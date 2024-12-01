using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using SimMetricsCore;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class ContentList
{
    public static string ContentTypeToContentListItemFilterTag(object content)
    {
        return content switch
        {
            NoteContent => "post",
            PostContent => "post",
            ImageContent => "image",
            PhotoContent => "image",
            FileContent => "file",
            LinkContent => "link",
            TrailContent => "trail",
            VideoContent => "video",
            _ => "other"
        };
    }

    public static async Task<HtmlTag> FromContentCommon(IContentCommon content)
    {
        var linkTo = UserSettingsSingleton.CurrentSettings().ContentUrl(content.ContentId).Result;

        var listItemContainerDiv = new DivTag().AddClasses("content-list-item-container", "info-box");
        listItemContainerDiv.Data("title", content.Title);
        if (content is PhotoContent photoContent)
            listItemContainerDiv.Data("created", photoContent.PhotoCreatedOn.ToString("s"));
        else
            listItemContainerDiv.Data("created", content.CreatedOn.ToString("s"));
        listItemContainerDiv.Data("updated", (content.LastUpdatedOn ?? content.CreatedOn).ToString("s"));
        listItemContainerDiv.Data("tags",
            string.Join(",", Db.TagListParseToSlugs(content, false)));
        listItemContainerDiv.Data("summary", content.Summary);
        listItemContainerDiv.Data("site-main-feed", content.ShowInMainSiteFeed);
        listItemContainerDiv.Data("content-type", ContentTypeToContentListItemFilterTag(content));
        listItemContainerDiv.Data("target-url", linkTo);

        if (content is LineContent lineForData)
        {
            listItemContainerDiv.Data("distance", lineForData.LineDistance);
            listItemContainerDiv.Data("climb", lineForData.ClimbElevation);
            listItemContainerDiv.Data("descent", lineForData.DescentElevation);
            listItemContainerDiv.Data("min-elevation", lineForData.MinimumElevation);
            listItemContainerDiv.Data("max-elevation", lineForData.MaximumElevation);
        }

        LineContent? trailLine = null;
        PointContent? trailStart = null;
        PointContent? trailEnd = null;

        if (content is TrailContent trailForData)
        {
            listItemContainerDiv.Data("trail-fees", trailForData.Fees);
            listItemContainerDiv.Data("trail-bikes", trailForData.Bikes);
            listItemContainerDiv.Data("trail-dogs", trailForData.Dogs);
            if (!string.IsNullOrWhiteSpace(trailForData.LocationArea))
                listItemContainerDiv.Data("trail-location-area", SlugTools.CreateSlug(true, trailForData.LocationArea));

            if (trailForData.LineContentId is not null || trailForData.StartingPointContentId is not null ||
                trailForData.EndingPointContentId is not null)
            {
                var dbContext = await Db.Context();
                if (trailForData.LineContentId is not null)
                {
                    trailLine =
                        await dbContext.LineContents.FirstOrDefaultAsync(x =>
                            x.ContentId == trailForData.LineContentId);
                    if (trailLine is not null)
                    {
                        listItemContainerDiv.Data("distance", trailLine.LineDistance);
                        listItemContainerDiv.Data("climb", trailLine.ClimbElevation);
                        listItemContainerDiv.Data("descent", trailLine.DescentElevation);
                        listItemContainerDiv.Data("min-elevation", trailLine.MinimumElevation);
                        listItemContainerDiv.Data("max-elevation", trailLine.MaximumElevation);
                    }
                }

                if (trailForData.StartingPointContentId is not null)
                {
                    trailStart =
                        dbContext.PointContents.FirstOrDefault(x => x.ContentId == trailForData.StartingPointContentId);
                    if (trailStart is not null) listItemContainerDiv.Data("trail-start-point-title", trailStart.Title);
                }

                if (trailForData.EndingPointContentId is not null)
                {
                    trailEnd =
                        dbContext.PointContents.FirstOrDefault(x => x.ContentId == trailForData.EndingPointContentId);
                    if (trailEnd is not null) listItemContainerDiv.Data("trail-end-point-title", trailEnd.Title);
                }
            }
        }

        if (content.MainPicture != null)
        {
            var compactContentMainPictureContentDiv =
                new DivTag().AddClass("compact-content-image-content-container");

            var image = new PictureSiteInformation(content.MainPicture.Value);

            compactContentMainPictureContentDiv.Children.Add(Tags.PictureImgThumbWithLink(image.Pictures, linkTo));

            listItemContainerDiv.Children.Add(compactContentMainPictureContentDiv);
        }

        var compactContentMainTextContentDiv = new DivTag().AddClass("compact-content-text-content-container");

        var compactContentMainTextTitleTextDiv =
            new DivTag().AddClass("compact-content-text-content-title-container");
        var compactContentMainTextTitleLink =
            new LinkTag(content.Title, linkTo).AddClass("compact-content-text-content-title-link");
        compactContentMainTextTitleTextDiv.Children.Add(compactContentMainTextTitleLink);

        //Especially in automated imports the summary and title could end up the same - if they are blank the 
        //summary in the context of compact content.
        var summaryLines = new List<string>();

        if (!string.IsNullOrWhiteSpace(content.Summary))
        {
            var summaryIsInTitle = content.Title.ContainsFuzzy(content.Summary, 0.8, SimMetricType.JaroWinkler);
            if (!summaryIsInTitle) summaryLines.Add(content.Summary);
        }

        if (content is LineContent line) summaryLines.Add(LineParts.LineStatsString(line));
        if (content is TrailContent)
        {
            if (trailStart is not null && trailEnd is not null && trailStart.ContentId == trailEnd.ContentId)
            {
                summaryLines.Add($"Start/End: {trailStart.Title}");
            }
            else if (trailStart is not null && trailEnd is not null)
            {
                summaryLines.Add($"Start: {trailStart.Title} | End: {trailEnd.Title}");
            }

            if (trailStart is not null && trailEnd is null)
            {
                summaryLines.Add($"Start: {trailStart.Title}");
            }

            if (trailStart is null && trailEnd is not null)
            {
                summaryLines.Add($"End: {trailEnd.Title}");
            }

            if (trailLine != null)
            {
                summaryLines.Add(LineParts.LineStatsString(trailLine));
            }
        }

        if (!string.IsNullOrWhiteSpace(content.Tags)) summaryLines.Add($"Tags: {content.Tags}");

        var compactContentSummaryTextDiv = new DivTag().AddClass("compact-content-text-content-summary")
            .Text(string.Join("<br>", summaryLines)).Encoded(false);

        var compactContentMainTextCreatedOrUpdatedTextDiv = new DivTag()
            .AddClass("compact-content-text-content-date")
            .Text(Tags.LatestCreatedOnOrUpdatedOn(content)?.ToString("M/d/yyyy") ?? string.Empty);

        var compactContentTitleSummaryGroupDiv = new DivTag().AddClass("compact-content-title-summary-container");
        compactContentTitleSummaryGroupDiv.Children.Add(compactContentMainTextTitleTextDiv);
        compactContentTitleSummaryGroupDiv.Children.Add(compactContentSummaryTextDiv);

        compactContentMainTextContentDiv.Children.Add(compactContentTitleSummaryGroupDiv);
        compactContentMainTextContentDiv.Children.Add(compactContentMainTextCreatedOrUpdatedTextDiv);

        listItemContainerDiv.Children.Add(compactContentMainTextContentDiv);

        return listItemContainerDiv;
    }

    public static HtmlTag FromLinkContent(LinkContent content)
    {
        var linkListContent = new DivTag().AddClasses("content-list-item-container", "info-box");

        var titleList = new List<string>();

        if (!string.IsNullOrWhiteSpace(content.Title)) titleList.Add(content.Title);
        if (!string.IsNullOrWhiteSpace(content.Site)) titleList.Add(content.Site);
        if (!string.IsNullOrWhiteSpace(content.Author)) titleList.Add(content.Author);

        linkListContent.Data("title", string.Join(" - ", titleList));
        linkListContent.Data("created", content.CreatedOn.ToString("s"));
        linkListContent.Data("updated", (content.LastUpdatedOn ?? content.CreatedOn).ToString("s"));
        linkListContent.Data("tags",
            string.Join(",", Db.TagListParseToSlugs(content.Tags, false)));
        linkListContent.Data("summary", $"{content.Description} {content.Comments}");
        linkListContent.Data("site-main-feed", false);
        linkListContent.Data("content-type", ContentTypeToContentListItemFilterTag(content));

        var compactContentMainTextContentDiv = new DivTag().AddClass("link-compact-text-content-container");

        var compactContentMainTextTitleTextDiv =
            new DivTag().AddClass("compact-content-text-content-title-container");
        var compactContentMainTextTitleLink =
            new LinkTag(string.IsNullOrWhiteSpace(content.Title) ? content.Url : content.Title, content.Url)
                .AddClass("compact-content-text-content-title-link");

        compactContentMainTextTitleTextDiv.Children.Add(compactContentMainTextTitleLink);

        var compactContentSummaryTextDiv = new DivTag().AddClass("link-compact-text-content-summary");

        var itemsPartOne = new List<string>();
        if (!string.IsNullOrWhiteSpace(content.Author)) itemsPartOne.Add(content.Author);
        if (content.LinkDate != null) itemsPartOne.Add(content.LinkDate.Value.ToString("M/d/yyyy"));
        if (content.LinkDate == null) itemsPartOne.Add($"Saved {content.CreatedOn:M/d/yyyy}");

        if (itemsPartOne.Any())
        {
            var textPartOneDiv = new DivTag().AddClass("compact-content-text-content-link-summary")
                .Text(string.Join(" - ", itemsPartOne));
            compactContentSummaryTextDiv.Children.Add(textPartOneDiv);
        }

        if (!string.IsNullOrWhiteSpace(content.Description))
        {
            var textPartThreeDiv = new DivTag().AddClass("compact-content-text-content-link-summary")
                .Text(content.Description);
            compactContentSummaryTextDiv.Children.Add(textPartThreeDiv);
        }

        if (!string.IsNullOrWhiteSpace(content.Comments))
        {
            var textPartTwoDiv = new DivTag().AddClass("compact-content-text-content-link-summary")
                .Text(content.Comments);
            compactContentSummaryTextDiv.Children.Add(textPartTwoDiv);
        }

        compactContentMainTextContentDiv.Children.Add(compactContentMainTextTitleTextDiv);
        compactContentMainTextContentDiv.Children.Add(compactContentSummaryTextDiv);

        linkListContent.Children.Add(compactContentMainTextContentDiv);

        return linkListContent;
    }
}