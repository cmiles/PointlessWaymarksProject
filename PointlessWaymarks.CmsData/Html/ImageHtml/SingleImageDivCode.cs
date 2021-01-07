using System;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.CommonHtml;

namespace PointlessWaymarks.CmsData.Html.ImageHtml
{
    public partial class SingleImageDiv
    {
        public SingleImageDiv(ImageContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.ImagePageUrl(DbEntry);

            PictureInformation = new PictureSiteInformation(DbEntry.ContentId);
        }

        public ImageContent DbEntry { get; set; }

        public DateTime? GenerationVersion { get; set; }

        public string PageUrl { get; set; }

        public PictureSiteInformation PictureInformation { get; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }
    }
}