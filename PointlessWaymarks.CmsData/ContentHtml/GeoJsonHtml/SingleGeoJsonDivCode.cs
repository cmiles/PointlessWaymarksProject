using System;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml
{
    public partial class SingleGeoJsonDiv
    {
        public SingleGeoJsonDiv(GeoJsonContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.GeoJsonPageUrl(DbEntry);
        }

        public GeoJsonContent DbEntry { get; set; }

        public DateTime? GenerationVersion { get; set; }

        public string PageUrl { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }
    }
}