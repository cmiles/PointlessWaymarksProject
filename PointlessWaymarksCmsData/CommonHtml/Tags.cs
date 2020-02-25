using System;
using System.Collections.Generic;
using System.Linq;
using HtmlTags;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class Tags
    {
        public static HtmlTag CreatedByAndUpdatedOnDiv(ICreatedAndLastUpdateOnAndBy dbEntry)
        {
            var titleContainer = new DivTag().AddClass("created-and-updated-container");
            titleContainer.Children.Add(new DivTag().AddClass("created-and-updated-content")
                .Text(CreatedByAndUpdatedOnString(dbEntry)));
            return titleContainer;
        }

        public static string CreatedByAndUpdatedOnString(ICreatedAndLastUpdateOnAndBy dbEntry)
        {
            var createdUpdatedString = $"Created by {dbEntry.CreatedBy}";

            var onlyCreated = false;

            if (dbEntry.LastUpdatedOn != null && dbEntry.CreatedOn.Date == dbEntry.LastUpdatedOn.Value.Date)
                if (string.Compare(dbEntry.CreatedBy, dbEntry.LastUpdatedBy, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    createdUpdatedString = $"Created and Updated by {dbEntry.LastUpdatedBy} ";
                    onlyCreated = true;
                }

            createdUpdatedString += $" on {dbEntry.CreatedOn:M/d/yyyy}.";

            if (onlyCreated) return createdUpdatedString.Trim();

            if (string.IsNullOrWhiteSpace(dbEntry.LastUpdatedBy) && dbEntry.LastUpdatedOn == null)
                return createdUpdatedString;

            if (dbEntry.LastUpdatedOn != null && dbEntry.CreatedOn.Date == dbEntry.LastUpdatedOn.Value.Date)
                return createdUpdatedString;

            var updatedString = " Updated";

            if (!string.IsNullOrWhiteSpace(dbEntry.LastUpdatedBy)) updatedString += $" by {dbEntry.LastUpdatedBy}";

            if (dbEntry.LastUpdatedOn != null) updatedString += $" on {dbEntry.LastUpdatedOn.Value:M/d/yyyy}";

            updatedString += ".";

            return (createdUpdatedString + updatedString).Trim();
        }

        public static string CssStyleFileString()
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            return $"<link rel=\"stylesheet\" href=\"{settings.CssMainStyleFileUrl()}?v=1.0\">";
        }

        public static string FavIconFileString()
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            return $"<link rel=\"shortcut icon\" href=\"{settings.FaviconUrl()}\">";
        }

        public static HtmlTag ImageFigCaptionTag(ImageContent dbEntry)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.Summary)) return HtmlTag.Empty();

            var figCaptionTag = new HtmlTag("figcaption");
            figCaptionTag.AddClass("single-image-caption");

            var summaryString = string.Empty;

            if (!string.IsNullOrWhiteSpace(dbEntry.Summary))
            {
                //titleSummaryString += ": ";
                if (!dbEntry.Summary.Trim().EndsWith(".")) summaryString += $"{dbEntry.Summary.Trim()}.";
                else summaryString += $"{dbEntry.Summary.Trim()}";
            }

            figCaptionTag.Text(string.Join(" ", summaryString));

            return figCaptionTag;
        }

        public static HtmlTag InfoDivTag(string contents, string className, string dataType, string dataValue)
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

        public static DateTime? LatestCreatedOnOrUpdatedOn(ICreatedAndLastUpdateOnAndBy dbEntry)
        {
            if (dbEntry == null) return null;

            return dbEntry.LastUpdatedOn ?? dbEntry.CreatedOn;
        }

        public static string OpenGraphImageMetaTags(PictureSiteInformation mainImage)
        {
            if (mainImage?.Pictures == null) return string.Empty;

            var metaString = "";
            metaString +=
                $"<meta property=\"og:image\" content=\"http:{mainImage.Pictures.DisplayPicture.SiteUrl}\" />";
            metaString +=
                $"<meta property=\"og:image:secure_url\" content=\"https:{mainImage.Pictures.DisplayPicture.SiteUrl}\" />";
            metaString += "<meta property=\"og:image:type\" content=\"image/jpeg\" />";
            metaString += $"<meta property=\"og:image:width\" content=\"{mainImage.Pictures.DisplayPicture.Width}\" />";
            metaString +=
                $"<meta property=\"og:image:height\" content=\"{mainImage.Pictures.DisplayPicture.Height}\" />";
            metaString +=
                $"<meta property=\"og:image:alt]\" content=\"{mainImage.Pictures.DisplayPicture.AltText}\" />";

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

        public static HtmlTag PhotoFigCaptionTag(PhotoContent dbEntry)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.Summary)) return HtmlTag.Empty();

            var figCaptionTag = new HtmlTag("figcaption");
            figCaptionTag.AddClass("single-photo-caption");

            var summaryStringList = new List<string>();

            //var titleSummaryString = DbEntry.Title;
            var titleSummaryString = string.Empty;

            if (!string.IsNullOrWhiteSpace(dbEntry.Summary))
            {
                //titleSummaryString += ": ";
                if (!dbEntry.Summary.Trim().EndsWith(".")) titleSummaryString += $"{dbEntry.Summary.Trim()}.";
                else titleSummaryString += $"{dbEntry.Summary.Trim()}";
            }

            summaryStringList.Add(titleSummaryString);

            summaryStringList.Add($"{dbEntry.PhotoCreatedBy}.");
            summaryStringList.Add($"{dbEntry.PhotoCreatedOn:M/d/yyyy}.");

            figCaptionTag.Text(string.Join(" ", summaryStringList));

            return figCaptionTag;
        }

        public static HtmlTag PictureImgTag(PictureAsset pictureDirectoryInfo)
        {
            var imageTag = new HtmlTag("img").AddClass("single-photo")
                .Attr("srcset", pictureDirectoryInfo.SrcSetString())
                .Attr("src", pictureDirectoryInfo.DisplayPicture.SiteUrl)
                .Attr("height", pictureDirectoryInfo.DisplayPicture.Height)
                .Attr("width", pictureDirectoryInfo.DisplayPicture.Width).Attr("loading", "lazy");

            if (!string.IsNullOrWhiteSpace(pictureDirectoryInfo.DisplayPicture.AltText))
                imageTag.Attr("alt", pictureDirectoryInfo.DisplayPicture.AltText);

            return imageTag;
        }

        public static HtmlTag PictureImgTagDisplayImageOnly(PictureAsset pictureDirectoryInfo)
        {
            var imageTag = new HtmlTag("img").AddClass("single-photo")
                .Attr("src", $"https:{pictureDirectoryInfo.DisplayPicture.SiteUrl}")
                .Attr("height", pictureDirectoryInfo.DisplayPicture.Height)
                .Attr("width", pictureDirectoryInfo.DisplayPicture.Width);

            if (!string.IsNullOrWhiteSpace(pictureDirectoryInfo.DisplayPicture.AltText))
                imageTag.Attr("alt", pictureDirectoryInfo.DisplayPicture.AltText);

            return imageTag;
        }

        public static HtmlTag PictureImgTagWithSmallestDefaultSrc(PictureAsset pictureAsset)
        {
            if (pictureAsset == null) return HtmlTag.Empty();

            var imageTag = new HtmlTag("img").AddClass("thumb-photo").Attr("srcset", pictureAsset.SrcSetString())
                .Attr("src", pictureAsset.SmallPicture.SiteUrl).Attr("height", pictureAsset.SmallPicture.Height)
                .Attr("width", pictureAsset.SmallPicture.Width).Attr("loading", "lazy");

            if (!string.IsNullOrWhiteSpace(pictureAsset.DisplayPicture.AltText))
                imageTag.Attr("alt", pictureAsset.DisplayPicture.AltText);

            return imageTag;
        }

        public static HtmlTag PictureImgThumbWithLink(PictureAsset pictureAsset, string linkTo)
        {
            if (pictureAsset == null) return HtmlTag.Empty();

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

        public static HtmlTag PostBodyDiv(IBodyContent dbEntry)
        {
            var bodyContainer = new HtmlTag("div").AddClass("post-body-container");

            var bodyText = BracketCodeCommon.ProcessCodesAndMarkdownForSite(dbEntry.BodyContent);

            var bodyHtmlProcessing = ContentProcessor.ContentHtml(dbEntry.BodyContentFormat, bodyText);

            if (bodyHtmlProcessing.success)
                bodyContainer.Children.Add(new HtmlTag("div").AddClass("post-body-content").Encoded(false)
                    .Text(bodyHtmlProcessing.output));

            return bodyContainer;
        }

        public static HtmlTag PostCreatedByAndUpdatedOnDiv(ICreatedAndLastUpdateOnAndBy dbEntry)
        {
            var titleContainer = new HtmlTag("div").AddClass("post-title-area-created-and-updated-container");
            titleContainer.Children.Add(new HtmlTag("h3").AddClass("post-title-area-created-and-updated-content")
                .Text(CreatedByAndUpdatedOnString(dbEntry)));
            return titleContainer;
        }

        public static HtmlTag SiteMainRss()
        {
            return new HtmlTag("Link").Attr("rel", "alternate").Attr("type", "application/rss+xml")
                .Attr("title", $"Main RSS Feed for {UserSettingsSingleton.CurrentSettings().SiteName}").Attr("href",
                    $"https:{UserSettingsSingleton.CurrentSettings().RssIndexFeedUrl()}");
        }

        public static HtmlTag TagList(ITag dbEntry)
        {
            var tagsContainer = new DivTag().AddClass("tags-container");

            if (string.IsNullOrWhiteSpace(dbEntry.Tags)) return HtmlTag.Empty();

            tagsContainer.Children.Add(new DivTag().Text("Tags:").AddClass("tag-detail-label-tag"));

            var tags = dbEntry.Tags.Split(",").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();

            if (!tags.Any()) return HtmlTag.Empty();

            foreach (var loopTag in tags) tagsContainer.Children.Add(InfoDivTag(loopTag, "tag-detail", "tag", loopTag));

            return tagsContainer;
        }

        public static HtmlTag TitleDiv(ITitleSummarySlugFolder content)
        {
            var titleContainer = new HtmlTag("div").AddClass("title-container");
            titleContainer.Children.Add(new HtmlTag("h1").AddClass("title-content").Text(content.Title));
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

        public static HtmlTag UpdateByAndOnAndNotesDiv(ICreatedAndLastUpdateOnAndBy createdEntry,
            IUpdateNotes updateEntry)
        {
            if (createdEntry.LastUpdatedOn == null) return HtmlTag.Empty();

            if (createdEntry.CreatedOn.Date == createdEntry.LastUpdatedOn.Value.Date &&
                string.IsNullOrWhiteSpace(updateEntry.UpdateNotes) &&
                (createdEntry.CreatedBy == createdEntry.LastUpdatedBy ||
                 string.IsNullOrWhiteSpace(createdEntry.LastUpdatedBy))) return HtmlTag.Empty();

            var updateNotesDiv = new DivTag().AddClass("update-notes-container");

            updateNotesDiv.Children.Add(HorizontalRule.StandardRule());

            var headingTag = new HtmlTag("div").AddClass("update-notes-title").Text("Updates:");

            var updateNotesContentContainer = new DivTag().AddClass("update-notes-content");

            updateNotesContentContainer.Children.Add(new HtmlTag("p").AddClass("update-by-and-on-content")
                .Text($"{createdEntry.LastUpdatedBy}, {createdEntry.LastUpdatedOn.Value:M/d/yyyy}"));

            if (!string.IsNullOrWhiteSpace(updateEntry.UpdateNotes))
            {
                var updateNotesHtml =
                    ContentProcessor.ContentHtml(updateEntry.UpdateNotesFormat, updateEntry.UpdateNotes);
                if (updateNotesHtml.success) updateNotesContentContainer.Encoded(false).Text(updateNotesHtml.output);
            }

            updateNotesDiv.Children.Add(headingTag);
            updateNotesDiv.Children.Add(updateNotesContentContainer);

            return updateNotesDiv;
        }

        public static HtmlTag UpdateNotesDiv(IUpdateNotes dbEntry)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.UpdateNotes)) return HtmlTag.Empty();

            var updateNotesDiv = new DivTag().AddClass("update-notes-container");

            updateNotesDiv.Children.Add(new DivTag().AddClass("update-notes-title").Text("Updates:"));

            var updateNotesContentContainer = new DivTag().AddClass("update-notes-content");

            var updateNotesHtml = ContentProcessor.ContentHtml(dbEntry.UpdateNotesFormat, dbEntry.UpdateNotes);

            if (updateNotesHtml.success) updateNotesContentContainer.Encoded(false).Text(updateNotesHtml.output);

            updateNotesDiv.Children.Add(updateNotesContentContainer);

            return updateNotesDiv;
        }
    }
}