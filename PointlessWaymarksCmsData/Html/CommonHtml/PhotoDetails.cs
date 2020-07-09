using HtmlTags;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Html.CommonHtml
{
    public static class PhotoDetails
    {
        public static HtmlTag PhotoDetailsDiv(PhotoContent dbEntry)
        {
            var outerContainer = new DivTag().AddClass("photo-details-container");

            outerContainer.Children.Add(new DivTag().AddClass("photo-detail-label-tag").Text("Details:"));

            outerContainer.Children.Add(Tags.InfoDivTag(dbEntry.Aperture, "photo-detail", "aperture",
                dbEntry.Aperture));
            outerContainer.Children.Add(Tags.InfoDivTag(dbEntry.ShutterSpeed, "photo-detail", "shutter-speed",
                dbEntry.ShutterSpeed));
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

            return outerContainer;
        }
    }
}