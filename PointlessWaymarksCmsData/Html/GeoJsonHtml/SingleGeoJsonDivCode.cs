using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Html.GeoJsonHtml
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

        public string PageUrl { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }
    }
}