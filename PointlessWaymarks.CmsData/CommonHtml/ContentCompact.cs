﻿using HtmlTags;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class ContentList
{
    public static HtmlTag FromContentCommon(IContentCommon content)
    {
        var listItemContainerDiv = new DivTag().AddClasses("content-list-item-container", "info-box");
        listItemContainerDiv.Data("title", content.Title);
        listItemContainerDiv.Data("tags",
            string.Join(",", Db.TagListParseToSlugs(content, false)));
        listItemContainerDiv.Data("summary", content.Summary);
        listItemContainerDiv.Data("contenttype", ContentTypeToContentListItemFilterTag(content));

        var linkTo = UserSettingsSingleton.CurrentSettings().ContentUrl(content.ContentId).Result;

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

        HtmlTag compactContentSummaryTextDiv;

        if (content.MainPicture == null)
            compactContentSummaryTextDiv = new DivTag().AddClass("compact-content-text-content-summary")
                .Text(content.Summary);
        else
            compactContentSummaryTextDiv = new DivTag().AddClass("compact-content-text-content-optional-summary")
                .Text(content.Summary);

        var compactContentMainTextCreatedOrUpdatedTextDiv = new DivTag()
            .AddClass("compact-content-text-content-date")
            .Text(Tags.LatestCreatedOnOrUpdatedOn(content)?.ToString("M/d/yyyy") ?? string.Empty);

        compactContentMainTextContentDiv.Children.Add(compactContentMainTextTitleTextDiv);
        compactContentMainTextContentDiv.Children.Add(compactContentSummaryTextDiv);
        compactContentMainTextContentDiv.Children.Add(compactContentMainTextCreatedOrUpdatedTextDiv);

        listItemContainerDiv.Children.Add(compactContentMainTextContentDiv);

        return listItemContainerDiv;
    }

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
            _ => "other"
        };
    }

    public static HtmlTag FromLinkContent(LinkContent content)
    {
        var photoListPhotoEntryDiv = new DivTag().AddClasses("content-list-item-container", "info-box");

        var titleList = new List<string>();

        if (!string.IsNullOrWhiteSpace(content.Title)) titleList.Add(content.Title);
        if (!string.IsNullOrWhiteSpace(content.Site)) titleList.Add(content.Site);
        if (!string.IsNullOrWhiteSpace(content.Author)) titleList.Add(content.Author);

        photoListPhotoEntryDiv.Data("title", string.Join(" - ", titleList));
        photoListPhotoEntryDiv.Data("tags",
            string.Join(",", Db.TagListParseToSlugs(content.Tags, false)));
        photoListPhotoEntryDiv.Data("summary", $"{content.Description} {content.Comments}");
        photoListPhotoEntryDiv.Data("contenttype", ContentTypeToContentListItemFilterTag(content));

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

        photoListPhotoEntryDiv.Children.Add(compactContentMainTextContentDiv);

        return photoListPhotoEntryDiv;
    }
}