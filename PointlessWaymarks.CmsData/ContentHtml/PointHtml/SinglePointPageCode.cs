using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentHtml.PointHtml;

public partial class SinglePointPage
{
    public SinglePointPage(PointContentDto dbEntry)
    {
        DbEntry = dbEntry;

        var settings = UserSettingsSingleton.CurrentSettings();
        SiteUrl = settings.SiteUrl();
        SiteName = settings.SiteName;
        PageUrl = settings.PointPageUrl(DbEntry);
        LangAttribute = settings.SiteLangAttribute;
        DirAttribute = settings.SiteDirectionAttribute;

        if (DbEntry.MainPicture != null) MainImage = new PictureSiteInformation(DbEntry.MainPicture.Value);

        if (DbEntry is { ShowInMainSiteFeed: true, IsDraft: false })
        {
            var (previousContent, laterContent) = Tags.MainFeedPreviousAndLaterContent(3, DbEntry.CreatedOn);
            PreviousPosts = previousContent;
            LaterPosts = laterContent;
        }
        else
        {
            PreviousPosts = new List<IContentCommon>();
            LaterPosts = new List<IContentCommon>();
        }
    }

    public PointContentDto DbEntry { get; }
    public string DirAttribute { get; set; }
    public DateTime? GenerationVersion { get; set; }
    public string LangAttribute { get; set; }
    public List<IContentCommon> LaterPosts { get; }
    public PictureSiteInformation? MainImage { get; }
    public string PageUrl { get; }
    public List<IContentCommon> PreviousPosts { get; }
    public string SiteName { get; }
    public string SiteUrl { get; }

    public async Task WriteLocalHtml()
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var htmlString = TransformText();

        var htmlFileInfo = settings.LocalSitePointHtmlFile(DbEntry);

        if (htmlFileInfo == null)
        {
            var toThrow =
                new Exception("The Point DbEntry did not have valid information to determine a file for the html");
            toThrow.Data.Add("Point DbEntry", DbEntry.SafeObjectDump());
            throw toThrow;
        }

        if (htmlFileInfo.Exists)
        {
            htmlFileInfo.Delete();
            htmlFileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString).ConfigureAwait(false);
    }
}