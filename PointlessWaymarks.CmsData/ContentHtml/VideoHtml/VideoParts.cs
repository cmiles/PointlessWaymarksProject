using Windows.Storage;
using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.Database.Models;
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
    
    public static HtmlTag VideoLocationDiv(VideoContent dbEntry)
    {
        if (!dbEntry.HasLocation()) return HtmlTag.Empty();
        
        var outerContainer = new DivTag().AddClasses("video-location-container", "info-list-container");
        
        outerContainer.Children.Add(new DivTag().AddClasses("video-location-label-tag", "info-list-label")
            .Text("Location:"));
        
        if (dbEntry is { Latitude: not null, Longitude: not null, ShowLocation: true })
            outerContainer.Children.Add(Tags.InfoLinkDivTag(
                PointParts.OsmCycleMapsLatLongUrl(dbEntry.Latitude.Value, dbEntry.Longitude.Value),
                $"{dbEntry.Latitude.Value:F5}, {dbEntry.Longitude.Value:F5}", "video-detail",
                "lat-long-decimal-degrees",
                $"{dbEntry.Latitude.Value:F5}, {dbEntry.Longitude.Value:F5}"));
        if (dbEntry is { Elevation: not null, ShowLocation: true })
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.Elevation.Value:N0}'",
                "video-detail", "elevation-in-feet", dbEntry.Elevation.Value.ToString("F0")));
        
        //Return empty if there are no details
        return outerContainer.Children.Count(x => !x.IsEmpty()) > 1 ? outerContainer : HtmlTag.Empty();
    }
}