using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Html.PointHtml
{
    public partial class SinglePointDiv
    {
        public SinglePointDiv(PointContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PointPageUrl(DbEntry);
        }

        public PointContent DbEntry { get; set; }

        public string PageUrl { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }
    }
}