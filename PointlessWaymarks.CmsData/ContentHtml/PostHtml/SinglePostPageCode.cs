using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.PostHtml
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
            LangAttribute = settings.SiteLangAttribute;
            DirAttribute = settings.SiteDirectionAttribute;

            if (DbEntry.MainPicture != null) MainImage = new PictureSiteInformation(DbEntry.MainPicture.Value);

            var (previousContent, laterContent) = Tags.MainFeedPreviousAndLaterContent(3, DbEntry.CreatedOn);
            PreviousPosts = previousContent;
            LaterPosts = laterContent;
        }

        public PostContent DbEntry { get; }

        public string DirAttribute { get; set; }
        public DateTime? GenerationVersion { get; set; }

        public string LangAttribute { get; set; }
        public List<IContentCommon> LaterPosts { get; }
        public PictureSiteInformation? MainImage { get; }
        public string PageUrl { get; }
        public List<IContentCommon> PreviousPosts { get; }
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

            var htmlFileInfo = settings.LocalSitePostHtmlFile(DbEntry);

            if (htmlFileInfo == null)
            {
                var toThrow =
                    new Exception("The Post DbEntry did not have valid information to determine a file for the html");
                toThrow.Data.Add("Post DbEntry", ObjectDumper.Dump(DbEntry));
                throw toThrow;
            }

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString).ConfigureAwait(false);
        }
    }
}