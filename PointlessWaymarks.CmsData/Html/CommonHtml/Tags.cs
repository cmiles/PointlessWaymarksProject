using System;
using System.Collections.Generic;
using System.Linq;
using HtmlTags;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Html.CommonHtml
{
    public static class Tags
    {
        public static HtmlTag CoreLinksDiv(IProgress<string>? progress = null)
        {
            var db = Db.Context().Result;

            var items = db.MenuLinks.OrderBy(x => x.MenuOrder).ToList();

            if (!items.Any()) return HtmlTag.Empty();

            var coreLinksDiv = new HtmlTag("nav").AddClass("core-links-container");

            foreach (var loopItems in items)
            {
                var html = ContentProcessing.ProcessContent(
                    BracketCodeCommon.ProcessCodesForSite(loopItems.LinkTag, progress),
                    ContentFormatEnum.MarkdigMarkdown01);

                var coreLinkContainer = new DivTag().AddClass("core-links-item").Text(html).Encoded(false);
                coreLinksDiv.Children.Add(coreLinkContainer);
            }

            return coreLinksDiv;
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
                if (string.Compare(dbEntry.CreatedBy, dbEntry.LastUpdatedBy, StringComparison.OrdinalIgnoreCase) == 0)
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
            return $"<link rel=\"stylesheet\" href=\"https:{settings.CssMainStyleFileUrl()}?v=1.0\">";
        }

        public static string FavIconFileString()
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            return $"<link rel=\"shortcut icon\" href=\"https:{settings.FaviconUrl()}\"/>";
        }

        public static string ImageCaptionText(ImageContent dbEntry, bool includeTitle = false)
        {
            var summaryString = includeTitle ? dbEntry.Title : string.Empty;

            if (!string.IsNullOrWhiteSpace(dbEntry.Summary))
            {
                if (includeTitle) summaryString += ": ";
                if (!dbEntry.Summary.Trim().EndsWith(".")) summaryString += $"{dbEntry.Summary.Trim()}.";
                else summaryString += $"{dbEntry.Summary.Trim()}";
            }

            return string.Join(" ", summaryString);
        }

        public static HtmlTag ImageFigCaptionTag(ImageContent dbEntry, bool includeTitle = false)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.Summary)) return HtmlTag.Empty();

            var figCaptionTag = new HtmlTag("figcaption");
            figCaptionTag.AddClass("single-image-caption");

            figCaptionTag.Text(ImageCaptionText(dbEntry, includeTitle));

            return figCaptionTag;
        }

        public static HtmlTag InfoDivTag(string? contents, string className, string dataType, string? dataValue)
        {
            if (string.IsNullOrWhiteSpace(contents)) return HtmlTag.Empty();
            var divTag = new HtmlTag("div");
            divTag.AddClass(className);

            var spanTag = new HtmlTag("div");
            spanTag.Text(contents.Trim());
            spanTag.AddClass($"{className}-content");
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
                $"<meta property=\"og:image\" content=\"https:{mainImage.Pictures.DisplayPicture.SiteUrl}\" />";
            metaString +=
                $"<meta property=\"og:image:secure_url\" content=\"https:{mainImage.Pictures.DisplayPicture.SiteUrl}\" />";
            metaString += "<meta property=\"og:image:type\" content=\"image/jpeg\" />";
            metaString += $"<meta property=\"og:image:width\" content=\"{mainImage.Pictures.DisplayPicture.Width}\" />";
            metaString +=
                $"<meta property=\"og:image:height\" content=\"{mainImage.Pictures.DisplayPicture.Height}\" />";
            metaString += $"<meta property=\"og:image:alt\" content=\"{mainImage.Pictures.DisplayPicture.AltText}\" />";

            return metaString;
        }

        public static HtmlTag PageCreatedDiv(ICreatedAndLastUpdateOnAndBy createdBy)
        {
            var createdByDiv = new DivTag().AddClass("created-by-container");

            createdByDiv.Children.Add(new HtmlTag("div").AddClass("created-title"));

            createdByDiv.Children.Add(new HtmlTag("p").AddClass("created-by-content").Text(
                $"Page Created by {createdBy.CreatedBy}, {createdBy.CreatedOn:M/d/yyyy}"));

            return createdByDiv;
        }

        public static string PhotoCaptionText(PhotoContent dbEntry, bool includeTitle = false)
        {
            var summaryStringList = new List<string>();

            string titleSummaryString;

            var summaryHasValue = !string.IsNullOrWhiteSpace(dbEntry.Summary);

            if (includeTitle || !summaryHasValue)
            {
                titleSummaryString = dbEntry.Title.TrimNullToEmpty();

                if (summaryHasValue)
                {
                    var summaryIsInTitle = titleSummaryString.Replace(".", string.Empty).ToLower()
                        .Contains(dbEntry.Summary.TrimNullToEmpty().Replace(".", string.Empty).ToLower());

                    if (!summaryIsInTitle) titleSummaryString += $": {dbEntry.Summary.TrimNullToEmpty()}";
                }

                if (!titleSummaryString.EndsWith(".")) titleSummaryString += ".";
            }
            else
            {
                titleSummaryString = dbEntry.Summary.TrimNullToEmpty();
                if (!titleSummaryString.EndsWith(".")) titleSummaryString += ".";
            }

            summaryStringList.Add(titleSummaryString);

            summaryStringList.Add($"{dbEntry.PhotoCreatedBy}.");
            summaryStringList.Add($"{dbEntry.PhotoCreatedOn:M/d/yyyy}.");

            return string.Join(" ", summaryStringList);
        }

        public static HtmlTag PhotoFigCaptionTag(PhotoContent dbEntry, bool includeTitle = false)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.Summary)) return HtmlTag.Empty();

            var figCaptionTag = new HtmlTag("figcaption");
            figCaptionTag.AddClass("single-photo-caption");
            figCaptionTag.Text(string.Join(" ", PhotoCaptionText(dbEntry, includeTitle)));

            return figCaptionTag;
        }

        public static HtmlTag PictureEmailImgTag(PictureAsset pictureDirectoryInfo, bool willHaveVisibleCaption)
        {
            var emailSize = pictureDirectoryInfo.SrcsetImages.Where(x => x.Width < 800).OrderByDescending(x => x.Width)
                .First();

            var stringWidth = "94%";
            if (emailSize.Width < emailSize.Height)
                stringWidth = emailSize.Height > 600
                    ? ((int) (600M / emailSize.Height * emailSize.Width)).ToString("F0")
                    : emailSize.Width.ToString("F0");

            var imageTag = new HtmlTag("img").Attr("src", $"https:{emailSize.SiteUrl}")
                .Attr("max-height", emailSize.Height).Attr("max-width", emailSize.Width).Attr("width", stringWidth);

            if (!string.IsNullOrWhiteSpace(emailSize.AltText))
                imageTag.Attr("alt", emailSize.AltText);

            if (!willHaveVisibleCaption && string.IsNullOrWhiteSpace(emailSize.AltText)
                                        && pictureDirectoryInfo.DbEntry != null
                                        && !string.IsNullOrWhiteSpace(
                                            ((ITitleSummarySlugFolder) pictureDirectoryInfo.DbEntry).Summary))
                imageTag.Attr("alt", ((ITitleSummarySlugFolder) pictureDirectoryInfo.DbEntry).Summary);

            return imageTag;
        }

        public static HtmlTag PictureImgTag(PictureAsset pictureDirectoryInfo, string sizes,
            bool willHaveVisibleCaption)
        {
            if (pictureDirectoryInfo.DisplayPicture == null) return HtmlTag.Empty();

            var imageTag = new HtmlTag("img").AddClass("single-photo")
                .Attr("srcset", pictureDirectoryInfo.SrcSetString())
                .Attr("src", pictureDirectoryInfo.DisplayPicture.SiteUrl)
                .Attr("height", pictureDirectoryInfo.DisplayPicture.Height)
                .Attr("width", pictureDirectoryInfo.DisplayPicture.Width).Attr("loading", "lazy");

            imageTag.Attr("sizes", !string.IsNullOrWhiteSpace(sizes) ? sizes : "100vw");

            if (!string.IsNullOrWhiteSpace(pictureDirectoryInfo.DisplayPicture.AltText))
                imageTag.Attr("alt", pictureDirectoryInfo.DisplayPicture.AltText);

            if (!willHaveVisibleCaption && string.IsNullOrWhiteSpace(pictureDirectoryInfo.DisplayPicture.AltText)
                                        && pictureDirectoryInfo.DbEntry != null
                                        && !string.IsNullOrWhiteSpace(
                                            ((ITitleSummarySlugFolder) pictureDirectoryInfo.DbEntry).Summary))
                imageTag.Attr("alt", ((ITitleSummarySlugFolder) pictureDirectoryInfo.DbEntry).Summary);

            return imageTag;
        }

        public static HtmlTag PictureImgTagDisplayImageOnly(PictureAsset pictureDirectoryInfo)
        {
            if (pictureDirectoryInfo.DisplayPicture == null) return HtmlTag.Empty();

            var imageTag = new HtmlTag("img").AddClass("single-photo")
                .Attr("src", $"https:{pictureDirectoryInfo.DisplayPicture.SiteUrl}")
                .Attr("height", pictureDirectoryInfo.DisplayPicture.Height)
                .Attr("width", pictureDirectoryInfo.DisplayPicture.Width);

            if (!string.IsNullOrWhiteSpace(pictureDirectoryInfo.DisplayPicture.AltText))
                imageTag.Attr("alt", pictureDirectoryInfo.DisplayPicture.AltText);

            return imageTag;
        }

        public static HtmlTag PictureImgTagWithSmallestDefaultSrc(PictureAsset? pictureAsset)
        {
            if (pictureAsset?.SmallPicture == null || pictureAsset.DisplayPicture == null) return HtmlTag.Empty();

            var imageTag = new HtmlTag("img").AddClass("thumb-photo").Attr("srcset", pictureAsset.SrcSetString())
                .Attr("src", pictureAsset.SmallPicture.SiteUrl).Attr("height", pictureAsset.SmallPicture.Height)
                .Attr("width", pictureAsset.SmallPicture.Width).Attr("loading", "lazy");

            var smallestGreaterThan100 = pictureAsset.SrcsetImages.Where(x => x.Width > 100).OrderBy(x => x.Width)
                .FirstOrDefault();

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

        public static HtmlTag PostBodyDiv(IBodyContent dbEntry, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.BodyContent)) return HtmlTag.Empty();

            var bodyContainer = new HtmlTag("div").AddClass("post-body-container");

            var bodyText = ContentProcessing.ProcessContent(
                BracketCodeCommon.ProcessCodesForSite(dbEntry.BodyContent, progress), dbEntry.BodyContentFormat);

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

        public static HtmlTag PreviousAndNextPostsDiv(List<IContentCommon> previousPosts,
            List<IContentCommon> laterPosts)
        {
            if (!laterPosts.Any() && !previousPosts.Any()) return HtmlTag.Empty();

            var hasPreviousPosts = previousPosts.Any();
            var hasLaterPosts = laterPosts.Any();
            var hasBothEarlierAndLaterPosts = hasPreviousPosts && hasLaterPosts;

            var relatedPostsContainer = new DivTag().AddClass("post-related-posts-container");
            relatedPostsContainer.Children.Add(new DivTag()
                .Text($"Posts {(hasPreviousPosts ? "Before" : "")}" +
                      $"{(hasBothEarlierAndLaterPosts ? "/" : "")}{(hasLaterPosts ? "After" : "")}:")
                .AddClass("post-related-posts-label-tag"));

            if (hasPreviousPosts)
                foreach (var loopPosts in previousPosts)
                    relatedPostsContainer.Children.Add(BodyContentReferences.RelatedContentDiv(loopPosts));

            if (hasLaterPosts)
                foreach (var loopPosts in laterPosts)
                    relatedPostsContainer.Children.Add(BodyContentReferences.RelatedContentDiv(loopPosts));

            return relatedPostsContainer;
        }

        public static HtmlTag SearchTypeLinksDiv()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var coreLinksDiv = new DivTag().AddClass("search-types-container");

            coreLinksDiv.Children.Add(
                new LinkTag("Search Posts", @$"{settings.PostsListUrl()}").AddClass("search-types-item"));
            coreLinksDiv.Children.Add(
                new LinkTag("Photos", @$"{settings.PhotoListUrl()}").AddClass("search-types-item"));
            coreLinksDiv.Children.Add(
                new LinkTag("Images", @$"{settings.ImageListUrl()}").AddClass("search-types-item"));
            coreLinksDiv.Children.Add(new LinkTag("Files", @$"{settings.FileListUrl()}").AddClass("search-types-item"));
            coreLinksDiv.Children.Add(new LinkTag("Notes", @$"{settings.NoteListUrl()}").AddClass("search-types-item"));
            coreLinksDiv.Children.Add(new LinkTag("Links", @$"{settings.LinkListUrl()}").AddClass("search-types-item"));

            return coreLinksDiv;
        }

        public static HtmlTag SiteMainRss()
        {
            return new HtmlTag("Link").Attr("rel", "alternate").Attr("type", "application/rss+xml")
                .Attr("title", $"Main RSS Feed for {UserSettingsSingleton.CurrentSettings().SiteName}").Attr("href",
                    $"https:{UserSettingsSingleton.CurrentSettings().RssIndexFeedUrl()}");
        }

        public static HtmlTag StandardHeader()
        {
            var titleContainer = new DivTag().AddClass("index-title-container");

            var titleHeader = new HtmlTag("H1").AddClass("index-title-content");
            titleHeader.Children.Add(new LinkTag(UserSettingsSingleton.CurrentSettings().SiteName,
                $"https://{UserSettingsSingleton.CurrentSettings().SiteUrl}", "index-title-content-link"));

            titleContainer.Children.Add(titleHeader);

            var siteSummary = UserSettingsSingleton.CurrentSettings().SiteSummary;

            if (!string.IsNullOrWhiteSpace(siteSummary))
            {
                var titleSiteSummary = new HtmlTag("H5").AddClass("index-title-summary-content").Text(siteSummary);
                titleContainer.Children.Add(titleSiteSummary);
            }

            titleContainer.Children.Add(CoreLinksDiv());

            return titleContainer;
        }

        public static HtmlTag TagList(ITag dbEntry)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.Tags)) return HtmlTag.Empty();

            var tags = Db.TagListParseToSlugsAndIsExcluded(dbEntry);

            return TagList(tags);
        }

        public static HtmlTag TagList(List<Db.TagSlugAndIsExcluded> tags)
        {
            if (!tags.Any()) return HtmlTag.Empty();

            var tagsContainer = new DivTag().AddClass("tags-container");

            tagsContainer.Children.Add(new DivTag().Text("Tags:").AddClass("tag-detail-label-tag"));

            foreach (var loopTag in tags)
            {
                var tagLinkContainer = new DivTag().AddClass("tags-detail-link-container");
                if (loopTag.IsExcluded)
                {
                    var tagP = new HtmlTag("p").AddClass("tag-detail-text");
                    tagP.Text(loopTag.TagSlug.Replace("-", " "));
                    tagLinkContainer.Children.Add(tagP);
                    tagsContainer.Children.Add(tagLinkContainer);
                }
                else
                {
                    var tagLink =
                        new LinkTag(loopTag.TagSlug.Replace("-", " "),
                                UserSettingsSingleton.CurrentSettings().TagPageUrl(loopTag.TagSlug))
                            .AddClass("tag-detail-link");
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
                innerContent.Add(tagLink.ToString());
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

        public static HtmlTag UpdateNotesDiv(IUpdateNotes dbEntry)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.UpdateNotes)) return HtmlTag.Empty();

            var updateNotesDiv = new DivTag().AddClass("update-notes-container");

            updateNotesDiv.Children.Add(new DivTag().AddClass("update-notes-title").Text("Updates:"));

            var updateNotesContentContainer = new DivTag().AddClass("update-notes-content");

            var updateNotesHtml = ContentProcessing.ProcessContent(
                BracketCodeCommon.ProcessCodesForSite(dbEntry.UpdateNotes), dbEntry.UpdateNotesFormat);

            updateNotesContentContainer.Encoded(false).Text(updateNotesHtml);

            updateNotesDiv.Children.Add(updateNotesContentContainer);

            return updateNotesDiv;
        }
    }
}