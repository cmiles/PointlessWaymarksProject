using HtmlTags;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using SimMetricsCore;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class Tags
{
    public static async Task<HtmlTag> CoreLinksDiv(IProgress<string>? progress = null)
    {
        var db = Db.Context().Result;

        var items = db.MenuLinks.OrderBy(x => x.MenuOrder).ToList();

        if (!items.Any()) return HtmlTag.Empty();

        var coreLinksDiv = new HtmlTag("nav").AddClass("core-links-container");

        foreach (var loopItems in items)
        {
            var html = ContentProcessing.ProcessContent(
                await BracketCodeCommon.ProcessCodesForSite(loopItems.LinkTag ?? string.Empty, progress)
                    .ConfigureAwait(false),
                ContentFormatEnum.MarkdigMarkdown01);

            var coreLinkContainer = new DivTag().AddClass("core-links-item").Text(html).Encoded(false);
            coreLinksDiv.Children.Add(coreLinkContainer);
        }

        return coreLinksDiv;
    }

    /// <summary>
    ///     Creates a comma separated list of names from the Created By and Last Updated By
    /// </summary>
    /// <param name="dbEntry"></param>
    /// <returns></returns>
    public static string CreatedByAndUpdatedByNameList(ICreatedAndLastUpdateOnAndBy dbEntry)
    {
        var nameList = new List<string>();

        if (!string.IsNullOrWhiteSpace(dbEntry.CreatedBy)) nameList.Add(dbEntry.CreatedBy);

        if (!string.IsNullOrWhiteSpace(dbEntry.LastUpdatedBy) && (string.IsNullOrWhiteSpace(dbEntry.CreatedBy) ||
                                                                  !dbEntry.CreatedBy.Equals(dbEntry.LastUpdatedBy,
                                                                      StringComparison.OrdinalIgnoreCase)))
            nameList.Add(dbEntry.LastUpdatedBy);

        return string.Join(",", nameList);
    }

    public static HtmlTag CreatedByAndUpdatedOnDiv(ICreatedAndLastUpdateOnAndBy dbEntry)
    {
        var titleContainer = new DivTag().AddClass("created-and-updated-container");
        titleContainer.Children.Add(new DivTag().AddClass("created-and-updated-content")
            .Text(CreatedByAndUpdatedOnString(dbEntry)));
        return titleContainer;
    }

    public static string CreatedByAndUpdatedOnFormattedDateTimeString(DateTime toFormat)
    {
        return toFormat.ToString("M/d/yyyy");
    }

    public static string CreatedByAndUpdatedOnString(ICreatedAndLastUpdateOnAndBy dbEntry)
    {
        var createdUpdatedString = $"Created by {dbEntry.CreatedBy}";

        var onlyCreated = false;

        if (dbEntry.LastUpdatedOn != null && dbEntry.CreatedOn.Date == dbEntry.LastUpdatedOn.Value.Date)
            if (string.Equals(dbEntry.CreatedBy, dbEntry.LastUpdatedBy, StringComparison.OrdinalIgnoreCase))
            {
                createdUpdatedString = $"Created and Updated by {dbEntry.LastUpdatedBy}";
                onlyCreated = true;
            }

        createdUpdatedString += $" on {CreatedByAndUpdatedOnFormattedDateTimeString(dbEntry.CreatedOn)}.";

        if (onlyCreated) return createdUpdatedString.Trim();

        if (string.IsNullOrWhiteSpace(dbEntry.LastUpdatedBy) && dbEntry.LastUpdatedOn == null)
            return createdUpdatedString;

        var updatedString = " Updated";

        if (!string.IsNullOrWhiteSpace(dbEntry.LastUpdatedBy) && dbEntry.CreatedBy != dbEntry.LastUpdatedBy)
            updatedString += $" by {dbEntry.LastUpdatedBy}";

        if (dbEntry.LastUpdatedOn != null)
            updatedString += $" on {CreatedByAndUpdatedOnFormattedDateTimeString(dbEntry.LastUpdatedOn.Value)}";

        updatedString += ".";

        return (createdUpdatedString + updatedString).Trim();
    }

    public static string CssStyleFileString()
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        return $"<link rel=\"stylesheet\" href=\"{settings.CssMainStyleFileUrl()}?v=1.0\">";
    }

    public static HtmlTag EmailCenterTableTag(HtmlTag tagToCenter)
    {
        if (tagToCenter.IsEmpty()) return HtmlTag.Empty();

        var emailCenterTable = (TableTag)new TableTag().Attr("width", "94%").Attr("margin", "10").Attr("border", "0")
            .Attr("cellspacing", "0").Attr("cellpadding", "0");

        var topMarginRow = (TableRowTag)emailCenterTable.AddBodyRow().Attr("height", "10");
        var topMarginCell = topMarginRow.Cell();
        topMarginCell.Text("&nbsp;").Encoded(false);

        var emailImageRow = emailCenterTable.AddBodyRow();

        emailImageRow.Cell().Attr("max-width", "1%").Attr("align", "center").Attr("valign", "top").Text("&nbsp;")
            // ReSharper disable once MustUseReturnValue - Does not appear to be an accurate annotation?
            .Encoded(false);

        var emailCenterContentCell =
            emailImageRow.Cell().Attr("width", "100%").Attr("align", "center").Attr("valign", "top");

        emailCenterContentCell.Children.Add(tagToCenter);

        emailImageRow.Cell().Attr("max-width", "1%").Attr("align", "center")
            // ReSharper disable once MustUseReturnValue  - Does not appear to be an accurate annotation?
            .Attr("valign", "top").Text("&nbsp;").Encoded(false);

        //var bottomMarginRow = emailCenterTable.AddBodyRow();
        //bottomMarginRow.Attr("height", "10");
        //var bottomMarginCell = bottomMarginRow.Cell();
        //bottomMarginCell.Text("&nbsp;").Encoded(false);

        return emailCenterTable;
    }

    public static string FavIconFileString()
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        return $"<link rel=\"shortcut icon\" href=\"{settings.FaviconUrl()}\"/>";
    }

    public static string ImageCaptionText(ImageContent dbEntry, bool includeTitle = false)
    {
        var summaryString = (includeTitle ? dbEntry.Title : string.Empty).TrimNullToEmpty();

        if (!string.IsNullOrWhiteSpace(dbEntry.Summary))
        {
            if (includeTitle) summaryString += ": ";
            summaryString += $"{dbEntry.Summary.Trim()}";
        }

        if (!char.IsPunctuation(summaryString[^1]))
            summaryString += ".";

        return summaryString;
    }

    public static HtmlTag ImageFigCaptionTag(ImageContent dbEntry, bool includeTitle = false)
    {
        if (string.IsNullOrWhiteSpace(dbEntry.Summary)) return HtmlTag.Empty();

        var figCaptionTag = new HtmlTag("figcaption");
        figCaptionTag.AddClass("single-image-caption");

        figCaptionTag.Text(ImageCaptionText(dbEntry, includeTitle));

        return figCaptionTag;
    }

    public static HtmlTag InfoLinkDivTag(string url, string linkText, string className, string dataType,
        string? dataValue)
    {
        if (string.IsNullOrWhiteSpace(linkText) || string.IsNullOrWhiteSpace(url)) return HtmlTag.Empty();
        var divTag = new HtmlTag("div");
        divTag.AddClasses(className, "info-box");

        var spanTag = new LinkTag(linkText, url) as HtmlTag;
        spanTag = spanTag.AddClasses("info-list-link-item", $"{className}-content").Data(dataType, dataValue);

        divTag.Children.Add(spanTag);

        return divTag;
    }

    public static HtmlTag InfoLinkDivTag(string url, string linkText, string className)
    {
        if (string.IsNullOrWhiteSpace(linkText) || string.IsNullOrWhiteSpace(url)) return HtmlTag.Empty();
        var divTag = new HtmlTag("div");
        divTag.AddClasses(className, "info-box");

        var spanTag = new LinkTag(linkText, url) as HtmlTag;
        spanTag = spanTag.AddClasses("info-list-link-item", $"{className}-content");

        divTag.Children.Add(spanTag);

        return divTag;
    }

    public static HtmlTag InfoLinkDownloadDivTag(string url, string linkText, string className, string? downloadAttributeValue)
    {
        if (string.IsNullOrWhiteSpace(linkText) || string.IsNullOrWhiteSpace(url)) return HtmlTag.Empty();
        var divTag = new HtmlTag("div");
        divTag.AddClasses(className, "info-box");

        var spanTag = new LinkTag(linkText, url) as HtmlTag;
        spanTag = spanTag.AddClasses("info-list-link-item", $"{className}-content");

        if (!string.IsNullOrWhiteSpace(downloadAttributeValue))
            spanTag.Attr("download", downloadAttributeValue);

        divTag.Children.Add(spanTag);

        return divTag;
    }

    public static HtmlTag InfoTextDivTag(string? contents, string className, string dataType, string? dataValue)
    {
        if (string.IsNullOrWhiteSpace(contents)) return HtmlTag.Empty();
        var divTag = new HtmlTag("div");
        divTag.AddClasses(className, "info-box");

        var spanTag = new HtmlTag("p");
        spanTag.Text(contents.Trim());
        spanTag.AddClasses("info-list-text-item", $"{className}-content");
        spanTag.Data(dataType, dataValue);

        divTag.Children.Add(spanTag);

        return divTag;
    }

    public static bool IsEmpty(this HtmlTag toCheck)
    {
        return string.IsNullOrWhiteSpace(toCheck.ToHtmlString());
    }

    public static DateTime? LatestCreatedOnOrUpdatedOn(ICreatedAndLastUpdateOnAndBy? dbEntry)
    {
        if (dbEntry == null) return null;

        return dbEntry.LastUpdatedOn ?? dbEntry.CreatedOn;
    }

    public static (List<IContentCommon> previousContent, List<IContentCommon> laterContent)
        MainFeedPreviousAndLaterContent(int numberOfPreviousAndLater, DateTime createdOn)
    {
        var previousContent = Db.MainFeedCommonContentBefore(createdOn, numberOfPreviousAndLater).Result;
        var laterContent = Db.MainFeedCommonContentAfter(createdOn, numberOfPreviousAndLater).Result;

        return (previousContent, laterContent);
    }

    public static string OpenGraphImageMetaTags(PictureSiteInformation? mainImage)
    {
        if (mainImage?.Pictures?.DisplayPicture == null) return string.Empty;

        var metaString = "";
        metaString +=
            $"<meta property=\"og:image\" content=\"{mainImage.Pictures.DisplayPicture.SiteUrl}\" />";
        metaString +=
            $"<meta property=\"og:image:secure_url\" content=\"{mainImage.Pictures.DisplayPicture.SiteUrl}\" />";
        metaString += "<meta property=\"og:image:type\" content=\"image/jpeg\" />";
        metaString += $"<meta property=\"og:image:width\" content=\"{mainImage.Pictures.DisplayPicture.Width}\" />";
        metaString +=
            $"<meta property=\"og:image:height\" content=\"{mainImage.Pictures.DisplayPicture.Height}\" />";
        metaString += $"<meta property=\"og:image:alt\" content=\"{mainImage.Pictures.DisplayPicture.AltText}\" />";

        return metaString;
    }

    public static string PhotoCaptionText(PhotoContent dbEntry, bool includeTitle = true)
    {
        var summaryStringList = new List<string>();

        var titleSummaryString = string.Empty;

        var summaryHasValue = !string.IsNullOrWhiteSpace(dbEntry.Summary);

        if (includeTitle || !summaryHasValue)
        {
            if (summaryHasValue)
            {
                var summaryIsInTitle = dbEntry.Title.ContainsFuzzy(dbEntry.Summary, 0.8, SimMetricType.JaroWinkler);
                var titleIsInSummary = dbEntry.Summary.ContainsFuzzy(dbEntry.Title, 0.8, SimMetricType.JaroWinkler);

                if (titleIsInSummary) titleSummaryString = dbEntry.Summary.TrimNullToEmpty();
                else if (summaryIsInTitle) titleSummaryString = dbEntry.Title.TrimNullToEmpty();
                else titleSummaryString = $"{dbEntry.Title.TrimNullToEmpty()}: {dbEntry.Summary.TrimNullToEmpty()}";
            }
            else
            {
                titleSummaryString = dbEntry.Title.TrimNullToEmpty();
            }
        }
        else
        {
            titleSummaryString = dbEntry.Summary.TrimNullToEmpty();
        }

        if (!string.IsNullOrWhiteSpace(titleSummaryString) && !char.IsPunctuation(titleSummaryString[^1]))
            titleSummaryString += ".";

        summaryStringList.Add(titleSummaryString);

        if (!string.IsNullOrWhiteSpace(dbEntry.PhotoCreatedBy)) summaryStringList.Add($"{dbEntry.PhotoCreatedBy}.");
        summaryStringList.Add($"{dbEntry.PhotoCreatedOn:M/d/yyyy}.");

        return string.Join(" ", summaryStringList);
    }

    public static HtmlTag PhotoFigCaptionTag(PhotoContent dbEntry, bool includeTitle = true)
    {
        if (string.IsNullOrWhiteSpace(dbEntry.Summary)) return HtmlTag.Empty();

        var figCaptionTag = new HtmlTag("figcaption");
        figCaptionTag.AddClass("single-photo-caption");
        figCaptionTag.Text(string.Join(" ", PhotoCaptionText(dbEntry, includeTitle)));

        return figCaptionTag;
    }

    public static (PhotoContent? previousContent, PhotoContent? laterContent)
        PhotoPreviousAndNextContent(DateTime photoDateTime)
    {
        var previousContent = Db.PhotoCommonContentPrevious(photoDateTime).Result;
        var nextContent = Db.PhotoCommonContentNext(photoDateTime).Result;

        return (previousContent, nextContent);
    }

    public static HtmlTag PictureEmailImgTag(PictureAsset pictureDirectoryInfo, bool willHaveVisibleCaption)
    {
        var emailSize = pictureDirectoryInfo.SrcsetImages.Where(x => x.Width < 800).OrderByDescending(x => x.Width)
            .First();

        var stringWidth = "94%";
        if (emailSize.Width < emailSize.Height)
            stringWidth = emailSize.Height > 600
                ? ((int)(600M / emailSize.Height * emailSize.Width)).ToString("F0")
                : emailSize.Width.ToString("F0");

        var imageTag = new HtmlTag("img").Attr("src", $"{emailSize.SiteUrl}")
            .Attr("max-height", emailSize.Height).Attr("max-width", emailSize.Width).Attr("width", stringWidth);

        if (!string.IsNullOrWhiteSpace(emailSize.AltText))
            imageTag.Attr("alt", emailSize.AltText);

        if (!willHaveVisibleCaption && string.IsNullOrWhiteSpace(emailSize.AltText) &&
            pictureDirectoryInfo.DbEntry != null &&
            !string.IsNullOrWhiteSpace(((ITitleSummarySlugFolder)pictureDirectoryInfo.DbEntry).Summary))
            imageTag.Attr("alt", ((ITitleSummarySlugFolder)pictureDirectoryInfo.DbEntry).Summary);

        return imageTag;
    }

    public static HtmlTag PictureImgCardWithLink(PictureAsset? pictureAsset, string linkTo)
    {
        if (pictureAsset?.SmallPicture == null) return HtmlTag.Empty();

        var imgTag = PictureImgTagWithCardSizedDefaultSrc(pictureAsset);

        if (imgTag.IsEmpty()) return HtmlTag.Empty();

        imgTag.AddClass(pictureAsset.SmallPicture.Height > pictureAsset.SmallPicture.Width
            ? "thumb-vertical"
            : "thumb-horizontal");

        if (string.IsNullOrWhiteSpace(linkTo)) return imgTag;

        var outerLink = new LinkTag(string.Empty, linkTo);
        outerLink.Children.Add(imgTag);

        return outerLink;
    }

    public static HtmlTag PictureImgTag(PictureAsset pictureDirectoryInfo, string sizes,
        bool willHaveVisibleCaption)
    {
        if (pictureDirectoryInfo.DisplayPicture == null) return HtmlTag.Empty();

        var imageTag = new HtmlTag("img").AddClass("single-photo")
            .Attr("srcset", pictureDirectoryInfo.SrcSetString())
            .Attr("src", pictureDirectoryInfo.DisplayPicture.SiteUrl)
            .Attr("height", pictureDirectoryInfo.DisplayPicture.Height)
            .Attr("width", pictureDirectoryInfo.DisplayPicture.Width)
            .Attr("loading", "lazy")
            .Attr("sizes", !string.IsNullOrWhiteSpace(sizes) ? sizes : "100vw")
            .Style("max-width",
                $"{pictureDirectoryInfo.LargePicture?.Width ?? pictureDirectoryInfo.DisplayPicture.Width}px");

        if (!string.IsNullOrWhiteSpace(pictureDirectoryInfo.DisplayPicture.AltText))
            imageTag.Attr("alt", pictureDirectoryInfo.DisplayPicture.AltText);

        if (!willHaveVisibleCaption && string.IsNullOrWhiteSpace(pictureDirectoryInfo.DisplayPicture.AltText) &&
            pictureDirectoryInfo.DbEntry != null &&
            !string.IsNullOrWhiteSpace(((ITitleSummarySlugFolder)pictureDirectoryInfo.DbEntry).Summary))
            imageTag.Attr("alt", ((ITitleSummarySlugFolder)pictureDirectoryInfo.DbEntry).Summary);

        return imageTag;
    }

    public static HtmlTag PictureImgTagDisplayImageOnly(PictureAsset pictureDirectoryInfo)
    {
        if (pictureDirectoryInfo.DisplayPicture == null) return HtmlTag.Empty();

        var imageTag = new HtmlTag("img").AddClass("single-photo")
            .Attr("src", $"{pictureDirectoryInfo.DisplayPicture.SiteUrl}")
            .Attr("height", pictureDirectoryInfo.DisplayPicture.Height)
            .Attr("width", pictureDirectoryInfo.DisplayPicture.Width);

        if (!string.IsNullOrWhiteSpace(pictureDirectoryInfo.DisplayPicture.AltText))
            imageTag.Attr("alt", pictureDirectoryInfo.DisplayPicture.AltText);

        return imageTag;
    }

    public static HtmlTag PictureImgTagWithCardSizedDefaultSrc(PictureAsset? pictureAsset)
    {
        if (pictureAsset?.SmallPicture == null || pictureAsset.DisplayPicture == null) return HtmlTag.Empty();

        var imgToUse = pictureAsset.SrcsetImages.Where(x => x.Width >= 300).MinBy(x => x.Width) ??
                       pictureAsset.SrcsetImages.Where(x => x.Width <= 300).MaxBy(x => x.Width);

        if (imgToUse == null) return HtmlTag.Empty();

        var imageTag = new HtmlTag("img").AddClass("card-photo").Attr("srcset", pictureAsset.SrcSetString())
            .Attr("src", imgToUse.SiteUrl).Attr("height", imgToUse.Height)
            .Attr("width", imgToUse.Width).Attr("loading", "lazy");

        imageTag.Attr("sizes", $"{imgToUse.Width}px");

        if (!string.IsNullOrWhiteSpace(pictureAsset.DisplayPicture?.AltText))
            imageTag.Attr("alt", pictureAsset.DisplayPicture.AltText);

        return imageTag;
    }

    public static HtmlTag PictureImgTagWithSmallestDefaultSrc(PictureAsset? pictureAsset)
    {
        if (pictureAsset?.SmallPicture == null || pictureAsset.DisplayPicture == null) return HtmlTag.Empty();

        var imageTag = new HtmlTag("img").AddClass("thumb-photo").Attr("srcset", pictureAsset.SrcSetString())
            .Attr("src", pictureAsset.SmallPicture.SiteUrl).Attr("height", pictureAsset.SmallPicture.Height)
            .Attr("width", pictureAsset.SmallPicture.Width).Attr("loading", "lazy");

        var smallestGreaterThan100 = pictureAsset.SrcsetImages.Where(x => x.Width > 100).MinBy(x => x.Width);

        imageTag.Attr("sizes", smallestGreaterThan100 == null ? "100px" : $"{smallestGreaterThan100.Width}px");

        if (!string.IsNullOrWhiteSpace(pictureAsset.DisplayPicture?.AltText))
            imageTag.Attr("alt", pictureAsset.DisplayPicture.AltText);

        return imageTag;
    }

    public static HtmlTag PictureImgThumbWithLink(PictureAsset? pictureAsset, string linkTo)
    {
        if (pictureAsset?.SmallPicture == null) return HtmlTag.Empty();

        var imgTag = PictureImgTagWithSmallestDefaultSrc(pictureAsset);

        if (imgTag.IsEmpty()) return HtmlTag.Empty();

        imgTag.AddClass(pictureAsset.SmallPicture.Height > pictureAsset.SmallPicture.Width
            ? "thumb-vertical"
            : "thumb-horizontal");

        if (string.IsNullOrWhiteSpace(linkTo)) return imgTag;

        var outerLink = new LinkTag(string.Empty, linkTo);
        outerLink.Children.Add(imgTag);

        return outerLink;
    }

    public static HtmlTag PictureSizeList(bool showSizeList, PictureSiteInformation? pictureInformation)
    {
        if (!showSizeList || pictureInformation?.Pictures == null) return HtmlTag.Empty();

        var outerContainer = new DivTag().AddClasses("picture-sizes-container", "info-list-container");

        outerContainer.Children.Add(new DivTag().AddClasses("picture-sizes-label-tag", "info-list-label")
            .Text("Sizes:"));

        var sizes = pictureInformation.Pictures.SrcsetImages.OrderBy(x => x.Height * x.Width).ToList();

        foreach (var loopSizes in sizes)
        {
            //Todo: look at abstracting to InfoLinkDivTag
            var tagLinkContainer = new DivTag().AddClasses("tags-detail-link-container", "info-box");

            var tagLink =
                new LinkTag($"{loopSizes.Width}x{loopSizes.Height}",
                        loopSizes.SiteUrl)
                    .AddClasses("tag-detail-link", "info-list-link-item");
            tagLinkContainer.Children.Add(tagLink);
            outerContainer.Children.Add(tagLinkContainer);
        }

        return outerContainer;
    }

    public static async Task<HtmlTag> PostBodyDiv(IBodyContent dbEntry, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(dbEntry.BodyContent)) return HtmlTag.Empty();

        var bodyContainer = new HtmlTag("div").AddClass("post-body-container");

        var bodyText = ContentProcessing.ProcessContent(
            await BracketCodeCommon.ProcessCodesForSite(dbEntry.BodyContent, progress).ConfigureAwait(false),
            dbEntry.BodyContentFormat);

        bodyContainer.Children.Add(new HtmlTag("div").AddClass("post-body-content").Encoded(false).Text(bodyText));

        return bodyContainer;
    }

    public static async Task<HtmlTag> PostBodyDivFromMarkdown(string? bodyContent,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(bodyContent)) return HtmlTag.Empty();

        var bodyContainer = new HtmlTag("div").AddClass("post-body-container");

        var bodyText = ContentProcessing.ProcessContent(
            await BracketCodeCommon.ProcessCodesForSite(bodyContent, progress).ConfigureAwait(false),
            ContentFormatEnum.MarkdigMarkdown01);

        bodyContainer.Children.Add(new HtmlTag("div").AddClass("post-body-content").Encoded(false).Text(bodyText));

        return bodyContainer;
    }

    public static HtmlTag PostCreatedByAndUpdatedOnDiv(ICreatedAndLastUpdateOnAndBy dbEntry)
    {
        var titleContainer = new HtmlTag("div").AddClass("post-title-area-created-and-updated-container");
        titleContainer.Children.Add(new HtmlTag("h3").AddClass("post-title-area-created-and-updated-content")
            .Text(CreatedByAndUpdatedOnString(dbEntry)));
        return titleContainer;
    }

    public static HtmlTag PreviousAndNextContentDiv(List<IContentCommon> previousPosts,
        List<IContentCommon> laterPosts)
    {
        if (!UserSettingsSingleton.CurrentSettings().ShowPreviousNextContent) return HtmlTag.Empty();
        if (!laterPosts.Any() && !previousPosts.Any()) return HtmlTag.Empty();

        var hasPreviousPosts = previousPosts.Any();
        var hasLaterPosts = laterPosts.Any();
        var hasBothEarlierAndLaterPosts = hasPreviousPosts && hasLaterPosts;

        var relatedPostsContainer =
            new DivTag().AddClasses("post-related-posts-container", "compact-content-list-container");
        relatedPostsContainer.Children.Add(new DivTag()
            .Text($"Posts {(hasPreviousPosts ? "Before" : "")}" +
                  $"{(hasBothEarlierAndLaterPosts ? "/" : "")}{(hasLaterPosts ? "After" : "")}:")
            .AddClasses("post-related-posts-label-tag", "compact-content-list-label"));

        if (hasPreviousPosts)
            foreach (var loopPosts in previousPosts)
                relatedPostsContainer.Children.Add(BodyContentReferences.CompactContentDiv(loopPosts));

        if (hasLaterPosts)
            foreach (var loopPosts in laterPosts)
                relatedPostsContainer.Children.Add(BodyContentReferences.CompactContentDiv(loopPosts));

        return relatedPostsContainer;
    }

    public static HtmlTag PreviousAndNextPhotoDiv(PhotoContent? previousPhoto,
        PhotoContent? nextPhoto)
    {
        if (!UserSettingsSingleton.CurrentSettings().ShowPreviousNextContent) return HtmlTag.Empty();
        if (previousPhoto is null && nextPhoto is null) return HtmlTag.Empty();

        var hasPreviousPosts = previousPhoto != null;
        var hasLaterPosts = nextPhoto != null;
        var hasBothEarlierAndLaterPosts = hasPreviousPosts && hasLaterPosts;

        var relatedPostsContainer =
            new DivTag().AddClasses("photo-previous-next-container", "compact-content-list-container");
        relatedPostsContainer.Children.Add(new DivTag()
            .Text($"{(hasPreviousPosts ? "Previous" : "")}" +
                  $"{(hasBothEarlierAndLaterPosts ? "/" : "")}{(hasLaterPosts ? "Next" : "")}:")
            .AddClasses("photo-previous-next-label-tag", "compact-content-list-label"));

        if (hasPreviousPosts)
            relatedPostsContainer.Children.Add(BodyContentReferences.CompactContentDiv(previousPhoto));

        if (hasLaterPosts)
            relatedPostsContainer.Children.Add(BodyContentReferences.CompactContentDiv(nextPhoto));

        return relatedPostsContainer;
    }

    public static HtmlTag SiteMainRss()
    {
        return new HtmlTag("Link").Attr("rel", "alternate").Attr("type", "application/rss+xml")
            .Attr("title", $"Main RSS Feed for {UserSettingsSingleton.CurrentSettings().SiteName}").Attr("href",
                $"{UserSettingsSingleton.CurrentSettings().RssIndexFeedUrl()}").Encoded(false);
    }

    public static async Task<HtmlTag> StandardHeader()
    {
        var titleContainer = new DivTag().AddClass("site-header-container");

        var titleHeader = new HtmlTag("H1").AddClass("site-header-title");
        titleHeader.Children.Add(new LinkTag(UserSettingsSingleton.CurrentSettings().SiteName,
            $"{UserSettingsSingleton.CurrentSettings().SiteUrl()}", "site-header-title-link"));

        titleContainer.Children.Add(titleHeader);

        var secondaryDiv = new DivTag().AddClass("site-header-subtitle-menu-container");
        titleContainer.Children.Add(secondaryDiv);

        var siteSummary = UserSettingsSingleton.CurrentSettings().SiteSummary;

        if (!string.IsNullOrWhiteSpace(siteSummary))
        {
            var titleSiteSummary = new HtmlTag("H5").AddClass("site-header-subtitle").Text(siteSummary);
            secondaryDiv.Children.Add(titleSiteSummary);
        }

        secondaryDiv.Children.Add(await CoreLinksDiv().ConfigureAwait(false));

        return titleContainer;
    }

    public static HtmlTag TagList(ITag dbEntry)
    {
        if (string.IsNullOrWhiteSpace(dbEntry.Tags)) return HtmlTag.Empty();

        if (dbEntry is IShowInSearch { ShowInSearch: false }) return HtmlTag.Empty();

        var tags = Db.TagListParseToSlugsAndIsExcluded(dbEntry);

        return TagList(tags);
    }

    public static HtmlTag TagList(List<Db.TagSlugAndIsExcluded> tags)
    {
        if (!tags.Any()) return HtmlTag.Empty();

        var tagsContainer = new DivTag().AddClasses("tags-container", "info-list-container");

        tagsContainer.Children.Add(new DivTag().Text("Tags:").AddClasses("tag-detail-label-tag", "info-list-label"));

        foreach (var loopTag in tags)
        {
            var tagLinkContainer = new DivTag().AddClasses("tags-detail-link-container", "info-box");
            if (loopTag.IsExcluded)
            {
                //Todo: look at abstracting to InfoTextDivTag
                var tagP = new HtmlTag("p").AddClasses("tag-detail-text", "info-list-text-item");
                tagP.Text(loopTag.TagSlug.Replace("-", " "));
                tagLinkContainer.Children.Add(tagP);
                tagsContainer.Children.Add(tagLinkContainer);
            }
            else
            {
                //Todo: look at abstracting to InfoLinkDivTag
                var tagLink =
                    new LinkTag(loopTag.TagSlug.Replace("-", " "),
                            UserSettingsSingleton.CurrentSettings().TagPageUrl(loopTag.TagSlug))
                        .AddClasses("tag-detail-link", "info-list-link-item");
                tagLinkContainer.Children.Add(tagLink);
                tagsContainer.Children.Add(tagLinkContainer);
            }
        }

        return tagsContainer;
    }

    public static HtmlTag TagListTextLinkList(ITag dbEntry)
    {
        if (string.IsNullOrWhiteSpace(dbEntry.Tags)) return HtmlTag.Empty();

        var tags = Db.TagListParseToSlugs(dbEntry, true);

        return TagListTextLinkList(tags);
    }

    public static HtmlTag TagListTextLinkList(List<string> tags)
    {
        tags = tags.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();

        if (!tags.Any()) return HtmlTag.Empty();

        var tagsContainer = new HtmlTag("p");

        var innerContent = new List<string>();

        foreach (var loopTag in tags)
        {
            var tagLink =
                new LinkTag(loopTag.Replace("-", " "), UserSettingsSingleton.CurrentSettings().TagPageUrl(loopTag))
                    .AddClass("tag-detail-link");
            var tagLinkString = tagLink.ToString();
            if (string.IsNullOrWhiteSpace(tagLinkString)) continue;
            innerContent.Add(tagLinkString);
        }

        tagsContainer.Text($"Tags: {string.Join(", ", innerContent)}").Encoded(false);

        return tagsContainer;
    }

    public static HtmlTag TitleDiv(ITitleSummarySlugFolder content)
    {
        var titleContainer = new HtmlTag("div").AddClass("title-container");
        titleContainer.Children.Add(new HtmlTag("h1").AddClass("title-content").Text(content.Title));
        return titleContainer;
    }

    public static HtmlTag TitleDiv(string stringTitle)
    {
        var titleContainer = new HtmlTag("div").AddClass("title-container");
        titleContainer.Children.Add(new HtmlTag("h1").AddClass("title-content").Text(stringTitle));
        return titleContainer;
    }

    public static HtmlTag TitleLinkDiv(ITitleSummarySlugFolder content, IContentId id)
    {
        var titleContainer = new HtmlTag("div").AddClass("title-link-container");

        var header = new HtmlTag("h1").AddClass("title-link-content");
        var linkToFullPost = new LinkTag(content.Title,
            UserSettingsSingleton.CurrentSettings().ContentUrl(id.ContentId).Result);
        header.Children.Add(linkToFullPost);

        titleContainer.Children.Add(header);

        return titleContainer;
    }

    public static async Task<HtmlTag> UpdateNotesDiv(IUpdateNotes dbEntry)
    {
        if (string.IsNullOrWhiteSpace(dbEntry.UpdateNotes)) return HtmlTag.Empty();

        var updateNotesDiv = new DivTag().AddClass("update-notes-container");

        updateNotesDiv.Children.Add(new DivTag().AddClass("update-notes-title").Text("Updates:"));

        var updateNotesContentContainer = new DivTag().AddClass("update-notes-content");

        var updateNotesHtml = ContentProcessing.ProcessContent(
            await BracketCodeCommon.ProcessCodesForSite(dbEntry.UpdateNotes).ConfigureAwait(false),
            dbEntry.UpdateNotesFormat);

        updateNotesContentContainer.Encoded(false).Text(updateNotesHtml);

        updateNotesDiv.Children.Add(updateNotesContentContainer);

        return updateNotesDiv;
    }
}