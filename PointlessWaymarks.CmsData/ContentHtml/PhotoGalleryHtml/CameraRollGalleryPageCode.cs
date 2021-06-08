using System;
using System.IO;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;

namespace PointlessWaymarks.CmsData.ContentHtml.PhotoGalleryHtml
{
    public partial class CameraRollGalleryPage
    {
        public HtmlTag? CameraRollContentTag { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? GenerationVersion { get; set; }

        public string? LangAttribute { get; set; }
        public DateTime? LastDateGroupDateTime { get; set; }
        public PictureSiteInformation? MainImage { get; set; }
        public string? PageUrl { get; set; }
        public string? SiteName { get; set; }
        public string? DirAttribute { get; set; }

        public async Task WriteLocalHtml()
        {
            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteCameraRollPhotoGalleryFileInfo();

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString);
        }
    }
}