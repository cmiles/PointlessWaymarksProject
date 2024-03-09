using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;

namespace PointlessWaymarks.CmsData.ContentHtml.PhotoGalleryHtml;

public partial class CameraRollGalleryPage
{
    public HtmlTag? CameraRollContentTag { get; set; }
    public string? CreatedBy { get; set; }
    public string? DirAttribute { get; set; }
    public DateTime? GenerationVersion { get; set; }

    public string? LangAttribute { get; set; }
    public DateTime? LastDateGroupDateTime { get; set; }
    public PictureSiteInformation? MainImage { get; set; }
    public string? PageUrl { get; set; }
    public string? SiteName { get; set; }

    public async Task WriteLocalHtml()
    {
        var htmlString = TransformText();

        var htmlFileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteCameraRollGalleryFileInfo();

        if (htmlFileInfo.Exists)
        {
            htmlFileInfo.Delete();
            htmlFileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString).ConfigureAwait(false);
    }
}