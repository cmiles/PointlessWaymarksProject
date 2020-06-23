using System.IO;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.PhotoHtml
{
    public partial class SinglePhotoPage
    {
        public SinglePhotoPage(PhotoContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PhotoPageUrl(DbEntry);
            
            PictureInformation = new PictureSiteInformation(DbEntry.ContentId);
        }

        public PhotoContent DbEntry { get; }
        public string PageUrl { get; }
        public PictureSiteInformation PictureInformation { get; }
        public string SiteName { get; }
        public string SiteUrl { get; }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo =
                new FileInfo(
                    $"{Path.Combine(settings.LocalSitePhotoContentDirectory(DbEntry).FullName, DbEntry.Slug)}.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}