using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlTags;
using HtmlTags.Extended.TagBuilders;
using TheLemmonWorkshopData.Models;

namespace TheLemmonWorkshopData.TextTransforms
{
    public partial class SinglePhotoPage
    {
        public PhotoContent DbEntry { get; set; }
        public string SiteUrl { get; set; }
        public string SiteName { get; set; }
        public string PageUrl { get; set; }

        public ImageFileInformation DisplayImage { get; set; }

        public ImageFileInformation LargeImage { get; set; }
        public List<ImageFileInformation> SrcsetImages { get; set; }

        public string SrcSetString()
        {
            return string.Join(", ",
                SrcsetImages.OrderByDescending(x => x.Width).Select(x => $"{x.FileName} {x.Width}w"));
        }

        public HtmlTag PhotoDetailsDiv()
        {
            var outerContainer = new DivTag();
            outerContainer.AddClass("photo-details-container");
            
            outerContainer.Children.Add(new DivTag().AddClass("photo-detail-label-tag").Text("Details:"));

            outerContainer.Children.Add(InfoDivTag(DbEntry.Aperture, "photo-detail", "aperture", DbEntry.Aperture));
            outerContainer.Children.Add(InfoDivTag(DbEntry.ShutterSpeed, "photo-detail", "shutter-speed",
                DbEntry.ShutterSpeed));
            outerContainer.Children.Add(InfoDivTag($"ISO {DbEntry.Iso?.ToString("F0")}", "photo-detail", "iso",
                DbEntry.Iso?.ToString("F0")));
            outerContainer.Children.Add(InfoDivTag(DbEntry.Lens, "photo-detail", "lens", DbEntry.Lens));
            outerContainer.Children.Add(InfoDivTag(DbEntry.FocalLength, "photo-detail", "focal-length",
                DbEntry.FocalLength));
            outerContainer.Children.Add(InfoDivTag(DbEntry.CameraMake, "photo-detail", "camera-make",
                DbEntry.CameraMake));
            outerContainer.Children.Add(InfoDivTag(DbEntry.CameraModel, "photo-detail", "camera-model",
                DbEntry.CameraModel));
            outerContainer.Children.Add(InfoDivTag(DbEntry.License, "photo-detail", "license", DbEntry.License));

            return outerContainer;
        }

        public HtmlTag InfoDivTag(string contents, string className, string dataType, string dataValue)
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

        public HtmlTag PhotoImageTag()
        {
            var imageTag = new HtmlTag("img");
            imageTag.Attr("srcset", SrcSetString());
            imageTag.Attr("src", DisplayImage.SiteUrl);
            imageTag.Attr("loading", "lazy");
            if(!string.IsNullOrWhiteSpace(DbEntry.AltText)) imageTag.Attr("alt", DbEntry.AltText);
            imageTag.AddClass("single-photo");

            return imageTag;
        }

        public HtmlTag TagsDiv()
        {
            var tagsContainer = new DivTag().AddClass("tags-container");
            
            if(string.IsNullOrWhiteSpace(DbEntry.Tags)) return HtmlTag.Empty();

            tagsContainer.Children.Add(new DivTag().Text("Tags:").AddClass("tag-detail-label-tag"));

            var tags = DbEntry.Tags.Split(",").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();

            if (!tags.Any()) return HtmlTag.Empty();
            
            foreach (var loopTag in tags)
            {
                tagsContainer.Children.Add(InfoDivTag(loopTag, "tag-detail", "tag", loopTag));
            }

            return tagsContainer;
        }

        public HtmlTag StandardHorizontalRule()
        {
            return new HtmlTag("hr").AddClass("standard-rule");
        }
        
        public HtmlTag CreatedDiv()
        {
            var createdByDiv = new DivTag();
            createdByDiv.AddClass("created-by-container");
            
            createdByDiv.Children.Add(new HtmlTag("div").AddClass("created-title").Text("Created:"));
            
            createdByDiv.Children.Add(new HtmlTag("p").AddClass("created-by-content").Text(
                $"{DbEntry.CreatedBy}, {DbEntry.CreatedOn:M/d/yyyy}"));

            return createdByDiv;
        }
        
        public HtmlTag SiteNameDiv()
        {
            var createdByDiv = new DivTag().AddClass("site-name-container");
            createdByDiv.Children.Add(StandardHorizontalRule());
            
            createdByDiv.Children.Add(new DivTag().AddClass("site-name-content").Text(
                $"{SiteName}"));

            return createdByDiv;
        }
        
        public HtmlTag UpdateDiv()
        {
            if(DbEntry.LastUpdatedOn == null) return HtmlTag.Empty();

            var updateNotesDiv = new DivTag().AddClass("update-notes-container");
            
            updateNotesDiv.Children.Add(StandardHorizontalRule());
            
            var headingTag = new HtmlTag("div").AddClass("update-notes-title").Text("Updates:");
            
            var updateNotesContentContainer = new DivTag().AddClass("update-notes-content");

            updateNotesContentContainer.Children.Add(new HtmlTag("p").AddClass("update-by-and-on-content").Text($"{DbEntry.LastUpdatedBy}, {DbEntry.LastUpdatedOn.Value:M/d/yyyy}"));

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
            figCaptionTag.AddClass("photo-caption");

            var summaryStringList = new List<string>();

            var titleSummaryString = DbEntry.Title;
            
            if (!string.IsNullOrWhiteSpace(DbEntry.Summary))
            {
                titleSummaryString += ": ";
                if (!DbEntry.Summary.Trim().EndsWith(".")) titleSummaryString += $"{DbEntry.Summary.Trim()}.";
                else titleSummaryString += $"{DbEntry.Summary.Trim()}";
            }
            
            summaryStringList.Add(titleSummaryString);

            summaryStringList.Add($"{DbEntry.PhotoCreatedBy}.");
            summaryStringList.Add($"{DbEntry.PhotoCreatedOn:M/d/yyyy}.");

            figCaptionTag.Text(string.Join(" ", summaryStringList));

            return figCaptionTag;
        }

        public void ProcessPhotosInDirectory(DirectoryInfo directoryInfo)
        {
            var baseFileNameList = DbEntry.OriginalFileName.Split(".").ToList();
            var baseFileName = string.Join("", baseFileNameList.Take(baseFileNameList.Count - 1));

            var fileVariants = directoryInfo.GetFiles().Where(x => x.Name.StartsWith($"{baseFileName}--")).ToList();

            var displayImageFile = fileVariants.Single(x => x.Name.Contains("--For-Display"));
            DisplayImage = new ImageFileInformation
            {
                FileName = displayImageFile.Name,
                SiteUrl = $@"//{SiteUrl}/Photos/{DbEntry.Folder}/{DbEntry.Slug}/{displayImageFile.Name}"
            };

            var srcsetImageFiles = fileVariants.Where(x => x.Name.Contains("--Sized"));
            SrcsetImages = srcsetImageFiles.Select(x => new ImageFileInformation
            {
                FileName = x.Name,
                Height =
                    int.Parse(Regex.Match(x.Name, @".*--(?<height>\d*)h.*", RegexOptions.Singleline)
                        .Groups["height"].Value),
                Width = int.Parse(Regex.Match(x.Name, @".*--(?<width>\d*)w.*", RegexOptions.Singleline)
                    .Groups["width"].Value),
                SiteUrl = $@"//{SiteUrl}/Photos/{DbEntry.Folder}/{DbEntry.Slug}/{x.Name}"
            }).ToList();

            LargeImage = SrcsetImages.OrderByDescending(x => Math.Max(x.Height, x.Width)).First();
        }
    }
}