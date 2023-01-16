using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.VideoHtml;

public partial class SingleVideoDiv
{
    public SingleVideoDiv(VideoContent dbEntry)
    {
        DbEntry = dbEntry;

        var settings = UserSettingsSingleton.CurrentSettings();
        SiteUrl = settings.SiteUrl();
        SiteName = settings.SiteName;
        PageUrl = settings.VideoPageUrl(DbEntry);
    }

    public VideoContent DbEntry { get; set; }

    public DateTime? GenerationVersion { get; set; }

    public string PageUrl { get; set; }

    public string SiteName { get; set; }

    public string SiteUrl { get; set; }
}