using Windows.Storage;
using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.Database.Models;
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
    
    public static HtmlTag FileLocationDiv(FileContent dbEntry)
    {
        if (!dbEntry.HasLocation()) return HtmlTag.Empty();
        
        var outerContainer = new DivTag().AddClasses("file-location-container", "info-list-container");
        
        outerContainer.Children.Add(new DivTag().AddClasses("file-location-label-tag", "info-list-label")
            .Text("Location:"));
        
        if (dbEntry is { Latitude: not null, Longitude: not null, ShowLocation: true })
            outerContainer.Children.Add(Tags.InfoLinkDivTag(
                PointParts.OsmCycleMapsLatLongUrl(dbEntry.Latitude.Value, dbEntry.Longitude.Value),
                $"{dbEntry.Latitude.Value:F5}, {dbEntry.Longitude.Value:F5}", "file-detail",
                "lat-long-decimal-degrees",
                $"{dbEntry.Latitude.Value:F5}, {dbEntry.Longitude.Value:F5}"));
        if (dbEntry is { Elevation: not null, ShowLocation: true })
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.Elevation.Value:N0}'",
                "file-detail", "elevation-in-feet", dbEntry.Elevation.Value.ToString("F0")));
        
        //Return empty if there are no details
        return outerContainer.Children.Count(x => !x.IsEmpty()) > 1 ? outerContainer : HtmlTag.Empty();
    }
}