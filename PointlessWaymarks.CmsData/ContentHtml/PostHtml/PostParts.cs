using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.PostHtml;

public static class PostParts
{
    public static HtmlTag PostLocationDiv(PostContent dbEntry)
    {
        if (!dbEntry.HasLocation()) return HtmlTag.Empty();
        
        var outerContainer = new DivTag().AddClasses("post-location-container", "info-list-container");
        
        outerContainer.Children.Add(new DivTag().AddClasses("post-location-label-tag", "info-list-label")
            .Text("Location:"));
        
        if (dbEntry is { Latitude: not null, Longitude: not null, ShowLocation: true })
            outerContainer.Children.Add(Tags.InfoLinkDivTag(
                PointParts.OsmCycleMapsLatLongUrl(dbEntry.Latitude.Value, dbEntry.Longitude.Value),
                $"{dbEntry.Latitude.Value:F5}, {dbEntry.Longitude.Value:F5}", "post-detail",
                "lat-long-decimal-degrees",
                $"{dbEntry.Latitude.Value:F5}, {dbEntry.Longitude.Value:F5}"));
        if (dbEntry is { Elevation: not null, ShowLocation: true })
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.Elevation.Value:N0}'",
                "post-detail", "elevation-in-feet", dbEntry.Elevation.Value.ToString("F0")));
        
        //Return empty if there are no details
        return outerContainer.Children.Count(x => !x.IsEmpty()) > 1 ? outerContainer : HtmlTag.Empty();
    }
}