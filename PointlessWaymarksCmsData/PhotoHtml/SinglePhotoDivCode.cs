using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.PhotoHtml
{
    public partial class SinglePhotoDiv
    {
        public SinglePhotoDiv(PhotoContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PhotoPageUrl(DbEntry);

            PictureInformation = new PictureSiteInformation(DbEntry.ContentId);
        }

        public PhotoContent DbEntry { get; set; }

        public string PageUrl { get; set; }

        public PictureSiteInformation PictureInformation { get; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }
    }
}