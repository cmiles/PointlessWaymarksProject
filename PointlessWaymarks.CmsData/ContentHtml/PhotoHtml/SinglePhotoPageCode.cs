using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;

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
    }

    public PhotoContent DbEntry { get; }
    public string DirAttribute { get; set; }
    public DateTime? GenerationVersion { get; set; }
    public string LangAttribute { get; set; }

    public PhotoContent? NextPhoto { get; set; }
    public string PageUrl { get; }
    public PictureSiteInformation PictureInformation { get; }

    public PhotoContent? PreviousPhoto { get; set; }
    public string SiteName { get; }
    public string SiteUrl { get; }

    public async Task WriteLocalHtml()
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var parser = new HtmlParser();
        var htmlDoc = parser.ParseDocument(TransformText());

        var stringWriter = new StringWriter();
        htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

        var htmlString = stringWriter.ToString();

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