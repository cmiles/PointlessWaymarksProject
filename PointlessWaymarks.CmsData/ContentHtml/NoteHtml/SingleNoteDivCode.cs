using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.NoteHtml;

public partial class SingleNoteDiv
{
    public SingleNoteDiv(NoteContent dbEntry)
    {
        DbEntry = dbEntry;

        var settings = UserSettingsSingleton.CurrentSettings();
        SiteUrl = settings.SiteUrl();
        SiteName = settings.SiteName;
        PageUrl = settings.NotePageUrl(DbEntry);
    }

    public NoteContent DbEntry { get; set; }
    public DateTime? GenerationVersion { get; set; }
    public string PageUrl { get; set; }
    public string SiteName { get; set; }
    public string SiteUrl { get; set; }
}