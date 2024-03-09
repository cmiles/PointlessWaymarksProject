using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentHtml.FileHtml;

public partial class SingleFilePage
{
    public SingleFilePage(FileContent dbEntry)
    {
        DbEntry = dbEntry;

        var settings = UserSettingsSingleton.CurrentSettings();
        SiteUrl = settings.SiteUrl();
        SiteName = settings.SiteName;
        PageUrl = settings.FilePageUrl(DbEntry);
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


    public FileContent DbEntry { get; set; }
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

        var htmlFileInfo = settings.LocalSiteFileHtmlFile(DbEntry);

        if (htmlFileInfo == null)
        {
            var toThrow =
                new Exception("The File DbEntry did not have valid information to determine a file for the html");
            toThrow.Data.Add("File DbEntry", DbEntry.SafeObjectDump());
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