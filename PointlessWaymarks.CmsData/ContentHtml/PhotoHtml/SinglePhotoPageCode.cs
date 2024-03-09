using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;

public partial class SinglePhotoPage
{
    public SinglePhotoPage(PhotoContent dbEntry)
    {
        DbEntry = dbEntry;

        var settings = UserSettingsSingleton.CurrentSettings();
        SiteUrl = settings.SiteUrl();
        SiteName = settings.SiteName;
        PageUrl = settings.PhotoPageUrl(DbEntry);
        LangAttribute = settings.SiteLangAttribute;
        DirAttribute = settings.SiteDirectionAttribute;

        PictureInformation = new PictureSiteInformation(DbEntry.ContentId);

        var (previousPhoto, nextPhoto) = Tags.PhotoPreviousAndNextContent(dbEntry.PhotoCreatedOn);
        PreviousPhoto = previousPhoto;
        NextPhoto = nextPhoto;

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

    public PhotoContent DbEntry { get; }
    public string DirAttribute { get; set; }
    public DateTime? GenerationVersion { get; set; }
    public string LangAttribute { get; set; }
    public List<IContentCommon> LaterPosts { get; }
    public PhotoContent? NextPhoto { get; set; }
    public string PageUrl { get; }
    public PictureSiteInformation PictureInformation { get; }
    public PhotoContent? PreviousPhoto { get; set; }
    public List<IContentCommon> PreviousPosts { get; }
    public string SiteName { get; }
    public string SiteUrl { get; }

    public async Task WriteLocalHtml()
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var htmlString = TransformText();

        var htmlFileInfo = settings.LocalSitePhotoHtmlFile(DbEntry);

        if (htmlFileInfo == null)
        {
            var toThrow =
                new Exception("The Photo DbEntry did not have valid information to determine a file for the html");
            toThrow.Data.Add("Photo DbEntry", DbEntry.SafeObjectDump());
            throw toThrow;
        }

        if (htmlFileInfo.Exists)
        {
            htmlFileInfo.Delete();
            htmlFileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLogAsync(htmlFileInfo.FullName, htmlString).ConfigureAwait(false);
    }
}