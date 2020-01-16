using System.IO;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.ImageHtml
{
    public partial class SingleImagePage
    {
        public SingleImagePage(ImageContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsUtilities.ReadSettings().Result;
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.ImagePageUrl(DbEntry);

            PictureAsset = new PictureSiteInformation(DbEntry.ContentId);
        }

        public ImageContent DbEntry { get; }
        public string PageUrl { get; }
        public PictureSiteInformation PictureAsset { get; }
        public string SiteName { get; }
        public string SiteUrl { get; }


        public void WriteLocalHtml()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;

            var htmlString = TransformText();

            var htmlFileInfo =
                new FileInfo(
                    $"{Path.Combine(settings.LocalSiteImageContentDirectory(DbEntry).FullName, DbEntry.Slug)}.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}