using System.IO;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.FileHtml
{
    public partial class SingleFilePage
    {
        public SingleFilePage(FileContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsUtilities.ReadSettings().Result;
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.FilePageUrl(DbEntry);

            var db = Db.Context().Result;

            if (DbEntry.MainPicture != null) MainImage = new PictureSiteInformation(DbEntry.MainPicture.Value);
        }

        public FileContent DbEntry { get; set; }

        public PictureSiteInformation MainImage { get; }

        public string PageUrl { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }

        public HtmlTag DownloadLinkTag()
        {
            if (!DbEntry.PublicDownloadLink) return HtmlTag.Empty();

            var downloadLinkContainer = new DivTag().AddClass("file-download-container");

            var settings = UserSettingsUtilities.ReadSettings().Result;
            var downloadLink =
                new LinkTag("Download", settings.FileDownloadUrl(DbEntry)).AddClass("file-download-link");
            downloadLinkContainer.Children.Add(downloadLink);

            return downloadLinkContainer;
        }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;

            var htmlString = TransformText();

            var htmlFileInfo =
                new FileInfo(
                    $"{Path.Combine(settings.LocalSiteFileContentDirectory(DbEntry).FullName, DbEntry.Slug)}.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}