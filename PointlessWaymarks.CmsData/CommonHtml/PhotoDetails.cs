using HtmlTags;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class PhotoDetails
{
    public static HtmlTag PhotoDetailsDiv(PhotoContent dbEntry)
    {
        var outerContainer = new DivTag().AddClasses("photo-details-container", "info-list-container");

        outerContainer.Children.Add(new DivTag().AddClasses("photo-detail-label-tag", "info-list-label").Text("Details:"));

        outerContainer.Children.Add(Tags.InfoDivTag(dbEntry.Aperture, "photo-detail", "aperture",
            dbEntry.Aperture));
        outerContainer.Children.Add(Tags.InfoDivTag(dbEntry.ShutterSpeed, "photo-detail", "shutter-speed",
            dbEntry.ShutterSpeed));
        //InfoDivTag guards against null and empty but because we put ISO in the string guard against blank (and sanity check) ISO.
        if (dbEntry.Iso is > 0)
            outerContainer.Children.Add(Tags.InfoDivTag($"ISO {dbEntry.Iso?.ToString("F0")}", "photo-detail", "iso",
                dbEntry.Iso?.ToString("F0")));
        outerContainer.Children.Add(Tags.InfoDivTag(dbEntry.Lens, "photo-detail", "lens", dbEntry.Lens));
        outerContainer.Children.Add(Tags.InfoDivTag(dbEntry.FocalLength, "photo-detail", "focal-length",
            dbEntry.FocalLength));
        outerContainer.Children.Add(Tags.InfoDivTag(dbEntry.CameraMake, "photo-detail", "camera-make",
            dbEntry.CameraMake));
        outerContainer.Children.Add(Tags.InfoDivTag(dbEntry.CameraModel, "photo-detail", "camera-model",
            dbEntry.CameraModel));
        outerContainer.Children.Add(Tags.InfoDivTag(dbEntry.License, "photo-detail", "license", dbEntry.License));

        //Return empty if there are no details
        return outerContainer.Children.Count(x => !x.IsEmpty()) > 1 ? outerContainer : HtmlTag.Empty();
    }
}