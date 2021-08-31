using System;
using HtmlTags;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.FileHtml
{
    public static class FileParts
    {
        public static HtmlTag DownloadLinkTag(FileContent content)
        {
            if (!content.PublicDownloadLink) return HtmlTag.Empty();

            var downloadLinkContainer = new DivTag().AddClass("file-download-container");

            var settings = UserSettingsSingleton.CurrentSettings();
            var downloadLink =
                new LinkTag("Download", settings.FileDownloadUrl(content)).AddClass("file-download-link");
            downloadLinkContainer.Children.Add(downloadLink);

            return downloadLinkContainer;
        }

        public static HtmlTag EmbedFileTag(FileContent content)
        {
            if (!content.EmbedFile) return HtmlTag.Empty();

            var embedContainer = new DivTag().AddClass("file-embed-container");

            var settings = UserSettingsSingleton.CurrentSettings();

            if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var embedTag = new HtmlTag("embed").Attr("src", settings.FileDownloadUrl(content))
                    .Attr("type", "application/pdf").Title(content.Title).AddClass("file-embed");
                embedContainer.Children.Add(embedTag);
            }
            else if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".mov", StringComparison.OrdinalIgnoreCase))
            {
                var embedTag = new HtmlTag("embed").Attr("src", settings.FileDownloadUrl(content))
                    .Attr("type", "video/quicktime").Title(content.Title).AddClass("file-embed");
                embedContainer.Children.Add(embedTag);
            }
            else if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".webm", StringComparison.OrdinalIgnoreCase))
            {
                var embedTag = new HtmlTag("embed").Attr("src", settings.FileDownloadUrl(content))
                    .Attr("type", "video/webm").Title(content.Title).AddClass("file-embed");
                embedContainer.Children.Add(embedTag);
            }
            else if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                var embedTag = new HtmlTag("embed").Attr("src", settings.FileDownloadUrl(content))
                    .Attr("type", "video/mp4").Title(content.Title).AddClass("file-embed");
                embedContainer.Children.Add(embedTag);
            }
            else if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                var embedTag = new HtmlTag("embed").Attr("src", settings.FileDownloadUrl(content))
                    .Attr("type", "audio/mpeg").Title(content.Title).AddClass("file-embed");
                embedContainer.Children.Add(embedTag);
            }
            else if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                var embedTag = new HtmlTag("embed").Attr("src", settings.FileDownloadUrl(content))
                    .Attr("type", "audio/ogg").Title(content.Title).AddClass("file-embed");
                embedContainer.Children.Add(embedTag);
            }
            else if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                var embedTag = new HtmlTag("embed").Attr("src", settings.FileDownloadUrl(content))
                    .Attr("type", "audio/vnd.wav").Title(content.Title).AddClass("file-embed");
                embedContainer.Children.Add(embedTag);
            }

            return embedContainer;
        }
    }
}