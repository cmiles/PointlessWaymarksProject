using HtmlTags;
using PointlessWaymarks.CmsData.Database.Models;
using Windows.Storage;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentHtml.VideoHtml;

public static class VideoParts
{
    public static async Task<HtmlTag> EmbedVideoTag(VideoContent content)
    {
        var embedContainer = new HtmlTag("figure").AddClass("file-embed-container");

        var settings = UserSettingsSingleton.CurrentSettings();

        if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".mov", StringComparison.OrdinalIgnoreCase))
        {
            var embedTag = new HtmlTag("embed").Attr("src", settings.VideoDownloadUrl(content))
                .Attr("type", "video/quicktime").Title(content.Title).AddClass("file-embed");
            embedContainer.Children.Add(embedTag);
        }
        else if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                 content.OriginalFileName.TrimNullToEmpty().EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                 content.OriginalFileName.TrimNullToEmpty().EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
        {
            var embedTag = new HtmlTag("video").Attr("src", settings.VideoDownloadUrl(content))
                .Attr("controls", "controls").Title(content.Title);

            var siteFile = UserSettingsSingleton.CurrentSettings().LocalSiteVideoContentFile(content);

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