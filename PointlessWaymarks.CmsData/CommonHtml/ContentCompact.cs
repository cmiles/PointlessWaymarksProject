﻿using HtmlTags;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class ContentCompact
{
    public static HtmlTag FromContentCommon(IContentCommon content)
    {
        var compactContentContainerDiv = new DivTag().AddClasses("content-compact-container", "info-box");

        var linkTo = UserSettingsSingleton.CurrentSettings().ContentUrl(content.ContentId).Result;

        if (content.MainPicture != null)
        {
            var compactContentMainPictureContentDiv =
                new DivTag().AddClass("content-compact-image-content-container");

            var image = new PictureSiteInformation(content.MainPicture.Value);

            compactContentMainPictureContentDiv.Children.Add(Tags.PictureImgThumbWithLink(image.Pictures, linkTo));

            compactContentContainerDiv.Children.Add(compactContentMainPictureContentDiv);
        }

        var compactContentMainTextContentDiv = new DivTag().AddClass("content-compact-text-content-container");

        var compactContentMainTextTitleTextDiv =
            new DivTag().AddClass("content-compact-text-content-title-container");
        var compactContentMainTextTitleLink =
            new LinkTag(content.Title, linkTo).AddClass("content-compact-text-content-title-link");
        compactContentMainTextTitleTextDiv.Children.Add(compactContentMainTextTitleLink);

        HtmlTag compactContentSummaryTextDiv;

        if (content.MainPicture == null)
            compactContentSummaryTextDiv = new DivTag().AddClass("content-compact-text-content-summary")
                .Text(content.Summary);
        else
            compactContentSummaryTextDiv = new DivTag().AddClass("content-compact-text-content-optional-summary")
                .Text(content.Summary);

        var compactContentMainTextCreatedOrUpdatedTextDiv = new DivTag()
            .AddClass("content-compact-text-content-date")
            .Text(Tags.LatestCreatedOnOrUpdatedOn(content)?.ToString("M/d/yyyy") ?? string.Empty);

        compactContentMainTextContentDiv.Children.Add(compactContentMainTextTitleTextDiv);
        compactContentMainTextContentDiv.Children.Add(compactContentSummaryTextDiv);
        compactContentMainTextContentDiv.Children.Add(compactContentMainTextCreatedOrUpdatedTextDiv);

        compactContentContainerDiv.Children.Add(compactContentMainTextContentDiv);

        return compactContentContainerDiv;
    }

    public static HtmlTag FromLinkContent(LinkContent content)
    {
        var compactContentContainerDiv = new DivTag().AddClasses("content-compact-container", "info-box");

        var compactContentMainTextContentDiv = new DivTag().AddClass("link-compact-text-content-container");

        var compactContentMainTextTitleTextDiv =
            new DivTag().AddClass("content-compact-text-content-title-container");
        var compactContentMainTextTitleLink =
            new LinkTag(string.IsNullOrWhiteSpace(content.Title) ? content.Url : content.Title, content.Url)
                .AddClass("content-compact-text-content-title-link");

        compactContentMainTextTitleTextDiv.Children.Add(compactContentMainTextTitleLink);

        var compactContentSummaryTextDiv = new DivTag().AddClass("link-compact-text-content-summary");

        var itemsPartOne = new List<string>();
        if (!string.IsNullOrWhiteSpace(content.Author)) itemsPartOne.Add(content.Author);
        if (content.LinkDate != null) itemsPartOne.Add(content.LinkDate.Value.ToString("M/d/yyyy"));
        if (content.LinkDate == null) itemsPartOne.Add($"Saved {content.CreatedOn:M/d/yyyy}");

        if (itemsPartOne.Any())
        {
            var textPartOneDiv = new DivTag().AddClass("content-compact-text-content-link-summary")
                .Text(string.Join(" - ", itemsPartOne));
            compactContentSummaryTextDiv.Children.Add(textPartOneDiv);
        }

        if (!string.IsNullOrWhiteSpace(content.Description))
        {
            var textPartThreeDiv = new DivTag().AddClass("content-compact-text-content-link-summary")
                .Text(content.Description);
            compactContentSummaryTextDiv.Children.Add(textPartThreeDiv);
        }

        if (!string.IsNullOrWhiteSpace(content.Comments))
        {
            var textPartTwoDiv = new DivTag().AddClass("content-compact-text-content-link-summary")
                .Text(content.Comments);
            compactContentSummaryTextDiv.Children.Add(textPartTwoDiv);
        }

        compactContentMainTextContentDiv.Children.Add(compactContentMainTextTitleTextDiv);
        compactContentMainTextContentDiv.Children.Add(compactContentSummaryTextDiv);

        compactContentContainerDiv.Children.Add(compactContentMainTextContentDiv);

        return compactContentContainerDiv;
    }
}