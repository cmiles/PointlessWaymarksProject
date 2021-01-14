using System;
using System.IO;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Html.CommonHtml;

namespace PointlessWaymarks.CmsData.Html.PhotoGalleryHtml
{
    public partial class CameraRollGalleryPage
    {
        public HtmlTag? CameraRollContentTag { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? GenerationVersion { get; set; }
        public DateTime? LastDateGroupDateTime { get; set; }
        public PictureSiteInformation? MainImage { get; set; }
        public string? PageUrl { get; set; }
        public string? SiteName { get; set; }

        public void WriteLocalHtml()
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

            FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString);
        }
    }
}