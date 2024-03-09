using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentHtml.VideoHtml;

public partial class SingleVideoPage
{
    public SingleVideoPage(VideoContent dbEntry)
    {
        DbEntry = dbEntry;

        var settings = UserSettingsSingleton.CurrentSettings();
        SiteUrl = settings.SiteUrl();
        SiteName = settings.SiteName;
        PageUrl = settings.VideoPageUrl(DbEntry);
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


    public VideoContent DbEntry { get; set; }

    public string DirAttribute { get; set; }
    public DateTime? GenerationVersion { get; init; }

    public string LangAttribute { get; set; }
    public List<IContentCommon> LaterPosts { get; }
    public PictureSiteInformation? MainImage { get; }
    public string PageUrl { get; set; }

    public List<IContentCommon> PreviousPosts { get; }
    public string SiteName { get; set; }
    public string SiteUrl { get; set; }

    public async Task WriteLocalHtml()
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var htmlString = TransformText();

        var htmlVideoInfo = settings.LocalSiteVideoHtmlFile(DbEntry);

        if (htmlVideoInfo == null)
        {
            var toThrow =
                new Exception("The Video DbEntry did not have valid information to determine a file for the html");
            toThrow.Data.Add("Video DbEntry", DbEntry.SafeObjectDump());
            throw toThrow;
        }

        if (htmlVideoInfo.Exists)
        {
            htmlVideoInfo.Delete();
            htmlVideoInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLog(htmlVideoInfo.FullName, htmlString).ConfigureAwait(false);
    }
}