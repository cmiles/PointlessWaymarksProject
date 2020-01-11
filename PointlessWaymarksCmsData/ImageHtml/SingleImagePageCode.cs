using System.Collections.Generic;
using System.IO;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.PhotoHtml;

namespace PointlessWaymarksCmsData.ImageHtml
{
    public partial class SingleImagePage
    {
        public SingleImagePage(ImageContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsUtilities.ReadSettings().Result;
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.ImagePageUrl(DbEntry);

            Images = ImageFiles.ProcessImagesInDirectory(DbEntry);
        }

        public ImageContent DbEntry { get; set; }

        public ImageDirectoryContentsInformation Images { get; set; }
        public string PageUrl { get; set; }
        public string SiteName { get; set; }
        public string SiteUrl { get; set; }

        public HtmlTag CreatedDiv()
        {
            var createdByDiv = new DivTag();
            createdByDiv.AddClass("created-by-container");

            createdByDiv.Children.Add(new HtmlTag("div").AddClass("created-title"));

            createdByDiv.Children.Add(new HtmlTag("p").AddClass("created-by-content").Text(
                $"Page Created by {DbEntry.CreatedBy}, {DbEntry.CreatedOn:M/d/yyyy}"));

            return createdByDiv;
        }

        public HtmlTag ImageFigCaptionTag()
        {
            if (string.IsNullOrWhiteSpace(DbEntry.Summary)) return HtmlTag.Empty();

            var figCaptionTag = new HtmlTag("figcaption");
            figCaptionTag.AddClass("single-image-caption");

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

            figCaptionTag.Text(string.Join(" ", summaryStringList));

            return figCaptionTag;
        }

        public HtmlTag ImageFigureTag()
        {
            var figureTag = new HtmlTag("figure").AddClass("single-image-container");
            figureTag.Children.Add(ImageImageTag());
            figureTag.Children.Add(ImageFigCaptionTag());
            return figureTag;
        }

        public HtmlTag ImageFigureWithLinkToPageTag()
        {
            var figureTag = new HtmlTag("figure").AddClass("single-image-container");
            var linkTag = new LinkTag(string.Empty, PageUrl);
            linkTag.Children.Add(ImageImageTag());
            figureTag.Children.Add(linkTag);
            figureTag.Children.Add(ImageFigCaptionTag());
            return figureTag;
        }

        public HtmlTag ImageImageTag()
        {
            var imageTag = new HtmlTag("img").AddClass("single-image").Attr("srcset", Images.SrcSetString())
                .Attr("src", Images.DisplayImage.SiteUrl).Attr("loading", "lazy");

            if (!string.IsNullOrWhiteSpace(DbEntry.AltText)) imageTag.Attr("alt", DbEntry.AltText);

            return imageTag;
        }

        public HtmlTag LocalDisplayImageImageTag()
        {
            var imageTag = new HtmlTag("img").AddClass("single-image")
                .Attr("src", $"file://{Images.DisplayImage.File.FullName}").Attr("loading", "lazy");

            if (!string.IsNullOrWhiteSpace(DbEntry.AltText)) imageTag.Attr("alt", DbEntry.AltText);

            return imageTag;
        }

        public HtmlTag LocalImageFigureTag()
        {
            var figureTag = new HtmlTag("figure").AddClass("single-image-container");
            figureTag.Children.Add(LocalDisplayImageImageTag());
            figureTag.Children.Add(ImageFigCaptionTag());
            return figureTag;
        }

        public HtmlTag SiteNameFooterDiv()
        {
            var createdByDiv = new DivTag().AddClass("site-name-footer-container");
            createdByDiv.Children.Add(HorizontalRule.StandardRule());

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

            updateNotesDiv.Children.Add(HorizontalRule.StandardRule());

            var headingTag = new HtmlTag("div").AddClass("update-notes-title").Text("Updates:");

            var updateNotesContentContainer = new DivTag().AddClass("update-notes-content");

            updateNotesContentContainer.Children.Add(new HtmlTag("p").AddClass("update-by-and-on-content")
                .Text($"{DbEntry.LastUpdatedBy}, {DbEntry.LastUpdatedOn.Value:M/d/yyyy}"));

            if (!string.IsNullOrWhiteSpace(DbEntry.UpdateNotes))
            {
                var updateNotesHtml = ContentProcessor.ContentHtml(DbEntry.UpdateNotesFormat, DbEntry.UpdateNotes);
                if (updateNotesHtml.success) updateNotesContentContainer.Encoded(false).Text(updateNotesHtml.output);
            }

            updateNotesDiv.Children.Add(headingTag);
            updateNotesDiv.Children.Add(updateNotesContentContainer);

            return updateNotesDiv;
        }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;

            var htmlString = TransformText();

            var htmlFileInfo =
                new FileInfo(
                    $"{Path.Combine(settings.LocalSiteImageContentDirectory(DbEntry).FullName, DbEntry.Slug)}.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}