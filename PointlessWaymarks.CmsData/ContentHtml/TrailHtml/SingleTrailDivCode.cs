﻿using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.TrailHtml;

public partial class SingleTrailDiv
{
    public SingleTrailDiv(TrailContent dbEntry)
    {
        DbEntry = dbEntry;

        var settings = UserSettingsSingleton.CurrentSettings();
        SiteUrl = settings.SiteUrl();
        SiteName = settings.SiteName;
        PageUrl = settings.TrailPageUrl(DbEntry);
    }

    public TrailContent DbEntry { get; set; }
    public DateTime? GenerationVersion { get; set; }
    public string PageUrl { get; set; }
    public string SiteName { get; set; }
    public string SiteUrl { get; set; }
}