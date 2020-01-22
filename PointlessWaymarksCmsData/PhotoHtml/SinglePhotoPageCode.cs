using System.IO;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.PhotoHtml
{
    public partial class SinglePhotoPage
    {
        public SinglePhotoPage(PhotoContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PhotoPageUrl(DbEntry);

            PictureAsset = new PictureSiteInformation(DbEntry.ContentId);
        }

        public PhotoContent DbEntry { get; }
        public string PageUrl { get; }
        public PictureSiteInformation PictureAsset { get; }
        public string SiteName { get; }
        public string SiteUrl { get; }


        public HtmlTag PhotoDetailsDiv()
        {
            var outerContainer = new DivTag().AddClass("photo-details-container");

            outerContainer.Children.Add(new DivTag().AddClass("photo-detail-label-tag").Text("Details:"));

            outerContainer.Children.Add(Tags.InfoDivTag(DbEntry.Aperture, "photo-detail", "aperture",
                DbEntry.Aperture));
            outerContainer.Children.Add(Tags.InfoDivTag(DbEntry.ShutterSpeed, "photo-detail", "shutter-speed",
                DbEntry.ShutterSpeed));
            outerContainer.Children.Add(Tags.InfoDivTag($"ISO {DbEntry.Iso?.ToString("F0")}", "photo-detail", "iso",
                DbEntry.Iso?.ToString("F0")));
            outerContainer.Children.Add(Tags.InfoDivTag(DbEntry.Lens, "photo-detail", "lens", DbEntry.Lens));
            outerContainer.Children.Add(Tags.InfoDivTag(DbEntry.FocalLength, "photo-detail", "focal-length",
                DbEntry.FocalLength));
            outerContainer.Children.Add(Tags.InfoDivTag(DbEntry.CameraMake, "photo-detail", "camera-make",
                DbEntry.CameraMake));
            outerContainer.Children.Add(Tags.InfoDivTag(DbEntry.CameraModel, "photo-detail", "camera-model",
                DbEntry.CameraModel));
            outerContainer.Children.Add(Tags.InfoDivTag(DbEntry.License, "photo-detail", "license", DbEntry.License));

            return outerContainer;
        }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var htmlString = TransformText();

            var htmlFileInfo =
                new FileInfo(
                    $"{Path.Combine(settings.LocalSitePhotoContentDirectory(DbEntry).FullName, DbEntry.Slug)}.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}