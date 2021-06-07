using System;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.FileHtml
{
    public partial class SingleFileDiv
    {
        public SingleFileDiv(FileContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.FilePageUrl(DbEntry);
        }

        public FileContent DbEntry { get; set; }

        public DateTime? GenerationVersion { get; set; }

        public string PageUrl { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }
    }
}