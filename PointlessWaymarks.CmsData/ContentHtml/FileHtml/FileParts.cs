using HtmlTags;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.FileHtml
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

        public static HtmlTag EmbedFileTag(FileContent content)
        {
            if (!content.EmbedFile) return HtmlTag.Empty();

            var embedContainer = new DivTag().AddClass("file-embed-container");

            var settings = UserSettingsSingleton.CurrentSettings();

            if (content.OriginalFileName.TrimNullToEmpty().EndsWith(".pdf"))
            {
                var embedTag = new HtmlTag("embed").Attr("src", settings.FileDownloadUrl(content))
                    .Attr("type", "application/pdf").AddClass("file-embed");
                embedContainer.Children.Add(embedTag);
            }

            return embedContainer;
        }
    }
}