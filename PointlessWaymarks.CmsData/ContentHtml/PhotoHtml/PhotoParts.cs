using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;

public static class PhotoParts
{
    public static HtmlTag PhotoDetailsDiv(PhotoContent dbEntry)
    {
        var outerContainer = new DivTag().AddClasses("photo-details-container", "info-list-container");
        
        outerContainer.Children.Add(new DivTag().AddClasses("photo-detail-label-tag", "info-list-label")
            .Text("Details:"));
        
        outerContainer.Children.Add(Tags.InfoTextDivTag(dbEntry.Aperture, "photo-detail", "aperture",
            dbEntry.Aperture));
        outerContainer.Children.Add(Tags.InfoTextDivTag(dbEntry.ShutterSpeed, "photo-detail", "shutter-speed",
            dbEntry.ShutterSpeed));
        //InfoDivTag guards against null and empty but because we put ISO in the string guard against blank (and sanity check) ISO.
        if (dbEntry.Iso is > 0)
            outerContainer.Children.Add(Tags.InfoTextDivTag($"ISO {dbEntry.Iso?.ToString("F0")}", "photo-detail", "iso",
                dbEntry.Iso?.ToString("F0")));
        outerContainer.Children.Add(Tags.InfoTextDivTag(dbEntry.Lens, "photo-detail", "lens", dbEntry.Lens));
        outerContainer.Children.Add(Tags.InfoTextDivTag(dbEntry.FocalLength, "photo-detail", "focal-length",
            dbEntry.FocalLength));
        outerContainer.Children.Add(Tags.InfoTextDivTag(dbEntry.CameraMake, "photo-detail", "camera-make",
            dbEntry.CameraMake));
        outerContainer.Children.Add(Tags.InfoTextDivTag(dbEntry.CameraModel, "photo-detail", "camera-model",
            dbEntry.CameraModel));
        outerContainer.Children.Add(Tags.InfoTextDivTag(dbEntry.License, "photo-detail", "license", dbEntry.License));
        if (dbEntry is { Latitude: not null, Longitude: not null, ShowLocation: true })
            outerContainer.Children.Add(Tags.InfoLinkDivTag(
                PointParts.OsmCycleMapsLatLongUrl(dbEntry.Latitude.Value, dbEntry.Longitude.Value),
                $"{dbEntry.Latitude.Value:F5}, {dbEntry.Longitude.Value:F5}", "photo-detail",
                "lat-long-decimal-degrees",
                $"{dbEntry.Latitude.Value:F5}, {dbEntry.Longitude.Value:F5}"));
        if (dbEntry is { Elevation: not null, ShowLocation: true })
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.Elevation.Value:N0}'",
                "photo-detail", "elevation-in-feet", dbEntry.Elevation.Value.ToString("F0")));
        if (dbEntry is { PhotoDirection: not null, ShowLocation: true })
            outerContainer.Children.Add(Tags.InfoTextDivTag($"{dbEntry.PhotoDirection.Value:N0}\u00B0",
                "photo-detail", "photo-direction", dbEntry.PhotoDirection.Value.ToString("F0")));

        //Return empty if there are no details
        return outerContainer.Children.Count(x => !x.IsEmpty()) > 1 ? outerContainer : HtmlTag.Empty();
    }
}