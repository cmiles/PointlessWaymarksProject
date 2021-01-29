using System;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.PointHtml
{
    public partial class SinglePointDiv
    {
        public SinglePointDiv(PointContentDto dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PointPageUrl(DbEntry);
        }

        public PointContentDto DbEntry { get; set; }

        public DateTime? GenerationVersion { get; set; }

        public string PageUrl { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }
    }
}