using HtmlTags;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.ImageHtml;

public static class ImageParts
{
    public static HtmlTag ImageLocationDiv(ImageContent dbEntry)
    {
        if (!dbEntry.HasLocation()) return HtmlTag.Empty();
        
        var outerContainer = new DivTag().AddClasses("image-location-container", "info-list-container");
        
        outerContainer.Children.Add(new DivTag().AddClasses("image-location-label-tag", "info-list-label")
            .Text("Location:"));
        
        if (dbEntry is { Latitude: not null, Longitude: not null, ShowLocation: true })
            outerContainer.Children.Add(Tags.InfoLinkDivTag(
                PointParts.OsmCycleMapsLatLongUrl(dbEntry.Latitude.Value, dbEntry.Longitude.Value),
                $"{dbEntry.Latitude.Value:F5}, {dbEntry.Longitude.Value:F5}", "image-detail",
                "lat-long-decimal-degrees",
                $"{dbEntry.Latitude.Value:F5}, {dbEntry.Longitude.Value:F5}"));
        if (dbEntry is { Elevation: not null, ShowLocation: true })
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.Elevation.Value:N0}'",
                "image-detail", "elevation-in-feet", dbEntry.Elevation.Value.ToString("F0")));
        
        //Return empty if there are no details
        return outerContainer.Children.Count(x => !x.IsEmpty()) > 1 ? outerContainer : HtmlTag.Empty();
    }
    
    public static async Task<HtmlTag> ImageSourceNotesDivTag(ImageContent dbEntry,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(dbEntry.BodyContent)) return HtmlTag.Empty();
        
        var sourceNotesContainer = new DivTag().AddClass("image-source-notes-container");
        var sourceNotes = new DivTag().AddClass("image-source-notes-content").Encoded(false).Text(
            ContentProcessing.ProcessContent(
                await BracketCodeCommon.ProcessCodesForSite($"Source: {dbEntry.BodyContent}", progress)
                    .ConfigureAwait(false),
                ContentFormatEnum.MarkdigMarkdown01));
        sourceNotesContainer.Children.Add(sourceNotes);
        
        return sourceNotesContainer;
    }
}