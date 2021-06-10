using System;
using System.IO;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.ImageHtml
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
            LangAttribute = settings.SiteLangAttribute;
            DirAttribute = settings.SiteDirectionAttribute;

            PictureInformation = new PictureSiteInformation(DbEntry.ContentId);
        }

        public ImageContent DbEntry { get; }

        public string DirAttribute { get; set; }
        public DateTime? GenerationVersion { get; set; }

        public string LangAttribute { get; set; }
        public string PageUrl { get; }
        public PictureSiteInformation PictureInformation { get; }
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

            var htmlFileInfo = settings.LocalSiteImageHtmlFile(DbEntry);

            if (htmlFileInfo == null)
            {
                var toThrow =
                    new Exception("The Image DbEntry did not have valid information to determine a file for the html");
                toThrow.Data.Add("Line DbEntry", ObjectDumper.Dump(DbEntry));
                throw toThrow;
            }

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString);
        }
    }
}