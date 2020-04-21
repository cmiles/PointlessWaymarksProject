using System.Collections.Generic;
using System.IO;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.PostHtml
{
    public partial class SinglePostPage
    {
        public SinglePostPage(PostContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PostPageUrl(DbEntry);

            var db = Db.Context().Result;

            if (DbEntry.MainPicture != null) MainImage = new PictureSiteInformation(DbEntry.MainPicture.Value);

            var previousLater = Tags.MainFeedPreviousAndLaterContent(3, DbEntry.CreatedOn);
            PreviousPosts = previousLater.previousContent;
            LaterPosts = previousLater.laterContent;
        }

        public PostContent DbEntry { get; }

        public List<IContentCommon> LaterPosts { get; }

        public PictureSiteInformation MainImage { get; }

        public string PageUrl { get; }

        public List<IContentCommon> PreviousPosts { get; }

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
                    $"{Path.Combine(settings.LocalSitePostContentDirectory(DbEntry).FullName, DbEntry.Slug)}.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}