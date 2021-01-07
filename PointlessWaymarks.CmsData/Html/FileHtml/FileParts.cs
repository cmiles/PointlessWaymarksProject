using HtmlTags;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Html.FileHtml
{
    public static class FileParts
    {
        public static HtmlTag DownloadLinkTag(FileContent content)
        {
            if (!content.PublicDownloadLink) return HtmlTag.Empty();

            var downloadLinkContainer = new DivTag().AddClass("file-download-container");

            var settings = UserSettingsSingleton.CurrentSettings();
            var downloadLink =
                new LinkTag("Download", settings.FileDownloadUrl(content)).AddClass("file-download-link");
            downloadLinkContainer.Children.Add(downloadLink);

            return downloadLinkContainer;
        }
    }
}