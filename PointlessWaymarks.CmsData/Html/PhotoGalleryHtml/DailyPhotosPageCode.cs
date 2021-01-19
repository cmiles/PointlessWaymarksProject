using System;
using System.Collections.Generic;
using System.IO;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Html.CommonHtml;

namespace PointlessWaymarks.CmsData.Html.PhotoGalleryHtml
{
    public partial class DailyPhotosPage
    {
        public string? CreatedBy { get; set; }
        public DateTime? GenerationVersion { get; set; }
        public List<PictureSiteInformation>? ImageList { get; set; }
        public PictureSiteInformation? MainImage { get; set; }
        public DailyPhotosPage? NextDailyPhotosPage { get; set; }
        public string? PageUrl { get; set; }
        public DateTime PhotoPageDate { get; set; }
        public List<Db.TagSlugAndIsExcluded>? PhotoTags { get; set; }
        public DailyPhotosPage? PreviousDailyPhotosPage { get; set; }
        public string? SiteName { get; set; }
        public string? Summary { get; set; }
        public string? Title { get; set; }

        public void WriteLocalHtml()
        {
            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo = UserSettingsSingleton.CurrentSettings()
                .LocalSiteDailyPhotoGalleryFileInfo(PhotoPageDate);

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString);
        }
    }
}