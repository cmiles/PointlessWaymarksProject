using System;
using System.IO;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.CommonHtml;

namespace PointlessWaymarks.CmsData.Html.ImageHtml
{
    public partial class SingleImagePage
    {
        public SingleImagePage(ImageContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.ImagePageUrl(DbEntry);

            PictureInformation = new PictureSiteInformation(DbEntry.ContentId);
        }

        public ImageContent DbEntry { get; }
        public DateTime? GenerationVersion { get; set; }
        public string PageUrl { get; }
        public PictureSiteInformation PictureInformation { get; }
        public string SiteName { get; }
        public string SiteUrl { get; }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument((string) TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo = settings.LocalSiteImageHtmlFile(DbEntry);

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString);
        }
    }
}