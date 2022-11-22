using HtmlTags;
using PointlessWaymarks.CmsData.Database.Models;
using Windows.Storage;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentHtml.FileHtml;

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

    public static async Task<HtmlTag> EmbedFileTag(FileContent content)
    {
        if (!content.EmbedFile) return HtmlTag.Empty();

        var embedContainer = new HtmlTag("figure").AddClass("file-embed-container");

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
        else if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                 content.OriginalFileName.TrimNullToEmpty().EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                 content.OriginalFileName.TrimNullToEmpty().EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
        {
            var embedTag = new HtmlTag("video").Attr("src", settings.FileDownloadUrl(content))
                .Attr("controls", "controls").Title(content.Title);

            var siteFile = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentFile(content);

            if (siteFile is { Exists: true })
            {
                var file = await StorageFile.GetFileFromPathAsync(siteFile.FullName);
                var videoProperties = await file.Properties.GetVideoPropertiesAsync();
                var width = videoProperties.Width;

                embedTag.Style("max-width", $"{width}px");
                embedTag.AddClass("video-embed");

                embedContainer.Children.Add(embedTag);
            }
        }
        else if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            var embedTag = new HtmlTag("embed").Attr("src", settings.FileDownloadUrl(content))
                .Attr("type", "audio/mpeg").Title(content.Title).AddClass("file-embed");
            embedContainer.Children.Add(embedTag);
        }
        else if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            var embedTag = new HtmlTag("embed").Attr("src", settings.FileDownloadUrl(content))
                .Attr("type", "audio/vnd.wav").Title(content.Title).AddClass("file-embed");
            embedContainer.Children.Add(embedTag);
        }

        if (!string.IsNullOrWhiteSpace(content.Summary))
        {
            var figCaptionTag = new HtmlTag("figcaption");
            figCaptionTag.AddClass("file-embed-caption");
            figCaptionTag.Text(content.Summary);
            embedContainer.Children.Add(figCaptionTag);
        }

        return embedContainer;
    }
}