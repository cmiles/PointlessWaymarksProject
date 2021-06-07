using System;
using System.IO;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.FileHtml
{
    public partial class SingleFilePage
    {
        public SingleFilePage(FileContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.FilePageUrl(DbEntry);

            if (DbEntry.MainPicture != null) MainImage = new PictureSiteInformation(DbEntry.MainPicture.Value);
        }

        public FileContent DbEntry { get; set; }
        public DateTime? GenerationVersion { get; init; }
        public PictureSiteInformation? MainImage { get; }
        public string PageUrl { get; set; }
        public string SiteName { get; set; }
        public string SiteUrl { get; set; }

        public async Task WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo = settings.LocalSiteFileHtmlFile(DbEntry);

            if (htmlFileInfo == null)
            {
                var toThrow =
                    new Exception("The File DbEntry did not have valid information to determine a file for the html");
                toThrow.Data.Add("File DbEntry", ObjectDumper.Dump(DbEntry));
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