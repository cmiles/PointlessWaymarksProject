using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlTags;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.PhotoHtml
{
    public partial class SinglePhotoPage
    {
        public SinglePhotoPage(PhotoContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsUtilities.ReadSettings().Result;
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PhotoPageUrl(DbEntry);

            Photos = PhotoFiles.ProcessPhotosInDirectory(DbEntry);
        }

        public PhotoContent DbEntry { get; set; }
        public string SiteUrl { get; set; }
        public string SiteName { get; set; }
        public string PageUrl { get; set; }

        public PhotoDirectoryContentsInformation Photos { get; set; }

        public HtmlTag PhotoDetailsDiv()
        {
            var outerContainer = new DivTag();
            outerContainer.AddClass("photo-details-container");

            outerContainer.Children.Add(new DivTag().AddClass("photo-detail-label-tag").Text("Details:"));

            outerContainer.Children.Add(CommonHtml.Tags.InfoDivTag(DbEntry.Aperture, "photo-detail", "aperture",
                DbEntry.Aperture));
            outerContainer.Children.Add(CommonHtml.Tags.InfoDivTag(DbEntry.ShutterSpeed, "photo-detail",
                "shutter-speed", DbEntry.ShutterSpeed));
            outerContainer.Children.Add(CommonHtml.Tags.InfoDivTag($"ISO {DbEntry.Iso?.ToString("F0")}", "photo-detail",
                "iso", DbEntry.Iso?.ToString("F0")));
            outerContainer.Children.Add(CommonHtml.Tags.InfoDivTag(DbEntry.Lens, "photo-detail", "lens", DbEntry.Lens));
            outerContainer.Children.Add(CommonHtml.Tags.InfoDivTag(DbEntry.FocalLength, "photo-detail", "focal-length",
                DbEntry.FocalLength));
            outerContainer.Children.Add(CommonHtml.Tags.InfoDivTag(DbEntry.CameraMake, "photo-detail", "camera-make",
                DbEntry.CameraMake));
            outerContainer.Children.Add(CommonHtml.Tags.InfoDivTag(DbEntry.CameraModel, "photo-detail", "camera-model",
                DbEntry.CameraModel));
            outerContainer.Children.Add(CommonHtml.Tags.InfoDivTag(DbEntry.License, "photo-detail", "license",
                DbEntry.License));

            return outerContainer;
        }

        public HtmlTag PhotoImageTag()
        {
            var imageTag = new HtmlTag("img").AddClass("single-photo").Attr("srcset", Photos.SrcSetString())
                .Attr("src", Photos.DisplayImage.SiteUrl).Attr("loading", "lazy");

            if (!string.IsNullOrWhiteSpace(DbEntry.AltText)) imageTag.Attr("alt", DbEntry.AltText);

            return imageTag;
        }

        public HtmlTag LocalDisplayPhotoImageTag()
        {
            var imageTag = new HtmlTag("img").AddClass("single-photo")
                .Attr("src", $"file://{Photos.DisplayImage.File.FullName}").Attr("loading", "lazy");

            if (!string.IsNullOrWhiteSpace(DbEntry.AltText)) imageTag.Attr("alt", DbEntry.AltText);

            return imageTag;
        }

        public HtmlTag LocalPhotoFigureTag()
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            figureTag.Children.Add(LocalDisplayPhotoImageTag());
            figureTag.Children.Add(PhotoFigCaptionTag());
            return figureTag;
        }

        public HtmlTag PhotoFigureWithLinkToPageTag()
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            var linkTag = new LinkTag(string.Empty, PageUrl);
            linkTag.Children.Add(PhotoImageTag());
            figureTag.Children.Add(linkTag);
            figureTag.Children.Add(PhotoFigCaptionTag());
            return figureTag;
        }

        public HtmlTag PhotoFigureTag()
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            figureTag.Children.Add(PhotoImageTag());
            figureTag.Children.Add(PhotoFigCaptionTag());
            return figureTag;
        }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;

            var htmlString = TransformText();

            var htmlFileInfo =
                new FileInfo(
                    $"{Path.Combine(settings.LocalSitePhotoContentDirectory(DbEntry).FullName, DbEntry.Slug)}.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }

        public HtmlTag CreatedDiv()
        {
            var createdByDiv = new DivTag();
            createdByDiv.AddClass("created-by-container");

            createdByDiv.Children.Add(new HtmlTag("div").AddClass("created-title"));

            createdByDiv.Children.Add(new HtmlTag("p").AddClass("created-by-content").Text(
                $"Page Created by {DbEntry.CreatedBy}, {DbEntry.CreatedOn:M/d/yyyy}"));

            return createdByDiv;
        }

        public HtmlTag SiteNameFooterDiv()
        {
            var createdByDiv = new DivTag().AddClass("site-name-footer-container");
            createdByDiv.Children.Add(CommonHtml.HorizontalRule.StandardRule());

            createdByDiv.Children.Add(new DivTag().AddClass("site-name-footer-content").Text($"{SiteName}"));

            return createdByDiv;
        }

        public HtmlTag UpdateDiv()
        {
            if (DbEntry.LastUpdatedOn == null) return HtmlTag.Empty();

            if (DbEntry.CreatedOn.Date == DbEntry.LastUpdatedOn.Value.Date &&
                string.IsNullOrWhiteSpace(DbEntry.UpdateNotes) &&
                DbEntry.CreatedBy == DbEntry.LastUpdatedBy) return HtmlTag.Empty();

            var updateNotesDiv = new DivTag().AddClass("update-notes-container");

            updateNotesDiv.Children.Add(CommonHtml.HorizontalRule.StandardRule());

            var headingTag = new HtmlTag("div").AddClass("update-notes-title").Text("Updates:");

            var updateNotesContentContainer = new DivTag().AddClass("update-notes-content");

            updateNotesContentContainer.Children.Add(new HtmlTag("p").AddClass("update-by-and-on-content")
                .Text($"{DbEntry.LastUpdatedBy}, {DbEntry.LastUpdatedOn.Value:M/d/yyyy}"));

            if (!string.IsNullOrWhiteSpace(DbEntry.UpdateNotes))
            {
                var updateNotesHtml = ContentProcessor.ContentHtml(DbEntry.UpdateNotesFormat, DbEntry.UpdateNotes);
                if (updateNotesHtml.success)
                {
                    updateNotesContentContainer.Encoded(false).Text(updateNotesHtml.output);
                }
            }

            updateNotesDiv.Children.Add(headingTag);
            updateNotesDiv.Children.Add(updateNotesContentContainer);

            return updateNotesDiv;
        }


        public HtmlTag PhotoFigCaptionTag()
        {
            if (string.IsNullOrWhiteSpace(DbEntry.Summary)) return HtmlTag.Empty();

            var figCaptionTag = new HtmlTag("figcaption");
            figCaptionTag.AddClass("single-photo-caption");

            var summaryStringList = new List<string>();

            //var titleSummaryString = DbEntry.Title;
            var titleSummaryString = string.Empty;

            if (!string.IsNullOrWhiteSpace(DbEntry.Summary))
            {
                //titleSummaryString += ": ";
                if (!DbEntry.Summary.Trim().EndsWith(".")) titleSummaryString += $"{DbEntry.Summary.Trim()}.";
                else titleSummaryString += $"{DbEntry.Summary.Trim()}";
            }

            summaryStringList.Add(titleSummaryString);

            summaryStringList.Add($"{DbEntry.PhotoCreatedBy}.");
            summaryStringList.Add($"{DbEntry.PhotoCreatedOn:M/d/yyyy}.");

            figCaptionTag.Text(string.Join(" ", summaryStringList));

            return figCaptionTag;
        }
    }
}