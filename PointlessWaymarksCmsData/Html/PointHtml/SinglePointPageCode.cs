using System;
using System.IO;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;

namespace PointlessWaymarksCmsData.Html.PointHtml
{
    public partial class SinglePointPage
    {
        public SinglePointPage(PointContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PointPageUrl(DbEntry);

            var db = Db.Context().Result;

            if (DbEntry.MainPicture != null) MainImage = new PictureSiteInformation(DbEntry.MainPicture.Value);
        }

        public PointContent DbEntry { get; }
        public DateTime? GenerationVersion { get; set; }
        public PictureSiteInformation MainImage { get; }
        public string PageUrl { get; }
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
                    $"{Path.Combine(settings.LocalSitePointContentDirectory(DbEntry).FullName, DbEntry.Slug)}.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}